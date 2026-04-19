/*
 * Tests for IxmlChunk and the wiring on RiffStream.IxmlChunk.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class IxmlChunkTests
{
    private const string MinimalIxml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><BWFXML><PROJECT>P</PROJECT><SCENE>S</SCENE><TAKE>T</TAKE></BWFXML>";

    [Fact]
    public void Parse_RoundTrip_PreservesRawBytes()
    {
        var bytes = Encoding.UTF8.GetBytes(MinimalIxml);
        var chunk = IxmlChunk.Parse(bytes);
        Assert.NotNull(chunk);
        Assert.True(chunk!.IsWellFormed);
        Assert.Equal("P", chunk.ProjectName);
        Assert.Equal("S", chunk.SceneName);
        Assert.Equal("T", chunk.TakeName);

        // bytes -> model -> bytes is byte-identical via raw payload.
        Assert.Equal(bytes, chunk.ToByteArray());
    }

    [Fact]
    public void Parse_BomPrefixed_StripsBomFromXmlString()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var bytes = new byte[bom.Length + Encoding.UTF8.GetByteCount(MinimalIxml)];
        Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
        Encoding.UTF8.GetBytes(MinimalIxml, 0, MinimalIxml.Length, bytes, bom.Length);

        var chunk = IxmlChunk.Parse(bytes);
        Assert.NotNull(chunk);
        Assert.True(chunk!.IsWellFormed);
        Assert.Equal("P", chunk.ProjectName);
        Assert.False(chunk.Xml.StartsWith('\uFEFF'));

        // Round-trip-with-bom round-trips via ToByteArray(true).
        var withBom = chunk.ToByteArray(true);
        Assert.Equal(0xEF, withBom[0]);
        Assert.Equal(0xBB, withBom[1]);
        Assert.Equal(0xBF, withBom[2]);
    }

    [Fact]
    public void Parse_MissingOptionalFields_ReturnsNullForThem()
    {
        var xml = "<BWFXML><PROJECT>OnlyProject</PROJECT></BWFXML>";
        var chunk = IxmlChunk.Parse(Encoding.UTF8.GetBytes(xml));
        Assert.NotNull(chunk);
        Assert.True(chunk!.IsWellFormed);
        Assert.Equal("OnlyProject", chunk.ProjectName);
        Assert.Null(chunk.SceneName);
        Assert.Null(chunk.TakeName);
        Assert.Null(chunk.Tape);
        Assert.Null(chunk.Note);
        Assert.Null(chunk.FileUid);
        Assert.Null(chunk.Ubits);
    }

    [Fact]
    public void Parse_MalformedXml_DoesNotThrow_ReturnsNotWellFormed()
    {
        var bytes = Encoding.UTF8.GetBytes("<BWFXML><PROJECT>oops");
        var chunk = IxmlChunk.Parse(bytes);
        Assert.NotNull(chunk);
        Assert.False(chunk!.IsWellFormed);
        Assert.Null(chunk.ProjectName);
    }

    [Fact]
    public void Parse_EmptyPayload_ReturnsNull()
        => Assert.Null(IxmlChunk.Parse([]));

    [Fact]
    public void Parse_NullArgument_Throws()
        => Assert.Throws<ArgumentNullException>(() => IxmlChunk.Parse(null!));

    [Fact]
    public void Parse_AllOptionalFields_AreSurfaced()
    {
        var xml =
            "<BWFXML>" +
            "<PROJECT>P</PROJECT>" +
            "<SCENE>S</SCENE>" +
            "<TAKE>1</TAKE>" +
            "<TAPE>R001</TAPE>" +
            "<NOTE>n</NOTE>" +
            "<FILE_UID>uid</FILE_UID>" +
            "<UBITS>$00000000</UBITS>" +
            "</BWFXML>";
        var chunk = IxmlChunk.Parse(Encoding.UTF8.GetBytes(xml));
        Assert.NotNull(chunk);
        Assert.Equal("P", chunk!.ProjectName);
        Assert.Equal("S", chunk.SceneName);
        Assert.Equal("1", chunk.TakeName);
        Assert.Equal("R001", chunk.Tape);
        Assert.Equal("n", chunk.Note);
        Assert.Equal("uid", chunk.FileUid);
        Assert.Equal("$00000000", chunk.Ubits);
    }

    [Fact]
    public void RiffStream_PicksUpIxmlChunk()
    {
        var payload = Encoding.UTF8.GetBytes(MinimalIxml);
        var chunkBytes = BuildIxmlChunkBytes(payload);
        var wav = RiffInfoTagTests.BuildWavWithChunks(chunkBytes);

        using var ms = new MemoryStream(wav);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.IxmlChunk);
        Assert.True(rs.IxmlChunk!.IsWellFormed);
        Assert.Equal("P", rs.IxmlChunk.ProjectName);
    }

    [Fact]
    public void RiffStream_MalformedXmlInChunk_StillExposesChunk()
    {
        var payload = Encoding.UTF8.GetBytes("<broken");
        var chunkBytes = BuildIxmlChunkBytes(payload);
        var wav = RiffInfoTagTests.BuildWavWithChunks(chunkBytes);

        using var ms = new MemoryStream(wav);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.IxmlChunk);
        Assert.False(rs.IxmlChunk!.IsWellFormed);
    }

    private static byte[] BuildIxmlChunkBytes(byte[] payload)
    {
        var pad = (payload.Length & 1) == 1 ? 1 : 0;
        var buf = new byte[8 + payload.Length + pad];
        Encoding.ASCII.GetBytes("iXML", 0, 4, buf, 0);
        var size = (uint)payload.Length;
        buf[4] = (byte)(size & 0xFF);
        buf[5] = (byte)((size >> 8) & 0xFF);
        buf[6] = (byte)((size >> 16) & 0xFF);
        buf[7] = (byte)((size >> 24) & 0xFF);
        Buffer.BlockCopy(payload, 0, buf, 8, payload.Length);
        return buf;
    }
}
