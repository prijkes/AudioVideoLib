namespace AudioVideoLib.IO;

using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Walks OGG pages structurally. Does not decode Vorbis/Opus/Theora payload.
/// </summary>
public sealed class OggStream : IAudioStream
{
    private readonly List<OggPage> _pages = [];

    public long StartOffset { get; private set; }

    public long EndOffset { get; private set; }

    public long TotalAudioLength => 0;

    public long TotalAudioSize => EndOffset - StartOffset;

    public int MaxFrameSpacingLength { get; set; } = 0;

    public IReadOnlyList<OggPage> Pages => _pages;

    public int PageCount => _pages.Count;

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

    public bool ReadStream(Stream stream)
    {
        var start = stream.Position;
        var magic = new byte[4];
        if (stream.Read(magic, 0, 4) != 4 || Encoding.ASCII.GetString(magic) != "OggS")
        {
            return false;
        }

        stream.Position = start;
        StartOffset = start;

        // Max page budget to avoid pathological files locking the inspector.
        const int MaxPages = 100_000;
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

            stream.Position += payloadSize;
            var pageEnd = stream.Position;

            _pages.Add(new OggPage(pageStart, pageEnd, version, flags, granule, serial, seq, checksum, segCount, payloadSize));
            pagesParsed++;

            if ((flags & 0x04) != 0 && pagesParsed > 2)
            {
                // End-of-stream flag; keep scanning if more bitstreams follow, but stop if we've already read enough.
            }
        }

        EndOffset = stream.Position;
        return _pages.Count > 0;
    }

    public byte[] ToByteArray() => [];

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
