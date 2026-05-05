namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Structural walker for Monkey's Audio (<c>.ape</c>) files (version 3.98+ — files with the
/// <c>APE_DESCRIPTOR</c> + <c>APE_HEADER</c> layout). Surfaces the descriptor, header, seek
/// table, and per-frame byte ranges. <see cref="WriteTo(Stream)"/> is byte-passthrough — no
/// audio re-encoding happens at any point. APEv2 footer / ID3v1 footer tag editing flows
/// through the existing <c>AudioTags</c> scanner; this walker only handles the audio container.
/// </summary>
/// <remarks>
/// Both integer (<c>"MAC "</c>) and float (<c>"MACF"</c>) variants are accepted; the active
/// variant is surfaced via <see cref="Format"/>. See
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APECompressCreate.cpp:250-253</c> for how the
/// upstream encoder writes the cID. Pre-3.98 files (<c>APE_HEADER_OLD</c>, no descriptor) are
/// not supported — <see cref="ReadStream(Stream)"/> returns <c>false</c> for them.
/// <para />
/// <see cref="ReadStream"/> populates <c>_source</c> with a <see cref="StreamSourceReader"/>
/// that holds the supplied <see cref="Stream"/> open via <c>leaveOpen: true</c>; callers must
/// keep that <see cref="Stream"/> alive until <see cref="WriteTo"/> finishes, in line with the
/// source-stream lifetime contract on <see cref="IMediaContainer"/>.
/// </remarks>
public sealed class MacStream : IMediaContainer, IDisposable
{
    private const string DetachedSourceMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly List<MacSeekEntry> _seekEntries = [];
    private readonly List<MacFrame> _frames = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration => Header is null || Header.SampleRate == 0
        ? 0
        : (long)(TotalBlocks * 1000.0 / Header.SampleRate);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>Gets the parsed descriptor, or <c>null</c> if <see cref="ReadStream"/> has not been called.</summary>
    public MacDescriptor? Descriptor { get; private set; }

    /// <summary>Gets the parsed header, or <c>null</c> if <see cref="ReadStream"/> has not been called.</summary>
    public MacHeader? Header { get; private set; }

    /// <summary>Gets the seek-table entries.</summary>
    public IReadOnlyList<MacSeekEntry> SeekEntries => _seekEntries;

    /// <summary>Gets the per-frame byte ranges.</summary>
    public IReadOnlyList<MacFrame> Frames => _frames;

    /// <summary>
    /// Gets which sample format the file uses (integer or float), per the descriptor's cID.
    /// Defaults to <see cref="MacFormat.Integer"/> when nothing has been read.
    /// </summary>
    public MacFormat Format { get; private set; } = MacFormat.Integer;

