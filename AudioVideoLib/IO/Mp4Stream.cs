namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// Minimal structural walker for ISO/IEC 14496-12 (MP4 / M4A / 3GP / MOV) containers.
/// Surfaces the iTunes-style metadata in <c>moov.udta.meta.ilst</c> and reports duration from <c>moov.mvhd</c>.
/// </summary>
/// <remarks>
/// The walker captures every top-level box structurally and descends into the
/// <c>moov.udta.meta.ilst</c> chain. The <c>meta</c> box is treated as a full-box (4-byte version/flags
/// before its children). Box sizes are bound-checked against the parent; size==0 ("to end of file") and
/// size==1 ("64-bit extended size in the next 8 bytes") are both supported. Recursion is bounded by an
/// explicit depth cap to avoid stack blow-up on malicious input.
/// </remarks>
public sealed class Mp4Stream : IAudioStream
{
    private const int MaxDepth = 16;
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly List<Mp4Box> _boxes = [];
    private byte[] _originalBytes = [];

    // All offsets below are buffer-relative (i.e. bytes from start of _originalBytes).
    private int _ilstHeaderStart = -1;
    private int _ilstPayloadStart = -1;
    private int _ilstEnd = -1;
    private int _metaHeaderStart = -1;
    private int _metaEnd = -1;
    private int _metaHeaderSize = 8;
    private int _udtaHeaderStart = -1;
    private int _udtaEnd = -1;
    private int _udtaHeaderSize = 8;
    private int _moovHeaderStart = -1;
    private int _moovEnd = -1;
    private int _moovHeaderSize = 8;
    private uint _movieTimescale;
    private ulong _movieDuration;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalAudioLength => _movieTimescale == 0 ? 0 : (long)(_movieDuration * 1000UL / _movieTimescale);

    /// <inheritdoc/>
    public long TotalAudioSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>Gets the top-level boxes discovered in the container, in file order.</summary>
    public IReadOnlyList<Mp4Box> Boxes => _boxes;

    /// <summary>Gets the parsed iTunes metadata tag, or an empty tag if none was present.</summary>
    public Mp4MetaTag Tag { get; private set; } = new();

    /// <summary>Gets the four-character major brand reported by the <c>ftyp</c> box, or <see cref="string.Empty"/> if absent.</summary>
    public string MajorBrand { get; private set; } = string.Empty;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        var length = stream.Length;
        if (length - start < 8)
        {
            return false;
        }

        // Peek the first box; an MP4 must begin with ftyp (occasionally a bare moov / wide / styp).
        var headerPeek = new byte[8];
        if (stream.Read(headerPeek, 0, 8) != 8)
        {
            return false;
        }

        stream.Position = start;
        var firstType = Latin1.GetString(headerPeek, 4, 4);
        if (firstType is not ("ftyp" or "moov" or "mdat" or "free" or "skip" or "wide" or "styp" or "pdin"))
        {
            return false;
        }

        // Buffer the whole stream so we can splice on write and so the walker has random access.
        var totalLength = (int)(length - start);
        _originalBytes = new byte[totalLength];
        var read = stream.Read(_originalBytes, 0, totalLength);
        if (read != totalLength)
        {
            Array.Resize(ref _originalBytes, read);
        }

        StartOffset = start;
        EndOffset = start + read;

        WalkTopLevel();

        // If we found an ilst, parse it now into the tag model.
        if (_ilstPayloadStart >= 0)
        {
            var len = _ilstEnd - _ilstPayloadStart;
            var slice = new byte[len];
            Array.Copy(_originalBytes, _ilstPayloadStart, slice, 0, len);
            Tag = Mp4MetaTag.Parse(slice);
        }

        return _boxes.Count > 0;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the original container bytes with the <c>ilst</c> body replaced by <see cref="Tag"/>'s serialised form,
    /// patching the size fields of the enclosing <c>ilst</c>, <c>meta</c>, <c>udta</c>, and <c>moov</c> boxes.
    /// If the original container has no <c>moov.udta.meta.ilst</c> chain, a fresh chain is built and inserted
    /// inside the existing <c>moov</c> (or appended to the file if no <c>moov</c> exists).
    /// </remarks>
    public byte[] ToByteArray()
    {
        if (_originalBytes.Length == 0)
        {
            return [];
        }

        var newIlstBody = Tag.ToByteArray();
        return _ilstHeaderStart >= 0 && _moovHeaderStart >= 0
            ? SpliceExistingIlst(newIlstBody)
            : BuildChainAndInsert(newIlstBody);
    }

