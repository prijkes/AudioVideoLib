namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Minimal structural walker for RIFF containers (e.g. WAV and RIFX). Does not decode audio samples.
/// </summary>
/// <remarks>
/// The walker captures the <c>fmt </c> and <c>data</c> chunks needed to report format parameters and
/// locate the sample block, and records every other chunk structurally without materialising its payload.
/// Only the <c>WAVE</c> form type is recognised; other RIFF types such as AVI are ignored.
/// </remarks>
public sealed class RiffStream : IAudioStream
{
    private const int MaxChunkCaptureSize = 64 * 1024;

    private readonly List<RiffChunk> _chunks = [];

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalAudioLength
    {
        get
        {
            if (SampleRate == 0 || BitsPerSample == 0 || Channels == 0)
            {
                return 0;
            }

            var bytesPerSecond = (long)SampleRate * Channels * (BitsPerSample / 8);
            return bytesPerSecond == 0 ? 0 : DataSize * 1000 / bytesPerSecond;
        }
    }

    /// <inheritdoc/>
    public long TotalAudioSize => DataSize;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>
    /// Gets the chunks discovered inside the RIFF container, in file order.
    /// </summary>
    public IReadOnlyList<RiffChunk> Chunks => _chunks;

    /// <summary>
    /// Gets the four-character RIFF form type. Only <c>"WAVE"</c> is currently recognised.
    /// </summary>
    public string FormatType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the WAV <c>wFormatTag</c> value from the <c>fmt </c> chunk (e.g. 1 = PCM, 3 = IEEE float).
    /// </summary>
    public int AudioFormat { get; private set; }

    /// <summary>
    /// Gets the channel count from the <c>fmt </c> chunk.
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the sample rate from the <c>fmt </c> chunk, in Hz.
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Gets the average byte rate from the <c>fmt </c> chunk.
    /// </summary>
    public int ByteRate { get; private set; }

    /// <summary>
    /// Gets the block alignment from the <c>fmt </c> chunk.
    /// </summary>
    public int BlockAlign { get; private set; }

    /// <summary>
    /// Gets the bits-per-sample value from the <c>fmt </c> chunk.
    /// </summary>
    public int BitsPerSample { get; private set; }

    /// <summary>
    /// Gets the absolute offset of the <c>data</c> chunk's payload, or 0 if the chunk is absent.
    /// </summary>
    public long DataOffset { get; private set; }

    /// <summary>
    /// Gets the size of the <c>data</c> chunk's payload in bytes.
    /// </summary>
    public long DataSize { get; private set; }

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
        if (magic is not "RIFF" and not "RIFX")
        {
            return false;
        }

        var bigEndian = magic == "RIFX";
        if (!TryReadU32(stream, bigEndian, out var declaredSize))
        {
            return false;
        }

        var formType = ReadAscii(stream, 4);
        if (formType != "WAVE")
        {
            // Other RIFF types exist (AVI, etc.) but for our inspector we restrict to WAV for now.
            return false;
        }

        StartOffset = start;
        FormatType = formType;

        // Chunks begin at start+12. Total container size is declaredSize+8 (fields after "RIFF").
        var containerEnd = Math.Min(start + 8 + declaredSize, stream.Length);
        while (stream.Position + 8 <= containerEnd)
        {
            var chunkStart = stream.Position;
            var id = ReadAscii(stream, 4);
            if (id.Length != 4 || !TryReadU32(stream, bigEndian, out var size))
            {
                break;
            }

            var dataStart = stream.Position;
            var dataEnd = Math.Min(dataStart + size, containerEnd);

            byte[] data = [];
            if (id is "fmt " or "LIST" && size is > 0 and < MaxChunkCaptureSize)
            {
                data = new byte[size];
                var read = stream.Read(data, 0, (int)size);
                if (read != size)
                {
                    // Truncated chunk — record what we got and stop walking.
                    _chunks.Add(new RiffChunk(id, chunkStart, dataStart + read, data.AsSpan(0, read).ToArray()));
                    break;
                }
            }
            else
            {
                stream.Position = dataEnd;
            }

            _chunks.Add(new RiffChunk(id, chunkStart, dataEnd, data));

            if (id == "fmt " && data.Length >= 16)
            {
                AudioFormat = bigEndian ? ReadBeU16(data, 0) : ReadLeU16(data, 0);
                Channels = bigEndian ? ReadBeU16(data, 2) : ReadLeU16(data, 2);
                SampleRate = bigEndian ? ReadBeU32(data, 4) : (int)ReadLeU32(data, 4);
                ByteRate = bigEndian ? ReadBeU32(data, 8) : (int)ReadLeU32(data, 8);
                BlockAlign = bigEndian ? ReadBeU16(data, 12) : ReadLeU16(data, 12);
                BitsPerSample = bigEndian ? ReadBeU16(data, 14) : ReadLeU16(data, 14);
            }
            else if (id == "data")
            {
                DataOffset = dataStart;
                DataSize = size;
            }

            // Word-align: chunks are padded to even length.
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

    private static bool TryReadU32(Stream s, bool bigEndian, out int value)
    {
        var b = new byte[4];
        if (s.Read(b, 0, 4) != 4)
        {
            value = 0;
            return false;
        }

        value = bigEndian ? ReadBeU32(b, 0) : (int)ReadLeU32(b, 0);
        return true;
    }

    private static int ReadLeU16(byte[] b, int off) => b[off] | (b[off + 1] << 8);

    private static uint ReadLeU32(byte[] b, int off) =>
        (uint)(b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24));

    private static int ReadBeU16(byte[] b, int off) => (b[off] << 8) | b[off + 1];

    private static int ReadBeU32(byte[] b, int off) => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];
}
