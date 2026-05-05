namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
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
/// The walker keeps an <see cref="ISourceReader"/> reference rather than buffering the file
/// in memory; <see cref="WriteTo(Stream)"/> streams unchanged regions directly from source to
/// destination, so editing iTunes metadata in a multi-GB MP4 is bounded by the metadata size,
/// not the file size.
/// <para />
/// Box sizes are bound-checked against the parent; <c>size==0</c> ("to end of file") and
/// <c>size==1</c> ("64-bit extended size in the next 8 bytes") are both supported. Recursion
/// is bounded by an explicit depth cap to avoid stack blow-up on malicious input.
/// <para />
/// Callers must keep the source <see cref="Stream"/> passed to <see cref="ReadStream(Stream)"/>
/// alive until <see cref="WriteTo(Stream)"/> / <c>ToByteArray</c> finishes.
/// </remarks>
public sealed class Mp4Stream : IMediaContainer, IDisposable
{
    private const int MaxDepth = 16;
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly List<Mp4Box> _boxes = [];
    private ISourceReader? _source;

    // All offsets below are file offsets (relative to the source start).
    private long _ilstHeaderStart = -1;
    private long _ilstPayloadStart = -1;
    private long _ilstEnd = -1;
    private long _metaHeaderStart = -1;
    private long _metaEnd = -1;
    private int _metaHeaderSize = 8;
    private long _udtaHeaderStart = -1;
    private long _udtaEnd = -1;
    private int _udtaHeaderSize = 8;
    private long _moovHeaderStart = -1;
    private long _moovEnd = -1;
    private int _moovHeaderSize = 8;
    private uint _movieTimescale;
    private ulong _movieDuration;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration => _movieTimescale == 0 ? 0 : (long)(_movieDuration * 1000UL / _movieTimescale);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>Gets the top-level boxes discovered in the container, in file order.</summary>
    public IReadOnlyList<Mp4Box> Boxes => _boxes;

    /// <summary>Gets the parsed iTunes metadata tag, or an empty tag if none was present.</summary>
    /// <remarks>
    /// Setter accepts <c>null</c> as shorthand for "clear all metadata"; the property
    /// is reset to a fresh empty <see cref="Mp4MetaTag"/> in that case.
    /// </remarks>
    public Mp4MetaTag Tag
    {
        get;
        set => field = value ?? new Mp4MetaTag();
    } = new();

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

        // Peek the first box; an MP4 must begin with one of a small set of recognised top-level types.
        Span<byte> headerPeek = stackalloc byte[8];
        if (stream.Read(headerPeek) != 8)
        {
            return false;
        }

        stream.Position = start;
        var firstType = Latin1.GetString(headerPeek[4..]);
        if (firstType is not ("ftyp" or "moov" or "mdat" or "free" or "skip" or "wide" or "styp" or "pdin"))
        {
            return false;
        }

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        // Walk the live stream — reads are sequential within each box, with seek-past
        // for nested-box content we don't need (mdat in particular).
        WalkTopLevel(stream, EndOffset);

        // Parse the ilst payload (typically a few KB).
        if (_ilstPayloadStart >= 0)
        {
            var len = (int)(_ilstEnd - _ilstPayloadStart);
            var slice = new byte[len];
            _source.Read(_ilstPayloadStart, slice);
            Tag = Mp4MetaTag.Parse(slice);
        }

