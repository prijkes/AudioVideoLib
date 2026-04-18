namespace AudioVideoLib.IO;

using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Minimal structural walker for RIFF / WAV files. Does not decode audio samples.
/// </summary>
public sealed class RiffStream : IAudioStream
{
    private readonly List<RiffChunk> _chunks = [];

    public long StartOffset { get; private set; }

    public long EndOffset { get; private set; }

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

    public long TotalAudioSize => DataSize;

    public int MaxFrameSpacingLength { get; set; } = 0;

    public IReadOnlyList<RiffChunk> Chunks => _chunks;

    public string FormatType { get; private set; } = string.Empty;

    public int AudioFormat { get; private set; }

    public int Channels { get; private set; }

    public int SampleRate { get; private set; }

    public int ByteRate { get; private set; }

    public int BlockAlign { get; private set; }

    public int BitsPerSample { get; private set; }

    public long DataOffset { get; private set; }

    public long DataSize { get; private set; }

    public bool ReadStream(Stream stream)
    {
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
        var declaredSize = ReadU32(stream, bigEndian);
        var formType = ReadAscii(stream, 4);
        if (formType != "WAVE")
        {
            // Other RIFF types exist (AVI, etc.) but for our inspector we restrict to WAV for now.
            return false;
        }

        StartOffset = start;
        FormatType = formType;

        // Chunks begin at start+12. Total container size is declaredSize+8 (fields after "RIFF").
        var containerEnd = System.Math.Min(start + 8 + declaredSize, stream.Length);
        while (stream.Position + 8 <= containerEnd)
        {
            var chunkStart = stream.Position;
            var id = ReadAscii(stream, 4);
            var size = ReadU32(stream, bigEndian);
            var dataStart = stream.Position;
            var dataEnd = System.Math.Min(dataStart + size, containerEnd);

            byte[] data = [];
            if (id is "fmt " or "LIST" && size is > 0 and < 1024 * 64)
            {
                data = new byte[size];
                stream.ReadExactly(data, 0, (int)size);
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

    public byte[] ToByteArray() => [];

    private static string ReadAscii(Stream s, int n)
    {
        var b = new byte[n];
        return s.Read(b, 0, n) != n ? string.Empty : Encoding.ASCII.GetString(b);
    }

    private static int ReadU32(Stream s, bool bigEndian)
    {
        var b = new byte[4];
        return s.Read(b, 0, 4) != 4
            ? 0
            : bigEndian ? ReadBeU32(b, 0) : (int)ReadLeU32(b, 0);
    }

    private static int ReadLeU16(byte[] b, int off) => b[off] | (b[off + 1] << 8);

    private static uint ReadLeU32(byte[] b, int off) =>
        (uint)(b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24));

    private static int ReadBeU16(byte[] b, int off) => (b[off] << 8) | b[off + 1];

    private static int ReadBeU32(byte[] b, int off) => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];
}