    private long TotalBlocks =>
        Header is null || Header.TotalFrames == 0
            ? 0
            : ((long)(Header.TotalFrames - 1) * Header.BlocksPerFrame) + Header.FinalFrameBlocks;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < 8)
        {
            return false;
        }

        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4)
        {
            return false;
        }

        stream.Position = start;

        var id = Latin1.GetString(magic);
        switch (id)
        {
            case "MAC ":
                Format = MacFormat.Integer;
                break;
            case "MACF":
                Format = MacFormat.Float;
                break;
            default:
                return false;
        }

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);

        if (!TryReadDescriptor(stream, out var descriptor))
        {
            return false;
        }

        if (descriptor!.Version < 3980)
        {
            // Pre-3.98 files (APE_HEADER_OLD) are out of scope for this walker.
            return false;
        }

        Descriptor = descriptor;

        if (!TryReadHeader(stream, descriptor, out var header))
        {
            return false;
        }

        Header = header;

        ReadSeekTable(stream, descriptor);

        // Skip any preserved-WAV-header bytes — these are pre-audio bytes that the walker
        // does not interpret but must round-trip via the source. With CreateWavHeaderFlag set,
        // HeaderDataBytes is zero (per APEHeader.cpp:255). Otherwise it's the original WAV
        // header that the encoder preserved verbatim.
        if (descriptor.HeaderDataBytes > 0)
        {
            stream.Seek(descriptor.HeaderDataBytes, SeekOrigin.Current);
        }

        BuildFrameTable();
        EndOffset = start + _source.Length;
        return true;
    }

    private static bool TryReadDescriptor(Stream stream, out MacDescriptor? descriptor)
    {
        descriptor = null;

        // The descriptor is at least 52 bytes (4 + 2 + 2 + 7×4 + 16). Read the fixed block first.
        Span<byte> fixedBlock = stackalloc byte[52];
        if (stream.Read(fixedBlock) != fixedBlock.Length)
        {
            return false;
        }

        var id = Latin1.GetString(fixedBlock[..4]);
        if (id is not ("MAC " or "MACF"))
        {
            return false;
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(fixedBlock[4..6]);

        // bytes 6..8 are nPadding — discarded.
        var descriptorBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[8..12]);
        var headerBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[12..16]);
        var seekTableBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[16..20]);
        var headerDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[20..24]);
        var apeFrameDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[24..28]);
        var apeFrameDataBytesHigh = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[28..32]);
        var terminatingDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[32..36]);
        var md5 = fixedBlock.Slice(36, 16);

        descriptor = new MacDescriptor(
            id,
            version,
            descriptorBytes,
            headerBytes,
            seekTableBytes,
            headerDataBytes,
            apeFrameDataBytes,
            apeFrameDataBytesHigh,
            terminatingDataBytes,
            md5);

        // Forward-compat: future versions may extend the descriptor; skip the rest.
        if (descriptorBytes > fixedBlock.Length)
        {
            stream.Seek(descriptorBytes - fixedBlock.Length, SeekOrigin.Current);
        }

        return true;
    }

    private static bool TryReadHeader(Stream stream, MacDescriptor descriptor, out MacHeader? header)
    {
        header = null;

        Span<byte> buf = stackalloc byte[24];
        if (stream.Read(buf) != buf.Length)
        {
            return false;
        }

        var compressionLevel = BinaryPrimitives.ReadUInt16LittleEndian(buf[0..2]);
        var formatFlags = BinaryPrimitives.ReadUInt16LittleEndian(buf[2..4]);
        var blocksPerFrame = BinaryPrimitives.ReadUInt32LittleEndian(buf[4..8]);
        var finalFrameBlocks = BinaryPrimitives.ReadUInt32LittleEndian(buf[8..12]);
        var totalFrames = BinaryPrimitives.ReadUInt32LittleEndian(buf[12..16]);
        var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(buf[16..18]);
        var channels = BinaryPrimitives.ReadUInt16LittleEndian(buf[18..20]);
        var sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(buf[20..24]);

        header = new MacHeader(
            compressionLevel,
            formatFlags,
            blocksPerFrame,
            finalFrameBlocks,
            totalFrames,
            bitsPerSample,
            channels,
            sampleRate);

        // Forward-compat: HeaderBytes might be larger in a future version.
        if (descriptor.HeaderBytes > buf.Length)
        {
            stream.Seek(descriptor.HeaderBytes - buf.Length, SeekOrigin.Current);
        }

        return true;
    }

    private void ReadSeekTable(Stream stream, MacDescriptor descriptor)
    {
        _seekEntries.Clear();
        var elementCount = (int)(descriptor.SeekTableBytes / 4);
        if (elementCount <= 0)
        {
            return;
        }

        var raw = new byte[descriptor.SeekTableBytes];
        if (stream.Read(raw) != raw.Length)
        {
            return;
        }

        for (var i = 0; i < elementCount; i++)
        {
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(i * 4, 4));
            _seekEntries.Add(new MacSeekEntry(i, offset));
        }
    }

    private void BuildFrameTable()
    {
        _frames.Clear();
        if (Header is null || Descriptor is null || _seekEntries.Count == 0 || Header.TotalFrames == 0)
        {
            return;
        }

        // The audio region's last byte is at:
        //   audioStart + descriptor.TotalApeFrameDataBytes
        // where audioStart is the file offset of the first audio byte. Per APEInfo.cpp:439 the
        // first audio byte sits at descriptor.DescriptorBytes + descriptor.HeaderBytes
        // + descriptor.SeekTableBytes + descriptor.HeaderDataBytes (relative to the start of
        // the descriptor, plus any leading junk-header bytes that this walker doesn't yet
        // surface). For frame-length math we only need the END of the audio region, which the
        // seek table itself anchors via descriptor.TotalApeFrameDataBytes.
        var audioEnd = _seekEntries[0].FileOffset + Descriptor.TotalApeFrameDataBytes;

        for (var i = 0; i < _seekEntries.Count; i++)
        {
            var startOffset = _seekEntries[i].FileOffset;
            var length = i + 1 < _seekEntries.Count
                ? _seekEntries[i + 1].FileOffset - startOffset
                : audioEnd - startOffset;

            var blocks = i == Header.TotalFrames - 1 ? Header.FinalFrameBlocks : Header.BlocksPerFrame;
            _frames.Add(new MacFrame(startOffset, length, blocks));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Byte-exact passthrough of the source. The walker does not edit the descriptor, header,
    /// seek table, preserved-WAV-header region, or audio region; APE/ID3 tag editing flows
    /// through the existing <c>AudioTags</c> scanner, which mutates only the trailing footer
    /// regions outside any of the descriptor's accounted ranges. Mirrors the Mp4Stream /
    /// MpcStream / TtaStream full-passthrough pattern: source offsets are relative to the
    /// position the stream was at when the source reader was constructed (i.e. StartOffset
    /// is offset 0 within the source view).
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(DetachedSourceMessage);
        }

        _source.CopyTo(0, _source.Length, destination);
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
}
