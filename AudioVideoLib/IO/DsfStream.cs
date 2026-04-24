namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// Minimal structural walker for Sony DSF (Direct Stream File) DSD audio containers (<c>.dsf</c>).
/// Does not decode audio samples.
/// </summary>
/// <remarks>
/// The walker captures the <c>DSD </c>, <c>fmt </c>, and <c>data</c> chunks (in that order, per spec)
/// and locates an optional ID3v2 metadata block at the absolute offset given by the <c>DSD </c>
/// chunk's metadata-pointer field. All multi-byte integers in DSF are little-endian.
/// </remarks>
public sealed class DsfStream : IMediaContainer
{
    private const int DsdChunkSize = 28;
    private const int FmtChunkSize = 52;
    private const long MaxAllowedFileSize = 1L << 40;

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
    /// Gets the chunks discovered inside the DSF container, in file order.
    /// </summary>
    public IReadOnlyList<DsdChunk> Chunks => _chunks;

    /// <summary>
    /// Gets the declared total file size from the <c>DSD </c> chunk, in bytes.
    /// </summary>
    public ulong DeclaredFileSize { get; private set; }

    /// <summary>
    /// Gets the absolute offset of the embedded ID3v2 metadata block, or 0 if no metadata is declared.
    /// </summary>
    public ulong MetadataPointer { get; private set; }

    /// <summary>
    /// Gets the format version from the <c>fmt </c> chunk (typically <c>1</c>).
    /// </summary>
    public int FormatVersion { get; private set; }

    /// <summary>
    /// Gets the format identifier from the <c>fmt </c> chunk (<c>0</c> = DSD raw).
    /// </summary>
    public int FormatId { get; private set; }

    /// <summary>
    /// Gets the channel-type code from the <c>fmt </c> chunk
    /// (1=mono, 2=stereo, 3=3ch, 4=quad, 5=4ch, 6=5ch, 7=5.1ch).
    /// </summary>
    public int ChannelType { get; private set; }

    /// <summary>
    /// Gets the channel count from the <c>fmt </c> chunk.
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the sample rate from the <c>fmt </c> chunk, in Hz (e.g. 2822400 for DSD64).
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Gets the bits-per-sample value from the <c>fmt </c> chunk (1 or 8).
    /// </summary>
    public int BitsPerSample { get; private set; }

    /// <summary>
    /// Gets the per-channel sample count from the <c>fmt </c> chunk.
    /// </summary>
    public long SampleCount { get; private set; }

    /// <summary>
    /// Gets the per-channel block size from the <c>fmt </c> chunk (typically 4096).
    /// </summary>
    public int BlockSizePerChannel { get; private set; }

    /// <summary>
    /// Gets the absolute offset of the <c>data</c> chunk's payload, or 0 if the chunk is absent.
    /// </summary>
    public long DataOffset { get; private set; }

    /// <summary>
    /// Gets the size of the <c>data</c> chunk's payload in bytes (excludes the 12-byte chunk header).
    /// </summary>
    public long DataSize { get; private set; }

    /// <summary>
    /// Gets the embedded ID3v2 tag, or <c>null</c> when no metadata block is present or it could not be parsed.
    /// </summary>
    public Id3v2Tag? EmbeddedId3v2 { get; private set; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < DsdChunkSize)
        {
            return false;
        }

        var dsdHeader = new byte[DsdChunkSize];
        if (stream.Read(dsdHeader, 0, DsdChunkSize) != DsdChunkSize)
        {
            return false;
        }

        var magic = Encoding.ASCII.GetString(dsdHeader, 0, 4);
        if (magic != "DSD ")
        {
            return false;
        }

        var dsdChunkSize = ReadLeU64(dsdHeader, 4);
        if (dsdChunkSize != DsdChunkSize)
        {
            return false;
        }

        var fileSize = ReadLeU64(dsdHeader, 12);
        var metadataPointer = ReadLeU64(dsdHeader, 20);

        if (fileSize > MaxAllowedFileSize)
        {
            return false;
        }

        StartOffset = start;
        DeclaredFileSize = fileSize;
        MetadataPointer = metadataPointer;
        _chunks.Add(new DsdChunk("DSD ", start, start + DsdChunkSize, dsdHeader));

