namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// Minimal structural walker for Philips DFF (Direct Stream Digital Interchange File Format) DSD audio
/// containers (<c>.dff</c>). Does not decode audio samples.
/// </summary>
/// <remarks>
/// The walker captures top-level chunks inside the <c>FRM8</c> form (form-type <c>"DSD "</c>), descends
/// into the <c>PROP</c> form to extract <c>FS </c> (sample rate) and <c>CHNL</c> (channel count), and
/// locates optional <c>DIIN</c>, <c>COMT</c>, and <c>ID3 </c> chunks. All multi-byte integers in DFF are
/// big-endian.
/// </remarks>
public sealed class DffStream : IMediaContainer
{
    private const long MaxAllowedFormSize = 1L << 40;
    private const int MaxChunkCaptureSize = 64 * 1024;

    private readonly List<DsdChunk> _chunks = [];

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration => SampleRate == 0 ? 0 : SampleCount * 1000L / SampleRate;

    /// <inheritdoc/>
    public long TotalMediaSize => DataSize;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>
    /// Gets the top-level chunks discovered inside the <c>FRM8</c> form, in file order.
    /// </summary>
    public IReadOnlyList<DsdChunk> Chunks => _chunks;

    /// <summary>
    /// Gets the form-type four-CC (always <c>"DSD "</c> for a recognised DFF file).
    /// </summary>
    public string FormatType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the format version from the <c>FVER</c> chunk (high byte = major).
    /// </summary>
    public uint FormatVersion { get; private set; }

    /// <summary>
    /// Gets the sample rate from <c>PROP/FS </c>, in Hz.
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Gets the channel count from <c>PROP/CHNL</c>.
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the channel identifiers from <c>PROP/CHNL</c>, one four-CC per channel.
    /// </summary>
    public IReadOnlyList<string> ChannelIds { get; private set; } = [];

    /// <summary>
    /// Gets the bits-per-sample value (DSD is always 1).
    /// </summary>
    public int BitsPerSample => 1;

    /// <summary>
    /// Gets the per-channel sample count, computed from the <c>DSD </c> audio chunk size and channel count.
    /// </summary>
    public long SampleCount { get; private set; }

    /// <summary>
    /// Gets the absolute offset of the <c>DSD </c> audio chunk's payload, or 0 if the chunk is absent.
    /// </summary>
    public long DataOffset { get; private set; }

    /// <summary>
    /// Gets the size of the <c>DSD </c> audio chunk's payload in bytes.
    /// </summary>
    public long DataSize { get; private set; }

    /// <summary>
    /// Gets the raw payload of the optional <c>DIIN</c> info chunk, or <c>null</c> when absent.
    /// </summary>
    public byte[]? DiinChunk { get; private set; }

    /// <summary>
    /// Gets the raw payload of the optional <c>COMT</c> comments chunk, or <c>null</c> when absent.
    /// </summary>
    public byte[]? ComtChunk { get; private set; }

    /// <summary>
    /// Gets the embedded ID3v2 tag from the optional <c>ID3 </c> chunk, or <c>null</c> when absent or unparseable.
    /// </summary>
    public Id3v2Tag? EmbeddedId3v2 { get; private set; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < 16)
        {
            return false;
        }

        var magic = ReadAscii(stream, 4);
        if (magic != "FRM8")
        {
            return false;
        }

        if (!TryReadBeU64(stream, out var formSize))
        {
            return false;
        }

        if (formSize > (ulong)MaxAllowedFormSize)
        {
            return false;
        }

        var formType = ReadAscii(stream, 4);
        if (formType != "DSD ")
        {
            return false;
        }

        StartOffset = start;
        FormatType = formType;

        // FRM8 chunk body starts after the 12-byte header. formSize includes the 4-byte form-type plus subchunks.
        var containerEnd = Math.Min(start + 12 + (long)formSize, stream.Length);

        while (stream.Position + 12 <= containerEnd)
        {
            var chunkStart = stream.Position;
            var id = ReadAscii(stream, 4);
            if (id.Length != 4 || !TryReadBeU64(stream, out var size))
            {
                break;
            }

            var dataStart = stream.Position;
            var declaredEnd = dataStart + (long)size;
            var dataEnd = Math.Min(declaredEnd, containerEnd);

            byte[] payload = [];
            var capture = id is "FVER" or "PROP" or "DIIN" or "COMT" or "ID3 ";
            if (capture && size is > 0 and < (ulong)MaxChunkCaptureSize)
            {
                payload = new byte[size];
                var read = stream.Read(payload, 0, (int)size);
                if (read != (int)size)
                {
                    _chunks.Add(new DsdChunk(id, chunkStart, dataStart + read, payload.AsSpan(0, read).ToArray()));
                    break;
                }
            }
            else
            {
                stream.Position = dataEnd;
            }

            _chunks.Add(new DsdChunk(id, chunkStart, dataEnd, payload));

            switch (id)
            {
                case "FVER" when payload.Length >= 4:
                    FormatVersion = ReadBeU32(payload, 0);
                    break;
                case "PROP":
                    ParseProp(payload);
                    break;
                case "DSD ":
                    DataOffset = dataStart;
                    DataSize = (long)size;
                    if (Channels > 0)
                    {
                        SampleCount = DataSize * 8L / Channels;
                    }

                    break;
                case "DIIN":
                    DiinChunk = payload;
                    break;
                case "COMT":
                    ComtChunk = payload;
                    break;
                case "ID3 ":
                    TryReadId3(payload);
                    break;
            }

            // DFF chunks are padded to even length.
            if (((long)size & 1L) != 0 && stream.Position < containerEnd)
            {
                stream.Position++;
            }
        }

