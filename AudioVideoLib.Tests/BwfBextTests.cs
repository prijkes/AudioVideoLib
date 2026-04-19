/*
 * Tests for BwfBextChunk and the wiring on RiffStream.BextChunk.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class BwfBextTests
{
    [Fact]
    public void Parse_RoundTrip_V0_IsByteIdentical()
    {
        var c = MakeBext(version: 0);
        var bytes = c.ToByteArray();

        var parsed = BwfBextChunk.Parse(bytes);
        Assert.NotNull(parsed);
        var roundTripped = parsed!.ToByteArray();
        Assert.Equal(bytes, roundTripped);
    }

    [Fact]
    public void Parse_RoundTrip_V1_IsByteIdentical()
    {
        var c = MakeBext(version: 1);
        c.LoudnessValue = -1234;
        c.LoudnessRange = 800;
        c.MaxTruePeakLevel = -100;
        c.MaxMomentaryLoudness = -500;
        c.MaxShortTermLoudness = -700;

        var bytes = c.ToByteArray();
        var parsed = BwfBextChunk.Parse(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(-1234, parsed!.LoudnessValue);
        Assert.Equal(800, parsed.LoudnessRange);
        Assert.Equal(-100, parsed.MaxTruePeakLevel);
        Assert.Equal(-500, parsed.MaxMomentaryLoudness);
        Assert.Equal(-700, parsed.MaxShortTermLoudness);
        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void Parse_V2_RoundTrips()
    {
        var c = MakeBext(version: 2);
        var bytes = c.ToByteArray();
        var parsed = BwfBextChunk.Parse(bytes);
        Assert.NotNull(parsed);
        Assert.Equal((ushort)2, parsed!.Version);
        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void Parse_TruncatedBext_ReturnsNull()
    {
        var c = MakeBext(version: 0);
        var bytes = c.ToByteArray();
        var truncated = bytes.Take(100).ToArray();
        Assert.Null(BwfBextChunk.Parse(truncated));
    }

    [Fact]
    public void Parse_AllZeroUmid_PreservesIt()
    {
        var c = MakeBext(version: 0);
        c.Umid = new byte[64];
        var bytes = c.ToByteArray();
        var parsed = BwfBextChunk.Parse(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(64, parsed!.Umid.Length);
        Assert.All(parsed.Umid, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Parse_MultiLineCodingHistory_RoundTrips()
    {
        var c = MakeBext(version: 1);
        c.CodingHistory = "A=PCM,F=44100,W=16,M=stereo\r\nA=PCM,F=48000,W=24,M=stereo\r\n";
        var bytes = c.ToByteArray();
        var parsed = BwfBextChunk.Parse(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(c.CodingHistory, parsed!.CodingHistory);
        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void Parse_NullArgument_Throws()
        => Assert.Throws<ArgumentNullException>(() => BwfBextChunk.Parse(null!));

    [Fact]
    public void RiffStream_PicksUpBextChunk()
    {
        var c = MakeBext(version: 1);
        c.Description = "Embedded BWF";
        c.Originator = "TestRig";
        c.OriginationDate = "2026-04-18";
        var chunkBytes = c.ToChunkBytes();

        var wav = RiffInfoTagTests.BuildWavWithChunks(chunkBytes);
        using var ms = new MemoryStream(wav);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.NotNull(rs.BextChunk);
        Assert.Equal("Embedded BWF", rs.BextChunk!.Description);
        Assert.Equal("TestRig", rs.BextChunk.Originator);
        Assert.Equal("2026-04-18", rs.BextChunk.OriginationDate);
    }

    [Fact]
    public void RiffStream_TruncatedBextChunk_LeavesPropertyNull()
    {
        // Build a bext chunk with declared size of 50 (much less than required minimum).
        var chunkBytes = new byte[8 + 50];
        chunkBytes[0] = (byte)'b';
        chunkBytes[1] = (byte)'e';
        chunkBytes[2] = (byte)'x';
        chunkBytes[3] = (byte)'t';
        chunkBytes[4] = 50;

        var wav = RiffInfoTagTests.BuildWavWithChunks(chunkBytes);
        using var ms = new MemoryStream(wav);
        var rs = new RiffStream();
        Assert.True(rs.ReadStream(ms));
        Assert.Null(rs.BextChunk);
    }

    private static BwfBextChunk MakeBext(ushort version) => new()
    {
        Description = "Sample description",
        Originator = "AudioVideoLib",
        OriginatorReference = "REF12345",
        OriginationDate = "2026-04-18",
        OriginationTime = "12:34:56",
        TimeReference = 0x123456789ABCDEF0,
        Version = version,
        Umid = [.. Enumerable.Range(0, 64).Select(i => (byte)i)],
        CodingHistory = "A=PCM,F=44100,W=16,M=mono\r\n",
    };
}