        var containerEnd = Math.Min(start + (long)fileSize, stream.Length);
        if (containerEnd <= start)
        {
            containerEnd = stream.Length;
        }

        // fmt chunk
        if (stream.Position + 12 > containerEnd)
        {
            EndOffset = stream.Position;
            return false;
        }

        var fmtStart = stream.Position;
        var fmtHeader = new byte[12];
        if (stream.Read(fmtHeader, 0, 12) != 12)
        {
            EndOffset = stream.Position;
            return false;
        }

        var fmtId = Encoding.ASCII.GetString(fmtHeader, 0, 4);
        if (fmtId != "fmt ")
        {
            EndOffset = stream.Position;
            return false;
        }

        var fmtChunkSize = ReadLeU64(fmtHeader, 4);
        if (fmtChunkSize < 12 || fmtChunkSize > (ulong)(containerEnd - fmtStart))
        {
            EndOffset = stream.Position;
            return false;
        }

        var fmtPayloadSize = (int)(fmtChunkSize - 12);
        var fmtPayload = new byte[fmtPayloadSize];
        if (stream.Read(fmtPayload, 0, fmtPayloadSize) != fmtPayloadSize)
        {
            EndOffset = stream.Position;
            return false;
        }

        var fmtData = new byte[fmtChunkSize];
        Array.Copy(fmtHeader, 0, fmtData, 0, 12);
        Array.Copy(fmtPayload, 0, fmtData, 12, fmtPayloadSize);
        _chunks.Add(new DsdChunk("fmt ", fmtStart, fmtStart + (long)fmtChunkSize, fmtData));

        if (fmtPayloadSize >= 40 && fmtChunkSize == FmtChunkSize)
        {
            FormatVersion = (int)ReadLeU32(fmtPayload, 0);
            FormatId = (int)ReadLeU32(fmtPayload, 4);
            ChannelType = (int)ReadLeU32(fmtPayload, 8);
            Channels = (int)ReadLeU32(fmtPayload, 12);
            SampleRate = (int)ReadLeU32(fmtPayload, 16);
            BitsPerSample = (int)ReadLeU32(fmtPayload, 20);
            SampleCount = (long)ReadLeU64(fmtPayload, 24);
            BlockSizePerChannel = (int)ReadLeU32(fmtPayload, 32);
        }

        // data chunk
        if (stream.Position + 12 > containerEnd)
        {
            EndOffset = stream.Position;
            return _chunks.Count > 0;
        }

        var dataStart = stream.Position;
        var dataHeader = new byte[12];
        if (stream.Read(dataHeader, 0, 12) != 12)
        {
            EndOffset = stream.Position;
            return _chunks.Count > 0;
        }

        var dataId = Encoding.ASCII.GetString(dataHeader, 0, 4);
        if (dataId != "data")
        {
            EndOffset = stream.Position;
            return _chunks.Count > 0;
        }

        var dataChunkSize = ReadLeU64(dataHeader, 4);
        if (dataChunkSize < 12)
        {
            EndOffset = stream.Position;
            return _chunks.Count > 0;
        }

        var dataPayloadSize = dataChunkSize - 12;
        var availableForData = (ulong)Math.Max(0, containerEnd - (dataStart + 12));
        if (dataPayloadSize > availableForData)
        {
            // Oversized — treat as truncated, clamp.
            dataPayloadSize = availableForData;
        }

        DataOffset = dataStart + 12;
        DataSize = (long)dataPayloadSize;
        var dataEnd = DataOffset + DataSize;
        var dataChunkBytes = new byte[12 + (int)dataPayloadSize];
        Array.Copy(dataHeader, 0, dataChunkBytes, 0, 12);
        if (dataPayloadSize > 0)
        {
            var read = stream.Read(dataChunkBytes, 12, (int)dataPayloadSize);
            if (read != (int)dataPayloadSize)
            {
                DataSize = read;
                dataEnd = DataOffset + read;
                Array.Resize(ref dataChunkBytes, 12 + read);
            }
        }
        _chunks.Add(new DsdChunk("data", dataStart, dataEnd, dataChunkBytes));
        stream.Position = dataEnd;

