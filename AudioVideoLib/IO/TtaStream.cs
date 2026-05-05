namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Walker for TrueAudio (<c>.tta</c>) streams. Parses the TTA1 fixed header and the
/// per-frame seek table, exposing the frame layout for inspection. <see cref="WriteTo"/>
/// streams the original bytes verbatim from the source — no audio re-encoding.
/// </summary>
/// <remarks>
/// Format reference: <c>3rdparty/libtta-c-2.3/libtta.c</c>, especially
/// <c>read_tta_header</c> (lines 449-471) and <c>tta_decoder_read_seek_table</c>
/// (lines 473-490). The standard frame length in samples is libtta's
/// <c>MUL_FRAME_TIME(sps) = 256 * sps / 245</c> (libtta.c:274).
/// <para />
/// <see cref="ReadStream"/> populates <c>_source</c> with a <see cref="StreamSourceReader"/>
/// that holds the supplied <see cref="Stream"/> open via <c>leaveOpen: true</c>; callers
/// must keep that <see cref="Stream"/> alive until <see cref="WriteTo"/> finishes, in line
/// with the source-stream lifetime contract on <see cref="IMediaContainer"/>.
/// </remarks>
public sealed class TtaStream : IMediaContainer, IDisposable
{
    private const string DetachedSourceMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    private readonly List<TtaFrame> _frames = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration =>
        Header is null || Header.SampleRate == 0
            ? 0
            : (long)(Header.TotalSamples * 1000UL / Header.SampleRate);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>The parsed fixed header, or <c>null</c> if <see cref="ReadStream"/> has not run successfully.</summary>
    public TtaHeader? Header { get; private set; }

    /// <summary>The parsed seek table, or <c>null</c> if <see cref="ReadStream"/> has not run successfully.</summary>
    public TtaSeekTable? SeekTable { get; private set; }

    /// <summary>Per-frame byte/sample ranges, in file order.</summary>
    public IReadOnlyList<TtaFrame> Frames => _frames;

    /// <summary>Standard frame length in samples: <c>256 * SampleRate / 245</c> per libtta's <c>MUL_FRAME_TIME</c>.</summary>
    public uint FrameLengthSamples =>
        Header is null ? 0u : (uint)(256UL * Header.SampleRate / 245UL);

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        var available = stream.Length - start;
        if (available < TtaHeader.FixedSize)
        {
            return false;
        }

        Span<byte> headerBuf = stackalloc byte[TtaHeader.FixedSize];
        if (stream.Read(headerBuf) != TtaHeader.FixedSize)
        {
            stream.Position = start;
            return false;
        }

        if (headerBuf[0] != (byte)'T' || headerBuf[1] != (byte)'T'
            || headerBuf[2] != (byte)'A' || headerBuf[3] != (byte)'1')
        {
            stream.Position = start;
            return false;
        }

        // Port: read_tta_header (libtta.c:449-471). Five LE info fields, then a 4-byte
        // CRC32 over the preceding 18 bytes (magic + info).
        var format       = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[4..6]);
        var nch          = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[6..8]);
        var bps          = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[8..10]);
        var sps          = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[10..14]);
        var totalSamples = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[14..18]);
        var headerCrc32  = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[18..22]);

        Header = new TtaHeader(format, nch, bps, sps, totalSamples, headerCrc32);

        // Reset the stream to start before constructing the source reader so the source
        // view's offset 0 maps to StartOffset (matches the WavPackStream / MpcStream pattern).
        stream.Position = start;
        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        // Position the stream just past the fixed header for the seek-table parse.
        stream.Position = start + TtaHeader.FixedSize;

        _frames.Clear();
        SeekTable = null;
        if (!ReadSeekTableAndBuildFrames(stream))
        {
            // Header parsed but the seek table couldn't be trusted; treat as a soft failure
            // for the seek-table state but report success for the header parse.
            _frames.Clear();
            SeekTable = null;
        }

        // Position the stream just past the audio so the outer scanner can pick up
        // any trailing ID3v1 / APEv2 footer.
        if (_frames.Count > 0)
        {
            var lastFrame = _frames[^1];
            stream.Position = lastFrame.StartOffset + lastFrame.Length;
        }
        else
        {
            stream.Position = EndOffset;
        }

        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Byte-passthrough: the parsed range is re-emitted verbatim from the live source.
    /// Mirrors the <see cref="Mp4Stream"/> / <see cref="MpcStream"/> / <see cref="WavPackStream"/>
    /// full-passthrough pattern. Tag mutations come back through <see cref="ReadStream"/> on
    /// a freshly tagged source produced by <c>AudioTags</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the source has been disposed or was never populated.
    /// </exception>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(DetachedSourceMessage);
        }

        // Source offsets are relative to StartOffset (i.e., StartOffset maps to offset 0
        // within the source view).
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

    /// <summary>
    /// Parses the per-frame seek table and builds the <see cref="Frames"/> list.
    /// Port of <c>tta_decoder_read_seek_table</c> (libtta.c:473-490) with frame-count
    /// math from <c>tta_decoder_init_set_info</c> (libtta.c:551-574).
    /// </summary>
    private bool ReadSeekTableAndBuildFrames(Stream stream)
    {
        if (Header is null || Header.SampleRate == 0)
        {
            return false;
        }

        // Frame count per libtta:
        //   flen_std  = 256 * sps / 245
        //   flen_last = total_samples % flen_std
        //   frames    = total_samples / flen_std + (flen_last ? 1 : 0)
        //   if (!flen_last) flen_last = flen_std;
        var flenStd = FrameLengthSamples;
        if (flenStd == 0)
        {
            return false;
        }

        var flenLast = Header.TotalSamples % flenStd;
        var frames = (Header.TotalSamples / flenStd) + (flenLast == 0 ? 0u : 1u);
        if (flenLast == 0)
        {
            flenLast = flenStd;
        }

        if (frames == 0)
        {
            SeekTable = new TtaSeekTable([], 0u);
            return true;
        }

        // Each entry is 4 bytes; trailing CRC32 is one more uint32.
        var seekTableBytes = checked((int)((frames + 1) * 4));
        if (stream.Length - stream.Position < seekTableBytes)
        {
            return false;
        }

        var buf = new byte[seekTableBytes];
        stream.ReadExactly(buf);

        var sizes = new uint[frames];
        for (var i = 0; i < frames; i++)
        {
            sizes[i] = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(i * 4, 4));
        }

        var crc = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan((int)frames * 4, 4));
        SeekTable = new TtaSeekTable(sizes, crc);

        // Build frame offsets. The first frame begins immediately after the seek table
        // (header end + (frames + 1) * 4 bytes); subsequent frames stack by their seek-table
        // entry length.
        var offset = StartOffset + TtaHeader.FixedSize + seekTableBytes;
        for (var i = 0; i < frames; i++)
        {
            var sampleCount = (i == frames - 1) ? flenLast : flenStd;
            _frames.Add(new TtaFrame(offset, sizes[i], sampleCount));
            offset += sizes[i];
        }

        return true;
    }
}