        EndOffset = stream.Position;
        return _chunks.Count > 0;
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = ToByteArray();
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Buffer-shaped override: kept as the fast path for callers who want bytes in hand.
    /// </remarks>
    public byte[] ToByteArray()
    {
        if (_chunks.Count == 0)
        {
            return [];
        }

        using var body = new MemoryStream();
        // form-type
        body.Write(Encoding.ASCII.GetBytes("DSD "), 0, 4);
        foreach (var chunk in _chunks)
        {
            WriteSubChunk(body, chunk);
        }

        var bodyBytes = body.ToArray();
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("FRM8"), 0, 4);
        WriteBeU64(ms, (ulong)bodyBytes.Length);
        ms.Write(bodyBytes, 0, bodyBytes.Length);
        return ms.ToArray();
    }

    private static void WriteSubChunk(Stream s, DsdChunk chunk)
    {
        var idBytes = Encoding.ASCII.GetBytes(chunk.Id.PadRight(4)[..4]);
        s.Write(idBytes, 0, 4);

        // For unmaterialised chunks (audio "DSD "), Data is empty but Size still holds 12+payload total.
        // We re-emit the declared size from EndOffset-StartOffset minus the 12-byte header.
        var declaredPayloadSize = chunk.Data.Length > 0
            ? chunk.Data.Length
            : (int)Math.Max(0, chunk.Size - 12);

        WriteBeU64(s, (ulong)declaredPayloadSize);

        if (chunk.Data.Length > 0)
        {
            s.Write(chunk.Data, 0, chunk.Data.Length);
        }
        else if (declaredPayloadSize > 0)
        {
            var zeros = new byte[Math.Min(declaredPayloadSize, 4096)];
            var remaining = declaredPayloadSize;
            while (remaining > 0)
            {
                var step = Math.Min(remaining, zeros.Length);
                s.Write(zeros, 0, step);
                remaining -= step;
            }
        }

        if ((declaredPayloadSize & 1) != 0)
        {
            s.WriteByte(0);
        }
    }

    private void ParseProp(byte[] payload)
    {
        if (payload.Length < 4)
        {
            return;
        }

        var propType = Encoding.ASCII.GetString(payload, 0, 4);
        if (propType != "SND ")
        {
            // Spec says PROP form-type is always "SND " but we tolerate other values by trying to parse subchunks anyway.
        }

        var pos = 4;
        while (pos + 12 <= payload.Length)
        {
            var id = Encoding.ASCII.GetString(payload, pos, 4);
            var size = ReadBeU64(payload, pos + 4);
            pos += 12;

            if (size > (ulong)(payload.Length - pos))
            {
                return;
            }

            var subPayloadEnd = pos + (int)size;

            switch (id)
            {
                case "FS  " when size >= 4:
                    SampleRate = (int)ReadBeU32(payload, pos);
                    break;
                case "CHNL" when size >= 2:
                    Channels = (payload[pos] << 8) | payload[pos + 1];
                    var idsStart = pos + 2;
                    var idsAvailable = (int)size - 2;
                    var idCount = Math.Min(Channels, idsAvailable / 4);
                    var ids = new string[idCount];
                    for (var i = 0; i < idCount; i++)
                    {
                        ids[i] = Encoding.ASCII.GetString(payload, idsStart + (i * 4), 4);
                    }

                    ChannelIds = ids;
                    break;
            }

            pos = subPayloadEnd;
            if (((int)size & 1) != 0 && pos < payload.Length)
            {
                pos++;
            }
        }
    }

    private void TryReadId3(byte[] payload)
    {
        if (payload.Length == 0)
        {
            return;
        }

        try
        {
            using var ms = new MemoryStream(payload);
            var offset = new Id3v2TagReader().ReadFromStream(ms, TagOrigin.Start);
            if (offset is not null && offset.AudioTag is Id3v2Tag id3)
            {
                EmbeddedId3v2 = id3;
            }
        }
        catch (IOException)
        {
            // Tolerate malformed metadata.
        }
        catch (InvalidOperationException)
        {
            // Tolerate malformed metadata.
        }
    }

    private static string ReadAscii(Stream s, int n)
    {
        var b = new byte[n];
        return s.Read(b, 0, n) != n ? string.Empty : Encoding.ASCII.GetString(b);
    }

    private static bool TryReadBeU64(Stream s, out ulong value)
    {
        var b = new byte[8];
        if (s.Read(b, 0, 8) != 8)
        {
            value = 0;
            return false;
        }

        value = ReadBeU64(b, 0);
        return true;
    }

    private static ulong ReadBeU64(byte[] b, int off) =>
        ((ulong)b[off] << 56)
        | ((ulong)b[off + 1] << 48)
        | ((ulong)b[off + 2] << 40)
        | ((ulong)b[off + 3] << 32)
        | ((ulong)b[off + 4] << 24)
        | ((ulong)b[off + 5] << 16)
        | ((ulong)b[off + 6] << 8)
        | b[off + 7];

    private static uint ReadBeU32(byte[] b, int off) =>
        ((uint)b[off] << 24) | ((uint)b[off + 1] << 16) | ((uint)b[off + 2] << 8) | b[off + 3];

    private static void WriteBeU64(Stream s, ulong value)
    {
        s.WriteByte((byte)((value >> 56) & 0xFF));
        s.WriteByte((byte)((value >> 48) & 0xFF));
        s.WriteByte((byte)((value >> 40) & 0xFF));
        s.WriteByte((byte)((value >> 32) & 0xFF));
        s.WriteByte((byte)((value >> 24) & 0xFF));
        s.WriteByte((byte)((value >> 16) & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
        s.WriteByte((byte)(value & 0xFF));
    }
}
