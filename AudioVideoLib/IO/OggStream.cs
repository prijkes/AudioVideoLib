namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Walks an Ogg bitstream (RFC 3533) page-by-page. Does not decode Vorbis/Opus/Theora payload.
/// </summary>
/// <remarks>
/// The walker records structural metadata for every page and peeks at the first beginning-of-stream
/// page to identify the codec (Vorbis or Opus) so it can report channel count, sample rate, and an
/// approximate duration derived from the largest observed granule position.
/// </remarks>
public sealed class OggStream : IMediaContainer
{
    private const int MaxPages = 100_000;
    private const int CodecIdentificationPeekSize = 64;

    private readonly List<OggPage> _pages = [];

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration
    {
        get
        {
            if (SampleRate == 0)
            {
                return 0;
            }

            // Opus granule is counted at 48 kHz regardless of the input sample rate.
            var granuleRate = Codec == "opus" ? 48000 : SampleRate;
            return granuleRate == 0 ? 0 : TotalGranulePosition * 1000 / granuleRate;
        }
    }

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>
    /// Gets the pages discovered inside the Ogg bitstream, in file order.
    /// </summary>
    public IReadOnlyList<OggPage> Pages => _pages;

    /// <summary>
    /// Gets the total number of pages in the bitstream.
    /// </summary>
    public int PageCount => _pages.Count;

    /// <summary>
    /// Gets the largest granule position seen across all pages — used to estimate duration.
    /// </summary>
    public long TotalGranulePosition
    {
        get
        {
            long max = 0;
            foreach (var p in _pages)
            {
                if (p.GranulePosition > max)
                {
                    max = p.GranulePosition;
                }
            }

            return max;
        }
    }

    /// <summary>
    /// Gets the identified codec for the first logical bitstream: <c>"vorbis"</c>, <c>"opus"</c>, or
    /// <see cref="string.Empty"/> if the codec could not be identified.
    /// </summary>
    public string Codec { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the channel count reported by the codec identification header, or 0 if unidentified.
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the input sample rate in Hz reported by the codec identification header, or 0 if unidentified.
    /// </summary>
    /// <remarks>
    /// For Opus this is the original sample rate; Opus always encodes at 48 kHz internally, which is
    /// what <see cref="TotalDuration"/> uses for duration calculation.
    /// </remarks>
    public int SampleRate { get; private set; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        var magic = new byte[4];
        if (stream.Read(magic, 0, 4) != 4 || Encoding.ASCII.GetString(magic) != "OggS")
        {
            stream.Position = start;
            return false;
        }

        stream.Position = start;
        StartOffset = start;

        var pagesParsed = 0;
        while (stream.Position + 27 <= stream.Length && pagesParsed < MaxPages)
        {
            var pageStart = stream.Position;
            var header = new byte[27];
            if (stream.Read(header, 0, 27) != 27)
            {
                break;
            }

            if (header[0] != (byte)'O' || header[1] != (byte)'g' || header[2] != (byte)'g' || header[3] != (byte)'S')
            {
                // Not a page start; bail out — the stream is either done or malformed.
                stream.Position = pageStart;
                break;
            }

            var version = header[4];
            var flags = header[5];
            var granule = ReadLeI64(header, 6);
            var serial = ReadLeI32(header, 14);
            var seq = ReadLeI32(header, 18);
            var checksum = ReadLeI32(header, 22);
            var segCount = header[26];

            if (stream.Position + segCount > stream.Length)
            {
                break;
            }

            var segTable = new byte[segCount];
            if (stream.Read(segTable, 0, segCount) != segCount)
            {
                break;
            }

            var payloadSize = 0;
            foreach (var b in segTable)
            {
                payloadSize += b;
            }

            if (stream.Position + payloadSize > stream.Length)
            {
                break;
            }

            var payloadStart = stream.Position;
            if (Codec.Length == 0 && (flags & 0x02) != 0 && payloadSize > 0)
            {
                var peekLen = Math.Min(payloadSize, CodecIdentificationPeekSize);
                var peek = new byte[peekLen];
                if (stream.Read(peek, 0, peekLen) == peekLen)
                {
                    TryIdentifyCodec(peek);
                }
            }

            stream.Position = payloadStart + payloadSize;
            var pageEnd = stream.Position;

            _pages.Add(new OggPage(pageStart, pageEnd, version, flags, granule, serial, seq, checksum, segCount, payloadSize));
            pagesParsed++;
        }

        EndOffset = stream.Position;
        return _pages.Count > 0;
    }

    /// <inheritdoc/>
    public byte[] ToByteArray() => [];

    private void TryIdentifyCodec(byte[] header)
    {
        // Vorbis identification header: packet type 0x01, then "vorbis" magic.
        // Layout: [0]=0x01, [1..6]="vorbis", [7..10]=version, [11]=channels, [12..15]=sample rate (LE).
        if (header.Length >= 16
            && header[0] == 0x01
            && header[1] == (byte)'v' && header[2] == (byte)'o' && header[3] == (byte)'r'
            && header[4] == (byte)'b' && header[5] == (byte)'i' && header[6] == (byte)'s')
        {
            Codec = "vorbis";
            Channels = header[11];
            SampleRate = header[12] | (header[13] << 8) | (header[14] << 16) | (header[15] << 24);
            return;
        }

        // Opus identification header: "OpusHead" magic, then version, channels, pre-skip, input sample rate (LE).
        // Layout: [0..7]="OpusHead", [8]=version, [9]=channels, [10..11]=pre-skip, [12..15]=input sample rate.
        if (header.Length >= 16
            && header[0] == (byte)'O' && header[1] == (byte)'p' && header[2] == (byte)'u' && header[3] == (byte)'s'
            && header[4] == (byte)'H' && header[5] == (byte)'e' && header[6] == (byte)'a' && header[7] == (byte)'d')
        {
            Codec = "opus";
            Channels = header[9];
            SampleRate = header[12] | (header[13] << 8) | (header[14] << 16) | (header[15] << 24);
        }
    }

    private static long ReadLeI64(byte[] b, int off)
    {
        long v = 0;
        for (var i = 7; i >= 0; i--)
        {
            v = (v << 8) | b[off + i];
        }

        return v;
    }

    private static int ReadLeI32(byte[] b, int off) =>
        b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24);
}
