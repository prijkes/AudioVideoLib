/*
 * Tests for RiffInfoTag (WAV LIST/INFO sub-chunk metadata) and the wiring on RiffStream.InfoTag.
 */
namespace AudioVideoLib.Tests;

using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class RiffInfoTagTests
{
    [Fact]
    public void Parse_RoundTrip_PreservesAllStandardTags()
    {
        var tag = new RiffInfoTag();
        tag.SetItem("INAM", "Test Title");
        tag.SetItem("IART", "Test Artist");
        tag.SetItem("IPRD", "Test Album");
        tag.SetItem("ICRD", "2026-04-18");
        tag.SetItem("ICMT", "A comment");
        tag.SetItem("IGNR", "Rock");
        tag.SetItem("ITRK", "1");
        tag.SetItem("IENG", "Engineer");
        tag.SetItem("ISFT", "AudioVideoLib");
        tag.SetItem("ICOP", "(c) 2026");

        var bytes = tag.ToByteArray();
        var roundTripped = RiffInfoTag.Parse(bytes);

        Assert.NotNull(roundTripped);
        Assert.Equal("Test Title", roundTripped!.Title);
        Assert.Equal("Test Artist", roundTripped.Artist);
        Assert.Equal("Test Album", roundTripped.Product);
        Assert.Equal("2026-04-18", roundTripped.CreationDate);
        Assert.Equal("A comment", roundTripped.Comment);
        Assert.Equal("Rock", roundTripped.Genre);
        Assert.Equal("1", roundTripped.Track);
        Assert.Equal("Engineer", roundTripped.Engineer);
        Assert.Equal("AudioVideoLib", roundTripped.Software);
        Assert.Equal("(c) 2026", roundTripped.Copyright);

        // Bytes -> model -> bytes is byte-identical.
        Assert.Equal(bytes, roundTripped.ToByteArray());
    }

    [Fact]
    public void Parse_Empty_ReturnsEmptyTag()
    {
        var tag = RiffInfoTag.Parse([]);
        Assert.NotNull(tag);
        Assert.Empty(tag!.Items);
        Assert.Null(tag.Title);
    }

    [Fact]
    public void Parse_SingleTag_Works()
    {
        var src = new RiffInfoTag();
        src.SetItem("INAM", "Single");
        var roundTripped = RiffInfoTag.Parse(src.ToByteArray());
        Assert.NotNull(roundTripped);
        Assert.Equal("Single", roundTripped!.Title);
        Assert.Single(roundTripped.Items);
    }

    [Fact]
    public void Parse_UnknownFourCc_FallsThroughToItemsDictionary()
    {
        var src = new RiffInfoTag();
        src.SetItem("XYZW", "weird");
        src.SetItem("INAM", "name");
        var roundTripped = RiffInfoTag.Parse(src.ToByteArray());
        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.Items.ContainsKey("XYZW"));
        Assert.Equal("weird", roundTripped.Items["XYZW"]);
        Assert.Equal("name", roundTripped.Title);
    }

    [Fact]
    public void Parse_MalformedSize_ReturnsNullOrPartial()
    {
        // Size declares 100 bytes, but only 4 bytes follow.
        var bytes = new byte[]
        {
            (byte)'I', (byte)'N', (byte)'A', (byte)'M',
            100, 0, 0, 0,
            0x41, 0x42, 0x43, 0x44,
        };
        var tag = RiffInfoTag.Parse(bytes);
        Assert.Null(tag);
    }

    [Fact]
    public void Parse_NullTermination_StripTrailingNuls()
    {
        var bytes = new byte[]
        {
            (byte)'I', (byte)'N', (byte)'A', (byte)'M',
            6, 0, 0, 0,
            0x41, 0x42, 0x43, 0x00, 0x00, 0x00,
        };
        var tag = RiffInfoTag.Parse(bytes);
        Assert.NotNull(tag);
        Assert.Equal("ABC", tag!.Title);
    }

    [Fact]
    public void Parse_CommentWithCrLf_PreservesNewlines()
    {
        var src = new RiffInfoTag();
        src.SetItem("ICMT", "line1\r\nline2");
        var roundTripped = RiffInfoTag.Parse(src.ToByteArray());
        Assert.NotNull(roundTripped);
        Assert.Equal("line1\r\nline2", roundTripped!.Comment);
    }

    [Fact]
    public void RiffStream_PicksUpListInfoChunk()
    {
        var bytes = BuildWavWithInfo([
            ("INAM", "Hello"),
            ("IART", "World"),
        ]);

        using var ms = new MemoryStream(bytes);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.InfoTag);
        Assert.Equal("Hello", rs.InfoTag!.Title);
        Assert.Equal("World", rs.InfoTag.Artist);
    }

    [Fact]
    public void RiffStream_MultipleListChunks_AreMerged()
    {
        var first = BuildListInfoChunk([("INAM", "FirstTitle")]);
        var second = BuildListInfoChunk([("IART", "SecondArtist"), ("INAM", "OverwrittenTitle")]);
        var bytes = BuildWavWithChunks(first, second);

        using var ms = new MemoryStream(bytes);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.InfoTag);
        Assert.Equal("OverwrittenTitle", rs.InfoTag!.Title);
        Assert.Equal("SecondArtist", rs.InfoTag.Artist);
    }

    [Fact]
    public void RiffStream_NoInfoChunk_InfoTagIsNull()
    {
        var bytes = BuildMinimalWav();
        using var ms = new MemoryStream(bytes);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.Null(rs.InfoTag);
    }

    [Fact]
    public void ToListChunkBytes_RoundTripsViaRiffStream()
    {
        var src = new RiffInfoTag();
        src.SetItem("INAM", "X");
        src.SetItem("IART", "Y");
        var listChunk = src.ToListChunkBytes();

        var bytes = BuildWavWithChunks(listChunk);
        using var ms = new MemoryStream(bytes);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.InfoTag);
        Assert.Equal("X", rs.InfoTag!.Title);
        Assert.Equal("Y", rs.InfoTag.Artist);

        // Re-emitting yields identical bytes.
        Assert.Equal(listChunk[8..], rs.InfoTag.ToListChunkBytes()[8..]);
    }

    private static byte[] BuildListInfoChunk((string Id, string Value)[] items)
    {
        using var inner = new MemoryStream();
        inner.Write(Encoding.ASCII.GetBytes("INFO"), 0, 4);
        foreach (var (id, value) in items)
        {
            var idBytes = Encoding.ASCII.GetBytes(id.PadRight(4)[..4]);
            var valueBytes = Encoding.ASCII.GetBytes(value);
            var size = (uint)(valueBytes.Length + 1);
            inner.Write(idBytes, 0, 4);
            WriteLeU32(inner, size);
            inner.Write(valueBytes, 0, valueBytes.Length);
            inner.WriteByte(0);
            if ((size & 1) != 0)
            {
                inner.WriteByte(0);
            }
        }

        var payload = inner.ToArray();
        using var outMs = new MemoryStream();
        outMs.Write(Encoding.ASCII.GetBytes("LIST"), 0, 4);
        WriteLeU32(outMs, (uint)payload.Length);
        outMs.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0)
        {
            outMs.WriteByte(0);
        }

        return outMs.ToArray();
    }

    internal static byte[] BuildWavWithInfo((string Id, string Value)[] items)
    {
        var listChunk = BuildListInfoChunk(items);
        return BuildWavWithChunks(listChunk);
    }

    internal static byte[] BuildWavWithChunks(params byte[][] extraChunks)
    {
        var fmtChunk = BuildFmtChunk();
        var dataChunk = BuildDataChunk();

        long totalChunkBytes = fmtChunk.Length + dataChunk.Length;
        foreach (var c in extraChunks)
        {
            totalChunkBytes += c.Length;
        }

        var size = (uint)(4 + totalChunkBytes);
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        WriteLeU32(ms, size);
        ms.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        ms.Write(fmtChunk, 0, fmtChunk.Length);
        foreach (var c in extraChunks)
        {
            ms.Write(c, 0, c.Length);
        }

        ms.Write(dataChunk, 0, dataChunk.Length);
        return ms.ToArray();
    }

    private static byte[] BuildMinimalWav()
    {
        var fmtChunk = BuildFmtChunk();
        var dataChunk = BuildDataChunk();
        var size = (uint)(4 + fmtChunk.Length + dataChunk.Length);
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        WriteLeU32(ms, size);
        ms.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        ms.Write(fmtChunk, 0, fmtChunk.Length);
        ms.Write(dataChunk, 0, dataChunk.Length);
        return ms.ToArray();
    }

    private static byte[] BuildFmtChunk()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
        WriteLeU32(ms, 16);
        // PCM mono 44100 Hz 16-bit.
        WriteLeU16(ms, 1);
        WriteLeU16(ms, 1);
        WriteLeU32(ms, 44100);
        WriteLeU32(ms, 88200);
        WriteLeU16(ms, 2);
        WriteLeU16(ms, 16);
        return ms.ToArray();
    }

    private static byte[] BuildDataChunk()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
        WriteLeU32(ms, 4);
        ms.Write([0, 0, 0, 0], 0, 4);
        return ms.ToArray();
    }

    internal static void WriteLeU32(Stream s, uint v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
        s.WriteByte((byte)((v >> 16) & 0xFF));
        s.WriteByte((byte)((v >> 24) & 0xFF));
    }

    internal static void WriteLeU16(Stream s, ushort v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
    }
}