        // Position the stream after the container so the outer scanner can continue.
        stream.Position = EndOffset;
        return _boxes.Count > 0;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Streams head + new <c>ilst</c> + tail directly from the source to the destination,
    /// patching the size fields of the enclosing <c>moov</c> / <c>udta</c> / <c>meta</c>
    /// boxes inline as the bytes flow through. Peak memory is bounded by the metadata
    /// size, not the file size.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        var newIlstBody = Tag.ToByteArray();
        if (_ilstHeaderStart >= 0 && _moovHeaderStart >= 0)
        {
            SpliceExistingIlst(destination, newIlstBody);
        }
        else
        {
            BuildChainAndInsert(destination, newIlstBody);
        }
    }

    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
    /// <see cref="Stream"/>; the caller still owns that.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }

    /// <summary>
    /// File offset of the byte immediately after a box's header bytes.
    /// </summary>
    private static long PayloadStartOf(long headerStart, int headerSize) => headerStart + headerSize;

    private void SpliceExistingIlst(Stream destination, byte[] newIlstBody)
    {
        var newIlstAtomSize = 8L + newIlstBody.Length;
        var oldIlstAtomSize = _ilstEnd - _ilstHeaderStart;
        var delta = newIlstAtomSize - oldIlstAtomSize;

        // Pre-moov bytes (often the large mdat, for non-faststart files).
        if (_moovHeaderStart > 0)
        {
            _source!.CopyTo(0, _moovHeaderStart, destination);
        }

        // Patched moov header.
        WriteAtomHeader(destination, "moov", _moovEnd - _moovHeaderStart + delta, _moovHeaderSize);

        // Moov children before udta.
        var moovDataStart = PayloadStartOf(_moovHeaderStart, _moovHeaderSize);
        if (_udtaHeaderStart > moovDataStart)
        {
            _source!.CopyTo(moovDataStart, _udtaHeaderStart - moovDataStart, destination);
        }

        // Patched udta header.
        WriteAtomHeader(destination, "udta", _udtaEnd - _udtaHeaderStart + delta, _udtaHeaderSize);

        // Udta children before meta.
        var udtaDataStart = PayloadStartOf(_udtaHeaderStart, _udtaHeaderSize);
        if (_metaHeaderStart > udtaDataStart)
        {
            _source!.CopyTo(udtaDataStart, _metaHeaderStart - udtaDataStart, destination);
        }

        // Patched meta header.
        WriteAtomHeader(destination, "meta", _metaEnd - _metaHeaderStart + delta, _metaHeaderSize);

        // Meta children before ilst (version+flags + optional hdlr).
        var metaDataStart = PayloadStartOf(_metaHeaderStart, _metaHeaderSize);
        if (_ilstHeaderStart > metaDataStart)
        {
            _source!.CopyTo(metaDataStart, _ilstHeaderStart - metaDataStart, destination);
        }

        // New ilst header + body.
        WriteAtomHeader(destination, "ilst", newIlstAtomSize, 8);
        destination.Write(newIlstBody, 0, newIlstBody.Length);

        // Tail — everything after the old ilst.
        var tailLen = _source!.Length - _ilstEnd;
        if (tailLen > 0)
        {
            _source.CopyTo(_ilstEnd, tailLen, destination);
        }
    }

    private void BuildChainAndInsert(Stream destination, byte[] newIlstBody)
    {
        var ilstAtom = WrapAtom("ilst", newIlstBody);
        var hdlrAtom = BuildHdlrAtom();
        var metaInner = ConcatBytes(hdlrAtom, ilstAtom);
        var metaPayload = ConcatBytes([0, 0, 0, 0], metaInner);
        var metaAtom = WrapAtom("meta", metaPayload);
        var udtaAtom = WrapAtom("udta", metaAtom);

        if (_moovHeaderStart < 0)
        {
            // No moov in original — stream the source verbatim then append a fresh moov.
            // Many players will reject this layout, but it's the best we can do.
            var moovAtom = WrapAtom("moov", udtaAtom);
            _source!.CopyTo(0, _source.Length, destination);
            destination.Write(moovAtom, 0, moovAtom.Length);
            return;
        }

        // Pre-moov bytes.
        if (_moovHeaderStart > 0)
        {
            _source!.CopyTo(0, _moovHeaderStart, destination);
        }

        // Patched moov header — size grows by udtaAtom.Length.
        WriteAtomHeader(destination, "moov", _moovEnd - _moovHeaderStart + udtaAtom.Length, _moovHeaderSize);

        // Existing moov children.
        var moovDataStart = PayloadStartOf(_moovHeaderStart, _moovHeaderSize);
        var moovDataLen = _moovEnd - moovDataStart;
        if (moovDataLen > 0)
        {
            _source!.CopyTo(moovDataStart, moovDataLen, destination);
        }

        // New udta atom appended inside moov.
        destination.Write(udtaAtom, 0, udtaAtom.Length);

        // Anything after moov.
        var postMoovLen = _source!.Length - _moovEnd;
        if (postMoovLen > 0)
        {
            _source.CopyTo(_moovEnd, postMoovLen, destination);
        }
    }

    private static void WriteAtomHeader(Stream destination, string type, long size, int headerSize)
    {
        Span<byte> hdr = stackalloc byte[16];
        if (headerSize == 16)
        {
            BinaryPrimitives.WriteUInt32BigEndian(hdr, 1);
            Latin1.GetBytes(type, hdr[4..8]);
            BinaryPrimitives.WriteUInt64BigEndian(hdr[8..16], (ulong)size);
            destination.Write(hdr);
        }
        else
        {
            if (size > uint.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Atom '{type}' size {size} exceeds 32-bit limit; the original header used a 32-bit size, " +
                    "and growing to 64-bit would shift downstream offsets which the splice writer can't safely fix up.");
            }

            BinaryPrimitives.WriteUInt32BigEndian(hdr, (uint)size);
            Latin1.GetBytes(type, hdr[4..8]);
            destination.Write(hdr[..8]);
        }
    }

    private void WalkTopLevel(Stream stream, long end)
    {
        Span<byte> brandBuf = stackalloc byte[4];
        while (stream.Position + 8 <= end)
        {
            var pos = stream.Position;
            if (!TryReadBoxHeader(stream, end, out var type, out var atomSize, out var headerSize))
            {
                break;
            }

            var atomEnd = pos + atomSize;
            _boxes.Add(new Mp4Box(type, pos, atomEnd, headerSize));

            switch (type)
            {
                case "ftyp":
                    if (atomSize >= headerSize + 4)
                    {
                        stream.Position = pos + headerSize;
                        stream.ReadExactly(brandBuf);
                        MajorBrand = Latin1.GetString(brandBuf);
                    }

                    break;
                case "moov":
                    _moovHeaderStart = pos - StartOffset;
                    _moovHeaderSize = headerSize;
                    _moovEnd = atomEnd - StartOffset;
                    stream.Position = pos + headerSize;
                    WalkContainer(stream, atomEnd, 1);
                    break;
            }

            stream.Position = atomEnd;
        }
    }

    private void WalkContainer(Stream stream, long end, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        while (stream.Position + 8 <= end)
        {
            var pos = stream.Position;
            if (!TryReadBoxHeader(stream, end, out var type, out var atomSize, out var headerSize))
            {
                break;
            }

            var atomEnd = pos + atomSize;
            switch (type)
            {
                case "udta":
                    _udtaHeaderStart = pos - StartOffset;
                    _udtaHeaderSize = headerSize;
                    _udtaEnd = atomEnd - StartOffset;
                    stream.Position = pos + headerSize;
                    WalkContainer(stream, atomEnd, depth + 1);
                    break;
                case "meta":
                    _metaHeaderStart = pos - StartOffset;
                    _metaHeaderSize = headerSize;
                    _metaEnd = atomEnd - StartOffset;
                    // Full-box header: skip 4 bytes of version+flags before recursing.
                    if (atomEnd - (pos + headerSize) >= 4)
                    {
                        stream.Position = pos + headerSize + 4;
                        WalkContainer(stream, atomEnd, depth + 1);
                    }

                    break;
                case "ilst":
                    _ilstHeaderStart = pos - StartOffset;
                    _ilstPayloadStart = pos + headerSize - StartOffset;
                    _ilstEnd = atomEnd - StartOffset;
                    break;
                case "mvhd":
                    AbsorbMvhd(stream, pos + headerSize, atomEnd);
                    break;
                case "trak":
                case "mdia":
                case "minf":
                case "stbl":
                case "edts":
                case "dinf":
                    stream.Position = pos + headerSize;
                    WalkContainer(stream, atomEnd, depth + 1);
                    break;
            }

            stream.Position = atomEnd;
        }
    }

    private void AbsorbMvhd(Stream stream, long start, long end)
    {
        var len = end - start;
        if (len < 4)
        {
            return;
        }

        stream.Position = start;
        var version = (byte)stream.ReadByte();
        stream.Position = start + 4;

        Span<byte> buf = stackalloc byte[28];

        // mvhd v0: creation(4) modification(4) timescale(4) duration(4) ...
        // mvhd v1: creation(8) modification(8) timescale(4) duration(8) ...
        if (version == 1)
        {
            if (len < 32)
            {
                return;
            }

            stream.ReadExactly(buf);
            _movieTimescale = BinaryPrimitives.ReadUInt32BigEndian(buf[16..]);
            _movieDuration = BinaryPrimitives.ReadUInt64BigEndian(buf[20..]);
        }
        else
        {
            if (len < 20)
            {
                return;
            }

            stream.ReadExactly(buf[..16]);
            _movieTimescale = BinaryPrimitives.ReadUInt32BigEndian(buf[8..]);
            _movieDuration = BinaryPrimitives.ReadUInt32BigEndian(buf[12..]);
        }
    }

    private static bool TryReadBoxHeader(Stream stream, long containerEnd, out string type, out long atomSize, out int headerSize)
    {
        type = string.Empty;
        atomSize = 0;
        headerSize = 8;
        var startPos = stream.Position;
        if (startPos + 8 > containerEnd)
        {
            return false;
        }

        Span<byte> hdr = stackalloc byte[16];
        stream.ReadExactly(hdr[..8]);
        var size32 = BinaryPrimitives.ReadUInt32BigEndian(hdr);
        type = Latin1.GetString(hdr[4..8]);

        if (size32 == 1)
        {
            if (startPos + 16 > containerEnd)
            {
                stream.Position = startPos;
                return false;
            }

            stream.ReadExactly(hdr[8..16]);
            var ext = BinaryPrimitives.ReadUInt64BigEndian(hdr[8..16]);
            if (ext is < 16 or > long.MaxValue)
            {
                stream.Position = startPos;
                return false;
            }

            atomSize = (long)ext;
            headerSize = 16;
        }
        else if (size32 == 0)
        {
            // "to end of container"
            atomSize = containerEnd - startPos;
            if (atomSize < 8)
            {
                stream.Position = startPos;
                return false;
            }
        }
        else if (size32 < 8)
        {
            stream.Position = startPos;
            return false;
        }
        else
        {
            atomSize = size32;
        }

        if (startPos + atomSize > containerEnd)
        {
            // Truncated atom — clamp so the walker advances rather than spinning.
            atomSize = containerEnd - startPos;
            if (atomSize < headerSize)
            {
                stream.Position = startPos;
                return false;
            }
        }

        // Leave the stream at the start of the atom payload.
        stream.Position = startPos + headerSize;
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
        BinaryPrimitives.WriteUInt32BigEndian(result, (uint)size);
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

    private static void WriteAscii(byte[] b, int off, string s)
    {
        var bytes = Latin1.GetBytes(s);
        Array.Copy(bytes, 0, b, off, Math.Min(4, bytes.Length));
    }
}
