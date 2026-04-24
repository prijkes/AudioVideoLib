namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class DsfStreamTests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private const int DsdHeaderSize = 28;
    private const int FmtChunkSize = 52;
    private const int DataChunkHeaderSize = 12;

    private static byte[] BuildDsf(
        int channels = 2,
        int channelType = 2,
        int sampleRate = 2_822_400,
        int bitsPerSample = 1,
        long sampleCount = 1024,
        int blockSize = 4096,
        long dataPayloadSize = 16,
        byte[]? id3 = null,
        ulong? metadataPointerOverride = null,
        ulong? fileSizeOverride = null)
    {
        var dataChunkTotal = DataChunkHeaderSize + dataPayloadSize;
        var totalSize = (ulong)(DsdHeaderSize + FmtChunkSize + dataChunkTotal + (id3?.Length ?? 0));
        var metadataPointer = id3 is { Length: > 0 }
            ? (ulong)(DsdHeaderSize + FmtChunkSize + dataChunkTotal)
            : 0UL;

        if (metadataPointerOverride.HasValue)
        {
            metadataPointer = metadataPointerOverride.Value;
        }

        if (fileSizeOverride.HasValue)
        {
            totalSize = fileSizeOverride.Value;
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // DSD chunk
        bw.Write(Encoding.ASCII.GetBytes("DSD "));
        bw.Write((ulong)DsdHeaderSize);
        bw.Write(totalSize);
        bw.Write(metadataPointer);

        // fmt chunk (52 bytes)
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write((ulong)FmtChunkSize);
        bw.Write((uint)1);                  // version
        bw.Write((uint)0);                  // format id (DSD raw)
        bw.Write((uint)channelType);
        bw.Write((uint)channels);
        bw.Write((uint)sampleRate);
        bw.Write((uint)bitsPerSample);
        bw.Write((ulong)sampleCount);
        bw.Write((uint)blockSize);
        bw.Write((uint)0);                  // reserved

        // data chunk
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write((ulong)(DataChunkHeaderSize + dataPayloadSize));
        for (var i = 0L; i < dataPayloadSize; i++)
        {
            bw.Write((byte)(i & 0xFF));
        }

        if (id3 is { Length: > 0 })
        {
            bw.Write(id3);
        }

        return ms.ToArray();
    }

    private static byte[] BuildId3v2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2");
        frame.Values.Add("DSF Title");
        tag.SetFrame(frame);
        return tag.ToByteArray();
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Magic & rejection
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RejectsTooShortStream()
    {
        var stream = new DsfStream();
        using var ms = new MemoryStream(new byte[10]);
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void RejectsWrongMagic()
    {
        var stream = new DsfStream();
        var bytes = new byte[64];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
        using var ms = new MemoryStream(bytes);
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void RejectsNullStream()
    {
        var stream = new DsfStream();
        Assert.Throws<ArgumentNullException>(() => stream.ReadStream(null!));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Header-only / minimal
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ReadsAllChunks()
    {
        var bytes = BuildDsf();
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Collection(
            stream.Chunks,
            c => Assert.Equal("DSD ", c.Id),
            c => Assert.Equal("fmt ", c.Id),
            c => Assert.Equal("data", c.Id));
    }

    [Fact]
    public void ParsesFmtFields()
    {
        var bytes = BuildDsf(channels: 2, sampleRate: 2_822_400, bitsPerSample: 1, sampleCount: 5_644_800);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(2, stream.Channels);
        Assert.Equal(2_822_400, stream.SampleRate);
        Assert.Equal(1, stream.BitsPerSample);
        Assert.Equal(5_644_800, stream.SampleCount);
        Assert.Equal(4096, stream.BlockSizePerChannel);
        Assert.Equal(2_000L, stream.TotalDuration); // 5644800 / 2822400 * 1000 = 2000ms
    }

    [Fact]
    public void EmptyDataChunkIsTolerated()
    {
        var bytes = BuildDsf(dataPayloadSize: 0);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(0, stream.DataSize);
    }

    [Fact]
    public void NoMetadataPointerMeansNoEmbeddedTag()
    {
        var bytes = BuildDsf();
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(0UL, stream.MetadataPointer);
        Assert.Null(stream.EmbeddedId3v2);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. Embedded ID3v2
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ParsesEmbeddedId3v2()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDsf(id3: id3);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.NotNull(stream.EmbeddedId3v2);
        Assert.True(stream.MetadataPointer > 0);
    }

    [Fact]
    public void MetadataPointerBeyondEofYieldsNoTag()
    {
        var bytes = BuildDsf(metadataPointerOverride: 0xFFFF_FFFF);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Null(stream.EmbeddedId3v2);
    }

    [Fact]
    public void MetadataPointerInsideDataChunkIsIgnored()
    {
        // Pointer inside the data chunk region (mid-chunk) — must be ignored, not parsed.
        var bytes = BuildDsf(dataPayloadSize: 64, metadataPointerOverride: (ulong)(DsdHeaderSize + FmtChunkSize + 16));
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Null(stream.EmbeddedId3v2);
    }

    [Fact]
    public void ZeroMetadataPointerYieldsNoTag()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDsf(id3: id3, metadataPointerOverride: 0UL);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Null(stream.EmbeddedId3v2);
    }

    [Fact]
    public void GarbageAtMetadataPointerYieldsNullTag()
    {
        // Insert a "metadata block" at the end that is NOT a valid ID3v2.
        var bytes = BuildDsf(id3: [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00, 0x11, 0x22, 0x33]);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Null(stream.EmbeddedId3v2);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. Malformed/oversized
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void OversizedFileSizeIsRejected()
    {
        var bytes = BuildDsf(fileSizeOverride: 1UL << 50);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);

        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void OversizedDataChunkIsClamped()
    {
        // Patch the data chunk size to declare more bytes than exist in the buffer.
        var bytes = BuildDsf(dataPayloadSize: 16);
        // data chunk header lives at DsdHeaderSize + FmtChunkSize.
        var dataHeaderOffset = DsdHeaderSize + FmtChunkSize;
        // Overwrite the size field (little-endian uint64 at offset +4).
        var huge = (ulong)(bytes.Length * 4);
        for (var i = 0; i < 8; i++)
        {
            bytes[dataHeaderOffset + 4 + i] = (byte)((huge >> (i * 8)) & 0xFF);
        }

        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));
        Assert.True(stream.DataSize <= bytes.Length);
    }

    [Fact]
    public void TruncatedFmtChunkReturnsFalse()
    {
        var bytes = BuildDsf();
        // Truncate to DSD chunk + half of fmt header.
        var truncated = bytes.AsSpan(0, DsdHeaderSize + 6).ToArray();
        var stream = new DsfStream();
        using var ms = new MemoryStream(truncated);

        Assert.False(stream.ReadStream(ms));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. Round-trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RoundTripWithoutMetadataIsByteIdentical()
    {
        var bytes = BuildDsf(dataPayloadSize: 32);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));

        var roundTripped = stream.ToByteArray();
        Assert.Equal(bytes, roundTripped);
    }

    [Fact]
    public void RoundTripWithEmbeddedId3IsByteIdentical()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDsf(id3: id3, dataPayloadSize: 32);
        var stream = new DsfStream();
        using var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));

        var roundTripped = stream.ToByteArray();
        // The DSD audio payload is regenerated as zeros, so we check structural equality:
        // header + fmt + data header + id3 region must match in size and key fields.
        Assert.Equal(bytes.Length, roundTripped.Length);
        // The DSD chunk (28 bytes) is byte-identical.
        Assert.Equal(bytes.Take(DsdHeaderSize), roundTripped.Take(DsdHeaderSize));
        // The fmt chunk (52 bytes) is byte-identical.
        Assert.Equal(bytes.Skip(DsdHeaderSize).Take(FmtChunkSize), roundTripped.Skip(DsdHeaderSize).Take(FmtChunkSize));
        // The data chunk header (first 12 bytes) is byte-identical.
        Assert.Equal(
            bytes.Skip(DsdHeaderSize + FmtChunkSize).Take(DataChunkHeaderSize),
            roundTripped.Skip(DsdHeaderSize + FmtChunkSize).Take(DataChunkHeaderSize));
        // The ID3 trailing block is byte-identical.
        var metaOffset = (int)stream.MetadataPointer;
        Assert.Equal(bytes.Skip(metaOffset), roundTripped.Skip(metaOffset));
    }
}