        // Embedded ID3v2 at metadata pointer.
        if (metadataPointer != 0
            && metadataPointer >= (ulong)dataEnd
            && metadataPointer < (ulong)stream.Length)
        {
            var metaOffset = (long)metadataPointer;
            stream.Position = metaOffset;
            var remaining = stream.Length - metaOffset;
            if (remaining > 0)
            {
                var buffer = new byte[remaining];
                var read = stream.Read(buffer, 0, (int)remaining);
                if (read > 0)
                {
                    using var ms = new MemoryStream(buffer, 0, read);
                    try
                    {
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
            }

            stream.Position = Math.Min(stream.Length, metaOffset + (EmbeddedId3v2?.ToByteArray().Length ?? 0));
        }

        EndOffset = Math.Max(stream.Position, containerEnd);
        if (EndOffset > stream.Length)
        {
            EndOffset = stream.Length;
        }

        return _chunks.Count > 0;
    }

    /// <inheritdoc/>
    public byte[] ToByteArray()
    {
        if (_chunks.Count == 0)
        {
            return [];
        }

        using var ms = new MemoryStream();

        // DSD chunk: rebuild so the metadata pointer / file size reflect current state.
        var id3Bytes = EmbeddedId3v2?.ToByteArray() ?? [];

        var fmtChunk = FindChunk("fmt ");
        var dataChunk = FindChunk("data");

        var fmtBytes = fmtChunk?.Data ?? [];
        var dataPayloadSize = dataChunk is null ? 0L : DataSize;
        var dataChunkTotal = 12 + dataPayloadSize;

        var totalSize = (ulong)(DsdChunkSize + fmtBytes.Length + dataChunkTotal + id3Bytes.Length);
        var metadataPointer = id3Bytes.Length > 0
            ? (ulong)(DsdChunkSize + fmtBytes.Length + dataChunkTotal)
            : 0UL;

        var dsdHeader = new byte[DsdChunkSize];
        Encoding.ASCII.GetBytes("DSD ", 0, 4, dsdHeader, 0);
        WriteLeU64(dsdHeader, 4, DsdChunkSize);
        WriteLeU64(dsdHeader, 12, totalSize);
        WriteLeU64(dsdHeader, 20, metadataPointer);
        ms.Write(dsdHeader, 0, DsdChunkSize);

        if (fmtBytes.Length > 0)
        {
            ms.Write(fmtBytes, 0, fmtBytes.Length);
        }

        if (dataChunk is not null)
        {
            ms.Write(dataChunk.Data, 0, dataChunk.Data.Length);
        }

        if (id3Bytes.Length > 0)
        {
            ms.Write(id3Bytes, 0, id3Bytes.Length);
        }

        return ms.ToArray();
    }

    private DsdChunk? FindChunk(string id)
    {
        foreach (var chunk in _chunks)
        {
            if (chunk.Id == id)
            {
                return chunk;
            }
        }

        return null;
    }

    private static ulong ReadLeU64(byte[] b, int off) =>
        b[off]
        | ((ulong)b[off + 1] << 8)
        | ((ulong)b[off + 2] << 16)
        | ((ulong)b[off + 3] << 24)
        | ((ulong)b[off + 4] << 32)
        | ((ulong)b[off + 5] << 40)
        | ((ulong)b[off + 6] << 48)
        | ((ulong)b[off + 7] << 56);

    private static uint ReadLeU32(byte[] b, int off) =>
        (uint)(b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24));

    private static void WriteLeU64(byte[] b, int off, ulong value)
    {
        b[off] = (byte)(value & 0xFF);
        b[off + 1] = (byte)((value >> 8) & 0xFF);
        b[off + 2] = (byte)((value >> 16) & 0xFF);
        b[off + 3] = (byte)((value >> 24) & 0xFF);
        b[off + 4] = (byte)((value >> 32) & 0xFF);
        b[off + 5] = (byte)((value >> 40) & 0xFF);
        b[off + 6] = (byte)((value >> 48) & 0xFF);
        b[off + 7] = (byte)((value >> 56) & 0xFF);
    }
}
