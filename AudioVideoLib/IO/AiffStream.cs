namespace AudioVideoLib.IO;

using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Minimal structural walker for AIFF / AIFF-C files.
/// </summary>
public sealed class AiffStream : IAudioStream
{
    private readonly List<AiffChunk> _chunks = [];

    public long StartOffset { get; private set; }

    public long EndOffset { get; private set; }

    public long TotalAudioLength
    {
        get
        {
            if (SampleRate == 0 || Channels == 0)
            {
                return 0;
            }

            var seconds = (double)SampleFrames / SampleRate;
            return (long)(seconds * 1000);
        }
    }

    public long TotalAudioSize => SsndSize;

    public int MaxFrameSpacingLength { get; set; } = 0;

    public IReadOnlyList<AiffChunk> Chunks => _chunks;

    public string FormatType { get; private set; } = string.Empty;

    public int Channels { get; private set; }

    public long SampleFrames { get; private set; }

    public int SampleSize { get; private set; }

    public double SampleRate { get; private set; }

    public string? Compression { get; private set; }

    public long SsndOffset { get; private set; }

    public long SsndSize { get; private set; }

    public bool ReadStream(Stream stream)
    {
        var start = stream.Position;
        if (stream.Length - start < 12)
        {
            return false;
        }

        var magic = ReadAscii(stream, 4);
        if (magic != "FORM")
        {
            return false;
        }

        var declaredSize = ReadBeU32(stream);
        var formType = ReadAscii(stream, 4);
        if (formType is not ("AIFF" or "AIFC"))
        {
            return false;
        }

        StartOffset = start;
        FormatType = formType;
        var containerEnd = System.Math.Min(start + 8 + declaredSize, stream.Length);

        while (stream.Position + 8 <= containerEnd)
        {
            var chunkStart = stream.Position;
            var id = ReadAscii(stream, 4);
            var size = ReadBeU32(stream);
            var dataStart = stream.Position;
            var dataEnd = System.Math.Min(dataStart + size, containerEnd);

            byte[] data = [];
            if (id is "COMM" && size is > 0 and < 1024 * 8)
            {
                data = new byte[size];
                stream.ReadExactly(data, 0, (int)size);
            }
            else
            {
                stream.Position = dataEnd;
            }

            _chunks.Add(new AiffChunk(id, chunkStart, dataEnd, data));

            if (id == "COMM" && data.Length >= 18)
            {
                Channels = ReadBeU16(data, 0);
                SampleFrames = ReadBeU32(data, 2);
                SampleSize = ReadBeU16(data, 6);
                SampleRate = ReadIeee80(data, 8);
                if (formType == "AIFC" && data.Length >= 22)
                {
                    Compression = Encoding.ASCII.GetString(data, 18, 4);
                }
            }
            else if (id == "SSND")
            {
                SsndOffset = dataStart;
                SsndSize = size;
            }

            if ((size & 1) != 0 && stream.Position < containerEnd)
            {
                stream.Position++;
            }
        }

        EndOffset = stream.Position;
        return _chunks.Count > 0;
    }

    public byte[] ToByteArray() => [];

    private static string ReadAscii(Stream s, int n)
    {
        var b = new byte[n];
        return s.Read(b, 0, n) != n ? string.Empty : Encoding.ASCII.GetString(b);
    }

    private static int ReadBeU32(Stream s)
    {
        var b = new byte[4];
        return s.Read(b, 0, 4) != 4 ? 0 : ReadBeU32(b, 0);
    }

    private static int ReadBeU16(byte[] b, int off) => (b[off] << 8) | b[off + 1];

    private static int ReadBeU32(byte[] b, int off) => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];

    // IEEE 754 80-bit extended precision (Apple SANE format) → double.
    private static double ReadIeee80(byte[] b, int off)
    {
        if (b.Length < off + 10)
        {
            return 0;
        }

        var signAndExp = (b[off] << 8) | b[off + 1];
        var sign = (signAndExp & 0x8000) != 0 ? -1 : 1;
        var exponent = signAndExp & 0x7FFF;
        var mantissa = 0UL;
        for (var i = 0; i < 8; i++)
        {
            mantissa = (mantissa << 8) | b[off + 2 + i];
        }

        return exponent switch
        {
            0 when mantissa == 0 => 0,
            0x7FFF => double.NaN,
            _ => (double)sign * mantissa * System.Math.Pow(2, exponent - 16383 - 63),
        };
    }
}