    private byte[] SpliceExistingIlst(byte[] newIlstBody)
    {
        var newIlstAtomSize = 8 + newIlstBody.Length;
        var oldIlstAtomSize = _ilstEnd - _ilstHeaderStart;
        var delta = newIlstAtomSize - oldIlstAtomSize;

        var resultLength = _originalBytes.Length + delta;
        var result = new byte[resultLength];

        Array.Copy(_originalBytes, 0, result, 0, _ilstHeaderStart);

        WriteBeU32(result, _ilstHeaderStart, (uint)newIlstAtomSize);
        WriteAscii(result, _ilstHeaderStart + 4, "ilst");
        Array.Copy(newIlstBody, 0, result, _ilstHeaderStart + 8, newIlstBody.Length);

        var tail = _originalBytes.Length - _ilstEnd;
        if (tail > 0)
        {
            Array.Copy(_originalBytes, _ilstEnd, result, _ilstHeaderStart + newIlstAtomSize, tail);
        }

        PatchAncestorSize(result, _metaHeaderStart, _metaHeaderSize, _metaEnd - _metaHeaderStart, delta);
        PatchAncestorSize(result, _udtaHeaderStart, _udtaHeaderSize, _udtaEnd - _udtaHeaderStart, delta);
        PatchAncestorSize(result, _moovHeaderStart, _moovHeaderSize, _moovEnd - _moovHeaderStart, delta);
        return result;
    }

    private byte[] BuildChainAndInsert(byte[] newIlstBody)
    {
        // Build: udta { meta(version/flags) { hdlr { 'mdir' 'appl' } ilst { ... } } }
        // hdlr is required by some readers; we provide a minimal one.
        var ilstAtom = WrapAtom("ilst", newIlstBody);
        var hdlrAtom = BuildHdlrAtom();
        var metaInner = ConcatBytes(hdlrAtom, ilstAtom);
        var metaPayload = ConcatBytes([0, 0, 0, 0], metaInner);
        var metaAtom = WrapAtom("meta", metaPayload);
        var udtaAtom = WrapAtom("udta", metaAtom);

        if (_moovHeaderStart < 0)
        {
            // No moov in the original — append a moov containing just udta. Many players will reject this,
            // but it is the best we can do for inputs that never had one to begin with.
            var moovAtom = WrapAtom("moov", udtaAtom);
            return ConcatBytes(_originalBytes, moovAtom);
        }

        // Insert the udta box at the end of the existing moov payload, then patch moov size.
        var insertOffset = _moovEnd;
        var resultLength = _originalBytes.Length + udtaAtom.Length;
        var result = new byte[resultLength];
        Array.Copy(_originalBytes, 0, result, 0, insertOffset);
        Array.Copy(udtaAtom, 0, result, insertOffset, udtaAtom.Length);
        Array.Copy(_originalBytes, insertOffset, result, insertOffset + udtaAtom.Length, _originalBytes.Length - insertOffset);

        PatchAncestorSize(result, _moovHeaderStart, _moovHeaderSize, _moovEnd - _moovHeaderStart, udtaAtom.Length);
        return result;
    }

    private static void PatchAncestorSize(byte[] buffer, int headerStart, int headerSize, int oldSize, int delta)
    {
        if (headerStart < 0)
        {
            return;
        }

        var newSize = oldSize + delta;
        if (headerSize == 16)
        {
            // 64-bit extended size: size32 field stays as 1, true size lives 8 bytes after the type.
            WriteBeU64(buffer, headerStart + 8, (ulong)newSize);
        }
        else
        {
            WriteBeU32(buffer, headerStart, (uint)newSize);
        }
    }

    private void WalkTopLevel()
    {
        var pos = 0;
        while (pos + 8 <= _originalBytes.Length)
        {
            if (!TryReadBoxHeader(_originalBytes, pos, _originalBytes.Length, out var type, out var atomSize, out var headerSize))
            {
                break;
            }

            var atomEnd = pos + (int)atomSize;
            _boxes.Add(new Mp4Box(type, StartOffset + pos, StartOffset + atomEnd, headerSize));

            switch (type)
            {
                case "ftyp":
                    if (atomSize >= headerSize + 4)
                    {
                        MajorBrand = Latin1.GetString(_originalBytes, pos + headerSize, 4);
                    }

                    break;
                case "moov":
                    _moovHeaderStart = pos;
                    _moovHeaderSize = headerSize;
                    _moovEnd = atomEnd;
                    WalkContainer(pos + headerSize, atomEnd, 1);
                    break;
            }

            pos = atomEnd;
        }
    }

    private void WalkContainer(int start, int end, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        var pos = start;
        while (pos + 8 <= end)
        {
            if (!TryReadBoxHeader(_originalBytes, pos, end, out var type, out var atomSize, out var headerSize))
            {
                break;
            }

            var atomEnd = pos + (int)atomSize;
            switch (type)
            {
                case "udta":
                    _udtaHeaderStart = pos;
                    _udtaHeaderSize = headerSize;
                    _udtaEnd = atomEnd;
                    WalkContainer(pos + headerSize, atomEnd, depth + 1);
                    break;
                case "meta":
                    _metaHeaderStart = pos;
                    _metaHeaderSize = headerSize;
                    _metaEnd = atomEnd;
                    // Full-box header: skip 4 bytes of version+flags before recursing.
                    if (atomEnd - (pos + headerSize) >= 4)
                    {
                        WalkContainer(pos + headerSize + 4, atomEnd, depth + 1);
                    }

                    break;
                case "ilst":
                    _ilstHeaderStart = pos;
                    _ilstPayloadStart = pos + headerSize;
                    _ilstEnd = atomEnd;
                    break;
                case "mvhd":
                    AbsorbMvhd(pos + headerSize, atomEnd);
                    break;
                case "trak":
                case "mdia":
                case "minf":
                case "stbl":
                case "edts":
                case "dinf":
                    WalkContainer(pos + headerSize, atomEnd, depth + 1);
                    break;
            }

            pos = atomEnd;
        }
    }

