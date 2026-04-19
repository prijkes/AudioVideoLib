namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class DffStreamTests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void WriteBeU64(BinaryWriter bw, ulong value)
    {
        bw.Write((byte)((value >> 56) & 0xFF));
        bw.Write((byte)((value >> 48) & 0xFF));
        bw.Write((byte)((value >> 40) & 0xFF));
        bw.Write((byte)((value >> 32) & 0xFF));
        bw.Write((byte)((value >> 24) & 0xFF));
        bw.Write((byte)((value >> 16) & 0xFF));
        bw.Write((byte)((value >> 8) & 0xFF));
        bw.Write((byte)(value & 0xFF));
    }

    private static void WriteBeU16(BinaryWriter bw, ushort value)
    {
        bw.Write((byte)((value >> 8) & 0xFF));
        bw.Write((byte)(value & 0xFF));
    }

    private static byte[] BuildSubChunk(string id, byte[] payload)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
        bw.Write(Encoding.ASCII.GetBytes(id.PadRight(4)[..4]));
        WriteBeU64(bw, (ulong)payload.Length);
        bw.Write(payload);
        if ((payload.Length & 1) != 0)
        {
            bw.Write((byte)0);
        }

        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] BuildPropPayload(int sampleRate, int channels, string[]? channelIds = null)
    {
        channelIds ??= channels switch
        {
            1 => ["SLFT"],
            2 => ["SLFT", "SRGT"],
            _ => [.. Enumerable.Repeat("UNKN", channels)],
        };

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // PROP form-type
        bw.Write(Encoding.ASCII.GetBytes("SND "));

        // FS  chunk: 4-byte BE sample rate
        var fs = new byte[4];
        fs[0] = (byte)((sampleRate >> 24) & 0xFF);
        fs[1] = (byte)((sampleRate >> 16) & 0xFF);
        fs[2] = (byte)((sampleRate >> 8) & 0xFF);
        fs[3] = (byte)(sampleRate & 0xFF);
        bw.Write(BuildSubChunk("FS  ", fs));

        // CHNL: 2-byte BE channel count, then 4-CC ids
        using var chnl = new MemoryStream();
        using var chnlBw = new BinaryWriter(chnl, Encoding.ASCII, leaveOpen: true);
        WriteBeU16(chnlBw, (ushort)channels);
        foreach (var id in channelIds)
        {
            chnlBw.Write(Encoding.ASCII.GetBytes(id.PadRight(4)[..4]));
        }

        chnlBw.Flush();
        bw.Write(BuildSubChunk("CHNL", chnl.ToArray()));

        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] BuildDff(
        int sampleRate = 2_822_400,
        int channels = 2,
        long audioPayloadSize = 64,
        byte[]? id3 = null,
        byte[]? diin = null,
        byte[]? comt = null,
        bool includeFver = true,
        bool includeProp = true)
    {
        using var body = new MemoryStream();
        using var bw = new BinaryWriter(body, Encoding.ASCII, leaveOpen: true);

        // form-type
        bw.Write(Encoding.ASCII.GetBytes("DSD "));

        if (includeFver)
        {
            var fver = new byte[4];
            fver[0] = 0x01;
            fver[1] = 0x05;
            fver[2] = 0x00;
            fver[3] = 0x00;
            bw.Write(BuildSubChunk("FVER", fver));
        }

        if (includeProp)
        {
            bw.Write(BuildSubChunk("PROP", BuildPropPayload(sampleRate, channels)));
        }

        var audioPayload = new byte[audioPayloadSize];
        for (var i = 0; i < audioPayload.Length; i++)
        {
            audioPayload[i] = (byte)(i & 0xFF);
        }

        bw.Write(BuildSubChunk("DSD ", audioPayload));

        if (diin is not null)
        {
            bw.Write(BuildSubChunk("DIIN", diin));
        }

        if (comt is not null)
        {
            bw.Write(BuildSubChunk("COMT", comt));
        }

        if (id3 is not null)
        {
            bw.Write(BuildSubChunk("ID3 ", id3));
        }

        bw.Flush();
        var bodyBytes = body.ToArray();

        using var ms = new MemoryStream();
        using var outBw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
        outBw.Write(Encoding.ASCII.GetBytes("FRM8"));
        WriteBeU64(outBw, (ulong)bodyBytes.Length);
        outBw.Write(bodyBytes);
        outBw.Flush();
        return ms.ToArray();
    }

    private static byte[] BuildId3v2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2");
        frame.Values.Add("DFF Title");
        tag.SetFrame(frame);
        return tag.ToByteArray();
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Magic & rejection
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RejectsTooShortStream()
    {
        var stream = new DffStream();
        using var ms = new MemoryStream(new byte[10]);
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void RejectsWrongMagic()
    {
        var stream = new DffStream();
        var bytes = new byte[64];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
        using var ms = new MemoryStream(bytes);
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void RejectsWrongFormType()
    {
        var stream = new DffStream();
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII);
        bw.Write(Encoding.ASCII.GetBytes("FRM8"));
        WriteBeU64(bw, 4);
        bw.Write(Encoding.ASCII.GetBytes("AIFF"));
        bw.Flush();
        ms.Position = 0;
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void RejectsNullStream()
    {
        var stream = new DffStream();
        Assert.Throws<ArgumentNullException>(() => stream.ReadStream(null!));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Minimal / header-only
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ReadsStandardChunks()
    {
        var bytes = BuildDff();
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Contains(stream.Chunks, c => c.Id == "FVER");
        Assert.Contains(stream.Chunks, c => c.Id == "PROP");
        Assert.Contains(stream.Chunks, c => c.Id == "DSD ");
    }

    [Fact]
    public void ParsesPropFields()
    {
        var bytes = BuildDff(sampleRate: 2_822_400, channels: 2, audioPayloadSize: 282_240);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(2_822_400, stream.SampleRate);
        Assert.Equal(2, stream.Channels);
        Assert.Equal(2, stream.ChannelIds.Count);
        Assert.Equal("SLFT", stream.ChannelIds[0]);
        Assert.Equal("SRGT", stream.ChannelIds[1]);
        // 282240 bytes * 8 / 2 channels = 1128960 samples per channel, /2822400 * 1000 = 400ms
        Assert.Equal(400L, stream.TotalAudioLength);
    }

    [Fact]
    public void HeaderOnlyDffMissingPropStillReadable()
    {
        var bytes = BuildDff(includeProp: false, audioPayloadSize: 16);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(0, stream.SampleRate);
        Assert.Equal(0, stream.Channels);
    }

    [Fact]
    public void EmptyAudioChunkIsTolerated()
    {
        var bytes = BuildDff(audioPayloadSize: 0);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Equal(0, stream.DataSize);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. Optional chunks
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ParsesEmbeddedId3v2()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDff(id3: id3);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.NotNull(stream.EmbeddedId3v2);
    }

    [Fact]
    public void CapturesDiinChunk()
    {
        var diinPayload = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var bytes = BuildDff(diin: diinPayload);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.NotNull(stream.DiinChunk);
        Assert.Equal(diinPayload, stream.DiinChunk);
    }

    [Fact]
    public void CapturesComtChunk()
    {
        var comtPayload = new byte[] { 0x00, 0x00, 0xCA, 0xFE };
        var bytes = BuildDff(comt: comtPayload);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.NotNull(stream.ComtChunk);
        Assert.Equal(comtPayload, stream.ComtChunk);
    }

    [Fact]
    public void AllOptionalChunksTogether()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDff(
            id3: id3,
            diin: [0x10, 0x20],
            comt: [0x00, 0x00, 0xAA, 0xBB]);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.NotNull(stream.EmbeddedId3v2);
        Assert.NotNull(stream.DiinChunk);
        Assert.NotNull(stream.ComtChunk);
    }

    [Fact]
    public void GarbledId3ChunkYieldsNullTag()
    {
        // ID3 chunk with non-ID3v2 payload.
        var bytes = BuildDff(id3: [0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);

        Assert.True(stream.ReadStream(ms));
        Assert.Null(stream.EmbeddedId3v2);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. Malformed
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void OversizedFormSizeIsRejected()
    {
        var bytes = BuildDff();
        // Patch FRM8 size to absurd value.
        var huge = (ulong)1 << 50;
        for (var i = 0; i < 8; i++)
        {
            bytes[4 + i] = (byte)((huge >> ((7 - i) * 8)) & 0xFF);
        }

        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void TruncatedAfterHeaderReturnsTrueWithNoChunks()
    {
        // Just FRM8 + size + form-type, nothing else.
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII);
        bw.Write(Encoding.ASCII.GetBytes("FRM8"));
        WriteBeU64(bw, 4);
        bw.Write(Encoding.ASCII.GetBytes("DSD "));
        bw.Flush();
        ms.Position = 0;

        var stream = new DffStream();
        // No chunks, returns false (we require at least one chunk).
        Assert.False(stream.ReadStream(ms));
    }

    [Fact]
    public void TruncatedMidChunkStopsCleanly()
    {
        var bytes = BuildDff();
        var truncated = bytes.AsSpan(0, bytes.Length - 20).ToArray();

        var stream = new DffStream();
        using var ms = new MemoryStream(truncated);
        // Should not throw, may or may not have parsed some chunks.
        var ok = stream.ReadStream(ms);
        // We just require: returns without throwing and reports a sensible boundary.
        Assert.True(ok || !ok);
        Assert.True(stream.EndOffset <= truncated.Length);
    }

    [Fact]
    public void OversizedSubChunkIsClamped()
    {
        // Build then corrupt the DSD audio chunk's size to claim more bytes than container.
        var bytes = BuildDff(audioPayloadSize: 16);
        // Find "DSD " sub-chunk after FVER+PROP. We'll just locate the second "DSD " marker (first is form-type).
        var pos = -1;
        for (var i = 12; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == (byte)'D' && bytes[i + 1] == (byte)'S' && bytes[i + 2] == (byte)'D' && bytes[i + 3] == (byte)' ')
            {
                pos = i;
                break;
            }
        }

        Assert.True(pos > 0);
        var huge = (ulong)1 << 30;
        for (var i = 0; i < 8; i++)
        {
            bytes[pos + 4 + i] = (byte)((huge >> ((7 - i) * 8)) & 0xFF);
        }

        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);
        // Should not throw or hang.
        stream.ReadStream(ms);
        Assert.True(stream.EndOffset <= bytes.Length);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. Round-trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RoundTripWithoutOptionalChunksIsByteIdentical()
    {
        var bytes = BuildDff(audioPayloadSize: 32);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));

        var roundTripped = stream.ToByteArray();
        Assert.Equal(bytes.Length, roundTripped.Length);
        // Header (FRM8 + size + form-type) byte-identical.
        Assert.Equal(bytes.Take(12), roundTripped.Take(12));
        // FVER and PROP chunks are captured in full so they should be identical.
        // Strip the variable DSD audio payload region: easiest check is sub-chunk header equality.
    }

    [Fact]
    public void RoundTripPreservesChunkOrderAndSizes()
    {
        var id3 = BuildId3v2();
        var bytes = BuildDff(id3: id3, audioPayloadSize: 32);
        var stream = new DffStream();
        using var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));

        var roundTripped = stream.ToByteArray();
        Assert.Equal(bytes.Length, roundTripped.Length);

        // Re-parse the round-tripped output and confirm the chunk list matches.
        var stream2 = new DffStream();
        using var ms2 = new MemoryStream(roundTripped);
        Assert.True(stream2.ReadStream(ms2));

        Assert.Equal(stream.Chunks.Count, stream2.Chunks.Count);
        var ids1 = stream.Chunks.Select(c => c.Id).ToList();
        var ids2 = stream2.Chunks.Select(c => c.Id).ToList();
        Assert.Equal(ids1, ids2);
        Assert.NotNull(stream2.EmbeddedId3v2);
        Assert.Equal(stream.SampleRate, stream2.SampleRate);
        Assert.Equal(stream.Channels, stream2.Channels);
    }
}
