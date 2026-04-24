/*
 * Test suite for Mp4Stream — walks an ISO/IEC 14496-12 (MP4 / M4A) container
 * and exposes the iTunes-style metadata in `moov.udta.meta.ilst`.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

using Xunit;

public class Mp4StreamTests
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    // ================================================================
    // Helpers — synthesise minimal MP4 fixtures inline.
    // ================================================================

    private static byte[] Be32(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];

    private static byte[] Be64(ulong v)
    {
        var b = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            b[i] = (byte)((v >> (56 - (i * 8))) & 0xFF);
        }

        return b;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var len = parts.Sum(p => p.Length);
        var buf = new byte[len];
        var off = 0;
        foreach (var p in parts)
        {
            Array.Copy(p, 0, buf, off, p.Length);
            off += p.Length;
        }

        return buf;
    }

    private static byte[] Wrap(string type, byte[] payload)
    {
        var size = (uint)(8 + payload.Length);
        return Concat(Be32(size), Latin1.GetBytes(type), payload);
    }

    private static byte[] WrapLargeSize(string type, byte[] payload)
    {
        // size==1 marker + 8-byte true size after the type
        var size = (ulong)(16 + payload.Length);
        return Concat(Be32(1), Latin1.GetBytes(type), Be64(size), payload);
    }

    private static byte[] BuildIlstTextChild(string atomType, string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var dataAtomSize = 8u + 8u + (uint)valueBytes.Length;
        var outerSize = 8u + dataAtomSize;
        return Concat(
            Be32(outerSize),
            Latin1.GetBytes(atomType),
            Be32(dataAtomSize),
            Latin1.GetBytes("data"),
            Be32(1),
            Be32(0),
            valueBytes);
    }

    private static byte[] BuildFtyp(string brand) =>
        Wrap("ftyp", Concat(
            Latin1.GetBytes(brand),
            Be32(512), // minor version
            Latin1.GetBytes("isom"),
            Latin1.GetBytes(brand)));

    private static byte[] BuildMeta(byte[] ilstChildren)
    {
        var ilst = Wrap("ilst", ilstChildren);
        // meta is a full-box: 4 bytes version+flags before children.
        var metaPayload = Concat([0, 0, 0, 0], ilst);
        return Wrap("meta", metaPayload);
    }

    private static byte[] BuildBasicMp4(byte[] ilstChildren) =>
        Concat(
            BuildFtyp("M4A "),
            Wrap("moov", Wrap("udta", BuildMeta(ilstChildren))));

    // ================================================================
    // Walks top-level boxes and exposes ftyp brand
    // ================================================================

    [Fact]
    public void ReadStream_BasicFile_DetectsFtypAndMoov()
    {
        var ilst = BuildIlstTextChild("\u00A9nam", "Hello");
        var bytes = BuildBasicMp4(ilst);

        var stream = new Mp4Stream();
        var ms = new MemoryStream(bytes);
        var ok = stream.ReadStream(ms);

        Assert.True(ok);
        Assert.Equal("M4A ", stream.MajorBrand);
        Assert.Equal(2, stream.Boxes.Count);
        Assert.Equal("ftyp", stream.Boxes[0].Type);
        Assert.Equal("moov", stream.Boxes[1].Type);
    }

    [Fact]
    public void ReadStream_FindsIlstAndPopulatesTag()
    {
        var ilst = Concat(
            BuildIlstTextChild("\u00A9nam", "Track Title"),
            BuildIlstTextChild("\u00A9ART", "Track Artist"));
        var bytes = BuildBasicMp4(ilst);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Equal("Track Title", stream.Tag.Title);
        Assert.Equal("Track Artist", stream.Tag.Artist);
    }

    // ================================================================
    // Missing meta / udta / ilst — parses cleanly with empty tag
    // ================================================================

    [Fact]
    public void ReadStream_MoovWithoutUdta_EmptyTag()
    {
        var bytes = Concat(
            BuildFtyp("isom"),
            Wrap("moov", []));

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Null(stream.Tag.Title);
    }

    [Fact]
    public void ReadStream_NoMoov_EmptyTag()
    {
        var bytes = BuildFtyp("isom");

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Null(stream.Tag.Title);
    }

    [Fact]
    public void ReadStream_MetaWithoutIlst_EmptyTag()
    {
        var bytes = Concat(
            BuildFtyp("M4A "),
            Wrap("moov", Wrap("udta", Wrap("meta", [0, 0, 0, 0]))));

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Null(stream.Tag.Title);
    }

    // ================================================================
    // Atom size==0 ("to end of container"): an mdat occupying rest of file
    // ================================================================

    [Fact]
    public void ReadStream_SizeZero_TreatsAsToEnd()
    {
        var ilst = BuildIlstTextChild("\u00A9nam", "X");
        var moov = Wrap("moov", Wrap("udta", BuildMeta(ilst)));
        // Construct an mdat with size==0 — should consume to EOF.
        var mdatHeader = Concat(Be32(0), Latin1.GetBytes("mdat"));
        var bytes = Concat(BuildFtyp("M4A "), moov, mdatHeader, new byte[16]);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Equal("X", stream.Tag.Title);
        Assert.Contains(stream.Boxes, b => b.Type == "mdat");
    }

    // ================================================================
    // Atom size==1 (64-bit extended size)
    // ================================================================

    [Fact]
    public void ReadStream_SizeOneExtended_HandlesLargeSizeField()
    {
        var ilst = BuildIlstTextChild("\u00A9nam", "Big");
        var meta = BuildMeta(ilst);
        // Wrap udta with extended size, then moov normally.
        var udtaLarge = WrapLargeSize("udta", meta);
        var moov = Wrap("moov", udtaLarge);
        var bytes = Concat(BuildFtyp("M4A "), moov);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        Assert.Equal("Big", stream.Tag.Title);
    }

    // ================================================================
    // Truncated atom (size > remaining)
    // ================================================================

    [Fact]
    public void ReadStream_TruncatedAtomSize_DoesNotCrash()
    {
        // moov claims size=1000 but only ~20 bytes follow.
        var ftyp = BuildFtyp("M4A ");
        var truncMoov = Concat(Be32(1000), Latin1.GetBytes("moov"), new byte[12]);
        var bytes = Concat(ftyp, truncMoov);

        var stream = new Mp4Stream();
        // Should not throw. May or may not return true depending on clamping.
        var _ = stream.ReadStream(new MemoryStream(bytes));
        Assert.NotNull(stream.Tag);
    }

    // ================================================================
    // mvhd-derived duration
    // ================================================================

    [Fact]
    public void ReadStream_ParsesMvhdDuration()
    {
        // mvhd v0: version(1)=0, flags(3)=0, ctime(4), mtime(4), timescale(4)=1000, duration(4)=5000, rest...
        var mvhdPayload = new byte[100];
        mvhdPayload[12] = 0x00;
        mvhdPayload[13] = 0x00;
        mvhdPayload[14] = 0x03;
        mvhdPayload[15] = 0xE8; // timescale = 1000
        mvhdPayload[16] = 0x00;
        mvhdPayload[17] = 0x00;
        mvhdPayload[18] = 0x13;
        mvhdPayload[19] = 0x88; // duration = 5000
        var mvhd = Wrap("mvhd", mvhdPayload);
        var moov = Wrap("moov", mvhd);
        var bytes = Concat(BuildFtyp("M4A "), moov);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));

        // 5000 / 1000 * 1000ms = 5000ms
        Assert.Equal(5000, stream.TotalDuration);
    }

    // ================================================================
    // Round-trip: build → parse → serialise → byte-equal compare
    // ================================================================

    [Fact]
    public void RoundTrip_TwoKnownItems_BytesAreIdentical()
    {
        var ilst = Concat(
            BuildIlstTextChild("\u00A9nam", "Roundtrip Title"),
            BuildIlstTextChild("\u00A9ART", "Roundtrip Artist"));
        var original = BuildBasicMp4(ilst);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(original)));

        var rewritten = stream.ToByteArray();

        Assert.Equal(original, rewritten);
    }

    [Fact]
    public void RoundTrip_AfterMutation_FileIsStillWalkable()
    {
        var ilst = BuildIlstTextChild("\u00A9nam", "Original");
        var original = BuildBasicMp4(ilst);

        var stream = new Mp4Stream();
        Assert.True(stream.ReadStream(new MemoryStream(original)));

        stream.Tag.Title = "Mutated";
        stream.Tag.Artist = "New Artist";
        var rewritten = stream.ToByteArray();

        var verify = new Mp4Stream();
        Assert.True(verify.ReadStream(new MemoryStream(rewritten)));
        Assert.Equal("Mutated", verify.Tag.Title);
        Assert.Equal("New Artist", verify.Tag.Artist);
    }

    // ================================================================
    // IMediaContainer contract
    // ================================================================

    [Fact]
    public void IAudioStream_ExposesOffsetsAndSize()
    {
        var bytes = BuildBasicMp4(BuildIlstTextChild("\u00A9nam", "X"));

        var stream = new Mp4Stream();
        var ms = new MemoryStream(bytes);
        Assert.True(stream.ReadStream(ms));

        Assert.Equal(0, stream.StartOffset);
        Assert.Equal(bytes.Length, stream.EndOffset);
        Assert.Equal(bytes.Length, stream.TotalMediaSize);
    }

    [Fact]
    public void ReadStream_NullStream_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new Mp4Stream().ReadStream(null!));

    [Fact]
    public void ReadStream_NonMp4Bytes_ReturnsFalse()
    {
        var bytes = new byte[] { 0x49, 0x44, 0x33, 0x04, 0, 0, 0, 0, 0, 0, 0, 0 }; // ID3 tag header

        var stream = new Mp4Stream();
        Assert.False(stream.ReadStream(new MemoryStream(bytes)));
    }
}