    private void AbsorbMvhd(int start, int end)
    {
        if (end - start < 4)
        {
            return;
        }

        var version = _originalBytes[start];
        var pos = start + 4;

        // mvhd v0: creation(4) modification(4) timescale(4) duration(4) ...
        // mvhd v1: creation(8) modification(8) timescale(4) duration(8) ...
        if (version == 1)
        {
            if (end - pos < 28)
            {
                return;
            }

            pos += 16; // creation + modification
            _movieTimescale = ReadBeU32(_originalBytes, pos);
            pos += 4;
            _movieDuration = ReadBeU64(_originalBytes, pos);
        }
        else
        {
            if (end - pos < 16)
            {
                return;
            }

            pos += 8; // creation + modification
            _movieTimescale = ReadBeU32(_originalBytes, pos);
            pos += 4;
            _movieDuration = ReadBeU32(_originalBytes, pos);
        }
    }

    private static bool TryReadBoxHeader(byte[] buffer, int pos, int containerEnd, out string type, out long atomSize, out int headerSize)
    {
        type = string.Empty;
        atomSize = 0;
        headerSize = 8;
        if (pos + 8 > containerEnd)
        {
            return false;
        }

        var size32 = ReadBeU32(buffer, pos);
        type = Latin1.GetString(buffer, pos + 4, 4);

        if (size32 == 1)
        {
            if (pos + 16 > containerEnd)
            {
                return false;
            }

            var ext = ReadBeU64(buffer, pos + 8);
            if (ext is < 16 or > long.MaxValue)
            {
                return false;
            }

            atomSize = (long)ext;
            headerSize = 16;
        }
        else if (size32 == 0)
        {
            // "to end of container"
            atomSize = containerEnd - pos;
            if (atomSize < 8)
            {
                return false;
            }
        }
        else if (size32 < 8)
        {
            return false;
        }
        else
        {
            atomSize = size32;
        }

        if (pos + atomSize > containerEnd)
        {
            // Truncated atom — clamp so the walker advances rather than spinning.
            atomSize = containerEnd - pos;
            if (atomSize < headerSize)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] BuildHdlrAtom()
    {
        // hdlr (full box): version(1)=0, flags(3)=0, predefined(4)=0, handler_type(4)='mdir',
        // reserved[3*4]=0, name(\0).
        var payload = new byte[8 + 4 + 4 + 12 + 1];
        // version+flags already zero.
        WriteAscii(payload, 8, "mdir");
        WriteAscii(payload, 12, "appl");
        // remaining bytes already zero (3 reserved uint32 + null name)
        return WrapAtom("hdlr", payload);
    }

    private static byte[] WrapAtom(string type, byte[] payload)
    {
        var size = 8 + payload.Length;
        var result = new byte[size];
        WriteBeU32(result, 0, (uint)size);
        WriteAscii(result, 4, type);
        Array.Copy(payload, 0, result, 8, payload.Length);
        return result;
    }

    private static byte[] ConcatBytes(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static uint ReadBeU32(byte[] b, int off) =>
        ((uint)b[off] << 24) | ((uint)b[off + 1] << 16) | ((uint)b[off + 2] << 8) | b[off + 3];

    private static ulong ReadBeU64(byte[] b, int off) =>
        ((ulong)b[off] << 56) | ((ulong)b[off + 1] << 48) | ((ulong)b[off + 2] << 40) | ((ulong)b[off + 3] << 32)
        | ((ulong)b[off + 4] << 24) | ((ulong)b[off + 5] << 16) | ((ulong)b[off + 6] << 8) | b[off + 7];

    private static void WriteBeU32(byte[] b, int off, uint v)
    {
        b[off] = (byte)((v >> 24) & 0xFF);
        b[off + 1] = (byte)((v >> 16) & 0xFF);
        b[off + 2] = (byte)((v >> 8) & 0xFF);
        b[off + 3] = (byte)(v & 0xFF);
    }

    private static void WriteAscii(byte[] b, int off, string s)
    {
        var bytes = Latin1.GetBytes(s);
        Array.Copy(bytes, 0, b, off, Math.Min(4, bytes.Length));
    }

    private static void WriteBeU64(byte[] b, int off, ulong v)
    {
        for (var i = 0; i < 8; i++)
        {
            b[off + i] = (byte)((v >> (56 - (i * 8))) & 0xFF);
        }
    }
}
