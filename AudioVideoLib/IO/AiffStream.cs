namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Minimal structural walker for AIFF and AIFF-C containers. Does not decode audio samples.
/// </summary>
/// <remarks>
/// The walker captures the <c>COMM</c> chunk needed to report format parameters and locates the
/// <c>SSND</c> chunk, while recording every other chunk structurally without materialising its payload.
/// Sample rate is decoded from the 80-bit IEEE 754 extended-precision value stored by the AIFF spec.
/// </remarks>
public sealed class AiffStream : IAudioStream
{
    private const int MaxChunkCaptureSize = 8 * 1024;

    private readonly List<AiffChunk> _chunks = [];

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalAudioLength
    {
        get
        {
            if (SampleRate == 0 || Channels == 0)
            {
                return 0;
            }

            var seconds = SampleFrames / SampleRate;
            return (long)(seconds * 1000);
        }
    }

    /// <inheritdoc/>
    public long TotalAudioSize => SsndSize;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>
    /// Gets the chunks discovered inside the AIFF container, in file order.
    /// </summary>
    public IReadOnlyList<AiffChunk> Chunks => _chunks;

    /// <summary>
    /// Gets the four-character form type, either <c>"AIFF"</c> or <c>"AIFC"</c>.
    /// </summary>
    public string FormatType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the channel count from the <c>COMM</c> chunk.
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the total number of sample frames from the <c>COMM</c> chunk.
    /// </summary>
    public long SampleFrames { get; private set; }

    /// <summary>
    /// Gets the number of bits per sample from the <c>COMM</c> chunk.
    /// </summary>
    public int SampleSize { get; private set; }

    /// <summary>
    /// Gets the sample rate in Hz, decoded from the 80-bit IEEE 754 extended-precision value in the <c>COMM</c> chunk.
    /// </summary>
    public double SampleRate { get; private set; }

    /// <summary>
    /// Gets the four-character compression identifier from an AIFF-C <c>COMM</c> chunk, or <c>null</c> for plain AIFF.
    /// </summary>
    public string? Compression { get; private set; }

    /// <summary>
    /// Gets the absolute offset of the <c>SSND</c> chunk's payload, or 0 if the chunk is absent.
    /// </summary>
    public long SsndOffset { get; private set; }

    /// <summary>
    /// Gets the size of the <c>SSND</c> chunk's payload in bytes.
    /// </summary>
    public long SsndSize { get; private set; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

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

        if (!TryReadBeU32(stream, out var declaredSize))
        {
            return false;
        }

        var formType = ReadAscii(stream, 4);
        if (formType is not ("AIFF" or "AIFC"))
        {
            return false;
        }

        StartOffset = start;
        FormatType = formType;
        var containerEnd = Math.Min(start + 8 + declaredSize, stream.Length);

        while (stream.Position + 8 <= containerEnd)
        {
            var chunkStart = stream.Position;
            var id = ReadAscii(stream, 4);
            if (id.Length != 4 || !TryReadBeU32(stream, out var size))
            {
                break;
            }

            var dataStart = stream.Position;
            var dataEnd = Math.Min(dataStart + size, containerEnd);

            byte[] data = [];
            if (id is "COMM" && size is > 0 and < MaxChunkCaptureSize)
            {
                data = new byte[size];
                var read = stream.Read(data, 0, (int)size);
                if (read != size)
                {
                    _chunks.Add(new AiffChunk(id, chunkStart, dataStart + read, data.AsSpan(0, read).ToArray()));
                    break;
                }
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

    /// <inheritdoc/>
    public byte[] ToByteArray() => [];

    private static string ReadAscii(Stream s, int n)
    {
        var b = new byte[n];
        return s.Read(b, 0, n) != n ? string.Empty : Encoding.ASCII.GetString(b);
    }

    private static bool TryReadBeU32(Stream s, out int value)
    {
        var b = new byte[4];
        if (s.Read(b, 0, 4) != 4)
        {
            value = 0;
            return false;
        }

        value = ReadBeU32(b, 0);
        return true;
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
            _ => (double)sign * mantissa * Math.Pow(2, exponent - 16383 - 63),
        };
    }
}
