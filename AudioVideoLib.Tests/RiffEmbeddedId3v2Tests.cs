/*
 * Tests for the id3 / ID3 embedded ID3v2 chunk in WAV (A2 in the tag-formats expansion plan).
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

using Xunit;

public class RiffEmbeddedId3v2Tests
{
    [Fact]
    public void Wav_WithLowercaseId3Chunk_ParsesEmbeddedTag()
    {
        var id3Bytes = BuildId3v240Tag("TIT2", "Hello WAV");
        var wav = RiffInfoTagTests.BuildWavWithChunks(BuildId3Chunk("id3 ", id3Bytes));

        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.EmbeddedId3v2);

        var tag = Assert.IsType<Id3v2Tag>(rs.EmbeddedId3v2!.AudioTag);
        var tit2 = tag.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(tit2);
        Assert.Equal("Hello WAV", tit2!.Values.Single());
    }

    [Fact]
    public void Wav_WithUppercaseId3Chunk_AlsoParses()
    {
        var id3Bytes = BuildId3v240Tag("TPE1", "Capital ID");
        var wav = RiffInfoTagTests.BuildWavWithChunks(BuildId3Chunk("ID3 ", id3Bytes));

        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.EmbeddedId3v2);

        var tag = Assert.IsType<Id3v2Tag>(rs.EmbeddedId3v2!.AudioTag);
        var tpe1 = tag.GetFrame<Id3v2TextFrame>("TPE1");
        Assert.NotNull(tpe1);
        Assert.Equal("Capital ID", tpe1!.Values.Single());
    }

    [Fact]
    public void Wav_WithGarbledId3Chunk_LeavesEmbeddedId3v2Null()
    {
        var garbage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03 };
        var wav = RiffInfoTagTests.BuildWavWithChunks(BuildId3Chunk("id3 ", garbage));

        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.Null(rs.EmbeddedId3v2);
    }

    [Fact]
    public void Wav_WithoutId3Chunk_LeavesEmbeddedId3v2Null()
    {
        var wav = RiffInfoTagTests.BuildWavWithChunks();
        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.Null(rs.EmbeddedId3v2);
    }

    [Fact]
    public void Id3v2_RoundTripBytesThroughChunk_AreIdentical()
    {
        var id3Bytes = BuildId3v240Tag("TIT2", "RoundTripTitle");
        var chunk = BuildId3Chunk("id3 ", id3Bytes);

        // The chunk payload starts at offset 8 and is exactly id3Bytes.Length bytes long.
        var extracted = new byte[id3Bytes.Length];
        Buffer.BlockCopy(chunk, 8, extracted, 0, id3Bytes.Length);
        Assert.Equal(id3Bytes, extracted);

        var wav = RiffInfoTagTests.BuildWavWithChunks(chunk);
        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.EmbeddedId3v2);

        var tag = Assert.IsType<Id3v2Tag>(rs.EmbeddedId3v2!.AudioTag);
        var reSerialised = tag.ToByteArray();
        Assert.Equal(id3Bytes, reSerialised);
    }

    [Fact]
    public void Wav_WithOversizedButWellFormedId3Tag_ParsesEmbeddedTag()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 1024 };
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF8 };
        frame.Values.Add("Padded");
        tag.SetFrame(frame);
        var id3Bytes = tag.ToByteArray();

        var wav = RiffInfoTagTests.BuildWavWithChunks(BuildId3Chunk("id3 ", id3Bytes));
        using var ms = new MemoryStream(wav);
        var rs = new IO.RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.EmbeddedId3v2);

        var roundTripped = Assert.IsType<Id3v2Tag>(rs.EmbeddedId3v2!.AudioTag);
        var tit2 = roundTripped.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(tit2);
        Assert.Equal("Padded", tit2!.Values.Single());
    }

    private static byte[] BuildId3v240Tag(string identifier, string value)
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, identifier) { TextEncoding = Id3v2FrameEncodingType.UTF8 };
        frame.Values.Add(value);
        tag.SetFrame(frame);
        return tag.ToByteArray();
    }

    private static byte[] BuildId3Chunk(string fourCc, byte[] payload)
    {
        var pad = (payload.Length & 1) == 1 ? 1 : 0;
        var buf = new byte[8 + payload.Length + pad];
        for (var i = 0; i < 4; i++)
        {
            buf[i] = (byte)fourCc[i];
        }

        var size = (uint)payload.Length;
        buf[4] = (byte)(size & 0xFF);
        buf[5] = (byte)((size >> 8) & 0xFF);
        buf[6] = (byte)((size >> 16) & 0xFF);
        buf[7] = (byte)((size >> 24) & 0xFF);
        Buffer.BlockCopy(payload, 0, buf, 8, payload.Length);
        return buf;
    }
}
