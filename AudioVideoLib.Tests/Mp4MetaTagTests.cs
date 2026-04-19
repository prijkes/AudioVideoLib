/*
 * Test suite for Mp4MetaTag — parsing and serialising the body of an MP4 / M4A
 * `moov.udta.meta.ilst` atom. Synthesises ilst payloads inline so no external fixtures are required.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

using Xunit;

public class Mp4MetaTagTests
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    // ================================================================
    // Helpers
    // ================================================================

    private static byte[] Be32(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];

    private static byte[] BuildIlstTextChild(string atomType, string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return BuildIlstDataChild(atomType, 1, valueBytes);
    }

    private static byte[] BuildIlstDataChild(string atomType, uint dataType, byte[] payload)
    {
        var dataAtomSize = 8u + 8u + (uint)payload.Length;
        var outerSize = 8u + dataAtomSize;
        using var ms = new MemoryStream();
        ms.Write(Be32(outerSize), 0, 4);
        ms.Write(Latin1.GetBytes(atomType), 0, 4);
        ms.Write(Be32(dataAtomSize), 0, 4);
        ms.Write(Latin1.GetBytes("data"), 0, 4);
        ms.Write(Be32(dataType), 0, 4);
        ms.Write(Be32(0), 0, 4);
        ms.Write(payload, 0, payload.Length);
        return ms.ToArray();
    }

    private static byte[] BuildFreeFormChild(string mean, string name, string value)
    {
        var meanBytes = Encoding.UTF8.GetBytes(mean);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var valueBytes = Encoding.UTF8.GetBytes(value);

        var meanAtomSize = 8u + 4u + (uint)meanBytes.Length;
        var nameAtomSize = 8u + 4u + (uint)nameBytes.Length;
        var dataAtomSize = 8u + 8u + (uint)valueBytes.Length;
        var outerSize = 8u + meanAtomSize + nameAtomSize + dataAtomSize;

        using var ms = new MemoryStream();
        ms.Write(Be32(outerSize), 0, 4);
        ms.Write(Latin1.GetBytes("----"), 0, 4);

        ms.Write(Be32(meanAtomSize), 0, 4);
        ms.Write(Latin1.GetBytes("mean"), 0, 4);
        ms.Write(Be32(0), 0, 4);
        ms.Write(meanBytes, 0, meanBytes.Length);

        ms.Write(Be32(nameAtomSize), 0, 4);
        ms.Write(Latin1.GetBytes("name"), 0, 4);
        ms.Write(Be32(0), 0, 4);
        ms.Write(nameBytes, 0, nameBytes.Length);

        ms.Write(Be32(dataAtomSize), 0, 4);
        ms.Write(Latin1.GetBytes("data"), 0, 4);
        ms.Write(Be32(1), 0, 4);
        ms.Write(Be32(0), 0, 4);
        ms.Write(valueBytes, 0, valueBytes.Length);
        return ms.ToArray();
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

    // ================================================================
    // Empty ilst
    // ================================================================

    [Fact]
    public void Parse_EmptyIlst_ReturnsEmptyTag()
    {
        var tag = Mp4MetaTag.Parse([]);

        Assert.NotNull(tag);
        Assert.Empty(tag.Items);
        Assert.Null(tag.Title);
        Assert.Null(tag.Artist);
        Assert.Empty(tag.CoverArt);
        Assert.Empty(tag.FreeFormItems);
    }

    // ================================================================
    // Standard text atoms
    // ================================================================

    [Fact]
    public void Parse_AllStandardTextAtoms_PopulatesProperties()
    {
        var ilst = Concat(
            BuildIlstTextChild("\u00A9nam", "My Song"),
            BuildIlstTextChild("\u00A9ART", "My Artist"),
            BuildIlstTextChild("\u00A9alb", "My Album"),
            BuildIlstTextChild("aART", "Album Artist"),
            BuildIlstTextChild("\u00A9day", "2026"),
            BuildIlstTextChild("\u00A9cmt", "comment"),
            BuildIlstTextChild("\u00A9wrt", "composer"),
            BuildIlstTextChild("\u00A9too", "iTunes 1.0"));

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal("My Song", tag.Title);
        Assert.Equal("My Artist", tag.Artist);
        Assert.Equal("My Album", tag.Album);
        Assert.Equal("Album Artist", tag.AlbumArtist);
        Assert.Equal("2026", tag.Year);
        Assert.Equal("comment", tag.Comment);
        Assert.Equal("composer", tag.Composer);
        Assert.Equal("iTunes 1.0", tag.Tool);
    }

    // ================================================================
    // trkn / disk
    // ================================================================

    [Fact]
    public void Parse_TrknDisk_DecodesIndexAndTotal()
    {
        var trknPayload = Mp4MetaItem.BuildIndexTotalPayload(3, 12);
        var diskPayload = Mp4MetaItem.BuildIndexTotalPayload(1, 2);

        var ilst = Concat(
            BuildIlstDataChild("trkn", 0, trknPayload),
            BuildIlstDataChild("disk", 0, diskPayload));

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal(3, tag.TrackNumber);
        Assert.Equal(12, tag.TrackTotal);
        Assert.Equal(1, tag.DiscNumber);
        Assert.Equal(2, tag.DiscTotal);
    }

    // ================================================================
    // tmpo (BPM)
    // ================================================================

    [Fact]
    public void Parse_Tmpo_DecodesBpm()
    {
        var ilst = BuildIlstDataChild("tmpo", 21, [0x00, 0x80]); // 128

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal(128, tag.Bpm);
    }

    // ================================================================
    // cpil (compilation)
    // ================================================================

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Parse_Cpil_DecodesBoolean(byte payloadByte, bool expected)
    {
        var ilst = BuildIlstDataChild("cpil", 21, [payloadByte]);

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal(expected, tag.Compilation);
    }

    // ================================================================
    // covr — JPEG, PNG, multiple
    // ================================================================

    [Fact]
    public void Parse_Covr_Jpeg_IdentifiesFormat()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x10, 0x20 };
        var ilst = BuildIlstDataChild("covr", 13, jpeg);

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Single(tag.CoverArt);
        Assert.Equal(Mp4CoverArtFormat.Jpeg, tag.CoverArt[0].Format);
        Assert.Equal(jpeg, tag.CoverArt[0].Data);
    }

    [Fact]
    public void Parse_Covr_Png_IdentifiesFormat()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var ilst = BuildIlstDataChild("covr", 14, png);

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Single(tag.CoverArt);
        Assert.Equal(Mp4CoverArtFormat.Png, tag.CoverArt[0].Format);
    }

    [Fact]
    public void Parse_MultipleCovr_AllPreserved()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x10 };
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };

        var ilst = Concat(
            BuildIlstDataChild("covr", 13, jpeg),
            BuildIlstDataChild("covr", 14, png));

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal(2, tag.CoverArt.Count);
        Assert.Equal(Mp4CoverArtFormat.Jpeg, tag.CoverArt[0].Format);
        Assert.Equal(Mp4CoverArtFormat.Png, tag.CoverArt[1].Format);
    }

    // ================================================================
    // Free-form ----
    // ================================================================

    [Fact]
    public void Parse_FreeFormItem_PopulatesDictionary()
    {
        var ilst = BuildFreeFormChild("com.apple.iTunes", "MusicBrainz Track Id", "abc-123");

        var tag = Mp4MetaTag.Parse(ilst);

        var key = new Mp4FreeFormKey("com.apple.iTunes", "MusicBrainz Track Id");
        Assert.True(tag.FreeFormItems.ContainsKey(key));
        Assert.Equal("abc-123", tag.FreeFormItems[key]);
    }

    // ================================================================
    // gnre numeric genre fallback
    // ================================================================

    [Fact]
    public void Parse_GnreNumeric_BecomesGenreString()
    {
        // gnre payload is a 2-byte big-endian integer (1-based ID3v1 index).
        var ilst = BuildIlstDataChild("gnre", 0, [0x00, 0x11]); // 17

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal("17", tag.Genre);
    }

    [Fact]
    public void Parse_GnreThenCgen_TextualGenreWins()
    {
        var ilst = Concat(
            BuildIlstTextChild("\u00A9gen", "Rock"),
            BuildIlstDataChild("gnre", 0, [0x00, 0x11]));

        var tag = Mp4MetaTag.Parse(ilst);

        Assert.Equal("Rock", tag.Genre);
    }

    // ================================================================
    // Round-trip: build → serialise → parse → equal
    // ================================================================

    [Fact]
    public void RoundTrip_PropertiesPreserved()
    {
        var original = new Mp4MetaTag
        {
            Title = "Title",
            Artist = "Artist",
            Album = "Album",
            AlbumArtist = "Album Artist",
            Year = "2026",
            Genre = "Electronic",
            Comment = "Hello",
            Composer = "C.O.M.P",
            Tool = "Lib 1.0",
            TrackNumber = 5,
            TrackTotal = 10,
            DiscNumber = 1,
            DiscTotal = 2,
            Bpm = 140,
            Compilation = true,
        };
        original.CoverArt.Add(new Mp4CoverArt([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3], Mp4CoverArtFormat.Jpeg));
        original.SetFreeFormItem("com.apple.iTunes", "ISRC", "USRC17600001");

        var bytes = original.ToByteArray();
        var roundTripped = Mp4MetaTag.Parse(bytes);

        Assert.Equal(original.Title, roundTripped.Title);
        Assert.Equal(original.Artist, roundTripped.Artist);
        Assert.Equal(original.Album, roundTripped.Album);
        Assert.Equal(original.AlbumArtist, roundTripped.AlbumArtist);
        Assert.Equal(original.Year, roundTripped.Year);
        Assert.Equal(original.Genre, roundTripped.Genre);
        Assert.Equal(original.Comment, roundTripped.Comment);
        Assert.Equal(original.Composer, roundTripped.Composer);
        Assert.Equal(original.Tool, roundTripped.Tool);
        Assert.Equal(original.TrackNumber, roundTripped.TrackNumber);
        Assert.Equal(original.TrackTotal, roundTripped.TrackTotal);
        Assert.Equal(original.DiscNumber, roundTripped.DiscNumber);
        Assert.Equal(original.DiscTotal, roundTripped.DiscTotal);
        Assert.Equal(original.Bpm, roundTripped.Bpm);
        Assert.Equal(original.Compilation, roundTripped.Compilation);
        Assert.Single(roundTripped.CoverArt);
        Assert.Equal(Mp4CoverArtFormat.Jpeg, roundTripped.CoverArt[0].Format);
        Assert.Equal("USRC17600001", roundTripped.FreeFormItems[new Mp4FreeFormKey("com.apple.iTunes", "ISRC")]);
    }

    [Fact]
    public void RoundTrip_BytesAreStable()
    {
        var tag = new Mp4MetaTag
        {
            Title = "T",
            Artist = "A",
        };

        var first = tag.ToByteArray();
        var reparsed = Mp4MetaTag.Parse(first);
        var second = reparsed.ToByteArray();

        Assert.Equal(first, second);
    }

    // ================================================================
    // Truncated atom — ignored, doesn't crash
    // ================================================================

    [Fact]
    public void Parse_TruncatedChildSize_StopsCleanly()
    {
        // Declares size 1000 but only ~30 bytes follow.
        var bad = Concat(
            Be32(1000),
            Latin1.GetBytes("\u00A9nam"),
            new byte[18]);

        var tag = Mp4MetaTag.Parse(bad);

        Assert.Null(tag.Title);
    }
}
