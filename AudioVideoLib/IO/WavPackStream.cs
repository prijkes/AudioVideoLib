namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Structural walker for WavPack (<c>.wv</c>) files. Enumerates blocks and the
/// metadata sub-blocks within each block; <see cref="WriteTo"/> streams the
/// original bytes through unchanged (no audio re-encode).
/// </summary>
/// <remarks>
/// Reference: <c>3rdparty/WavPack/include/wavpack.h</c> for layout constants;
/// <c>3rdparty/WavPack/src/open_utils.c:read_next_header</c> (lines 951-984) for the
/// header validation rule; <c>read_metadata_buff</c> (lines 713-754) for sub-block parsing.
/// <para />
/// Hybrid mode: a <c>.wv</c> file may interleave correction-stream blocks if the file
/// was muxed that way; the walker treats each <c>wvpk</c> block uniformly, regardless
/// of whether its <c>ID_WVC_BITSTREAM</c> sub-block carries lossless residuals.
/// Multi-file hybrid (<c>.wvc</c> companion file) is out of scope per spec §4.2.
/// </remarks>
public sealed class WavPackStream : IMediaContainer, IDisposable
{
    private readonly List<WavPackBlock> _blocks = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration
    {
        get
        {
            if (_blocks.Count == 0)
            {
                return 0;
            }

            var first = _blocks[0].Header;
            var rate = first.SampleRate;
            if (rate <= 0)
            {
                return 0;
            }

            var total = first.TotalSamples;
            return total < 0 ? 0 : total * 1000L / rate;
        }
    }

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>Gets the WavPack blocks discovered in the input, in file order.</summary>
    public IReadOnlyList<WavPackBlock> Blocks => _blocks;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        var totalLen = stream.Length;
        if (totalLen - start < WavPackBlockHeader.Size)
        {
            return false;
        }

        // Magic check at offset 0 (relative to the caller's stream position).
        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4 ||
            magic[0] != (byte)'w' || magic[1] != (byte)'v' ||
            magic[2] != (byte)'p' || magic[3] != (byte)'k')
        {
            stream.Position = start;
            return false;
        }

        stream.Position = start;

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        _blocks.Clear();

        // pos is relative to StartOffset (i.e., offset 0 in the source view).
        var pos = 0L;
        var sourceLength = _source.Length;
        Span<byte> hdrBytes = stackalloc byte[WavPackBlockHeader.Size];
        while (pos + WavPackBlockHeader.Size <= sourceLength)
        {
            _source.Read(pos, hdrBytes);

            var header = WavPackBlockHeader.Parse(hdrBytes);
            if (header is null)
            {
                // No valid wvpk magic at the expected offset — stop. The library does
                // not currently support resync-on-garbage; tags / trailing data live
                // outside the wvpk block stream and are handled by AudioTags upstream.
                break;
            }

            var blockLength = (long)header.CkSize + 8L;
            if (blockLength < WavPackBlockHeader.Size || pos + blockLength > sourceLength)
            {
                break;
            }

            var subBlocks = ParseSubBlocks(pos + WavPackBlockHeader.Size, (int)(blockLength - WavPackBlockHeader.Size));
            _blocks.Add(new WavPackBlock(header, StartOffset + pos, blockLength, subBlocks));

            pos += blockLength;
        }

        // Position the caller's stream just past the last decoded block so an outer scanner can continue.
        var consumed = _blocks.Count > 0
            ? _blocks[^1].StartOffset + _blocks[^1].Length
            : start;
        stream.Position = consumed;
        EndOffset = consumed;

        return _blocks.Count > 0;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Byte-passthrough: the parsed range is re-emitted verbatim from the live source. Tag
    /// mutations come back through <see cref="ReadStream"/> on a freshly tagged source produced
    /// by <c>AudioTags</c>. There is no metadata block to re-emit at the WavPack level —
    /// APEv2 / ID3v1 footers live outside the wvpk block stream.
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
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        if (_blocks.Count == 0)
        {
            return;
        }

        var first = _blocks[0];
        var last = _blocks[^1];
        var length = last.StartOffset + last.Length - first.StartOffset;
        _source.CopyTo(first.StartOffset - StartOffset, length, destination);
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
    /// Walks the metadata sub-blocks packed inside a single WavPack block's post-header payload.
    /// Port of <c>3rdparty/WavPack/src/open_utils.c:read_metadata_buff</c> (lines 713-754).
    /// </summary>
    /// <param name="payloadStart">Source-relative offset of the first sub-block byte.</param>
    /// <param name="payloadLength">Total bytes of sub-block area (block length minus 32-byte header).</param>
    /// <returns>Sub-block summaries in file order; empty list on truncated / malformed input.</returns>
    private List<WavPackSubBlock> ParseSubBlocks(long payloadStart, int payloadLength)
    {
        var subs = new List<WavPackSubBlock>();
        if (_source is null || payloadLength <= 0)
        {
            return subs;
        }

        var span = new byte[payloadLength];
        _source.Read(payloadStart, span);

        var i = 0;
        while (i + 2 <= span.Length)
        {
            var rawId = span[i];
            i++;

            // Size header: 8-bit by default (in 16-bit "words"); 24-bit when ID_LARGE is set.
            var wordLen = (int)span[i];
            i++;

            if ((rawId & 0x80) != 0)
            {
                if (i + 2 > span.Length)
                {
                    break;
                }

                wordLen += span[i] << 8;
                wordLen += span[i + 1] << 16;
                i += 2;
            }

            // Logical size in bytes is words << 1 (i.e., the "size doubled" effect of word units).
            var byteLen = wordLen << 1;

            // ID_ODD_SIZE: subtract 1 byte from the logical length but keep the on-disk
            // padding (a stored even-aligned length).
            var hadOddSize = (rawId & 0x40) != 0;
            if (hadOddSize)
            {
                if (byteLen == 0)
                {
                    break;
                }

                byteLen--;
            }

            var onDiskLen = byteLen + (byteLen & 1);
            if (i + onDiskLen > span.Length)
            {
                break;
            }

            subs.Add(new WavPackSubBlock(_source, rawId, payloadStart + i, byteLen));
            i += onDiskLen;
        }

        return subs;
    }
}
