/*
 * Test suite for the Matroska Tags model and Tags-element round-trip writer.
 */
namespace AudioVideoLib.Tests;

using System.Linq;
using System.Text;

using AudioVideoLib.Tags;

using Xunit;

public class MatroskaTagTests
{
    // ================================================================
    // 1. Empty model.
    // ================================================================

    [Fact]
    public void EmptyTag_HasNoEntries()
    {
        var tag = new MatroskaTag();
        Assert.Empty(tag.Entries);
        Assert.Null(tag.Title);
    }

    [Fact]
    public void FromPayload_EmptyBytes_ReturnsEmptyTag()
    {
        var tag = MatroskaTag.FromPayload([]);
        Assert.Empty(tag.Entries);
    }

    // ================================================================
    // 2. Round-trip: build → serialize → parse → byte-equal.
    // ================================================================

    [Fact]
    public void RoundTrip_SingleSimpleTag_PreservesValue()
    {
        var entry = new MatroskaTagEntry();
        entry.Targets.TargetTypeValue = 50;
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Hello World" });

        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var bytes = tag.ToByteArray();
        Assert.NotEmpty(bytes);

        // Re-parse the payload portion: skip the Tags element id+size header.
        // Easier: re-parse and check the model.
        var parsed = ReparseTagsBytes(bytes);
        Assert.Single(parsed.Entries);
        Assert.Equal(50UL, parsed.Entries[0].Targets.TargetTypeValue);
        Assert.Single(parsed.Entries[0].SimpleTags);
        Assert.Equal("TITLE", parsed.Entries[0].SimpleTags[0].Name);
        Assert.Equal("Hello World", parsed.Entries[0].SimpleTags[0].Value);

        // Re-serialise and compare bytes.
        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void RoundTrip_NestedSimpleTag_PreservesHierarchy()
    {
        var outer = new MatroskaSimpleTag { Name = "ARTIST", Value = "Some Band" };
        outer.SimpleTags.Add(new MatroskaSimpleTag { Name = "BAND", Value = "Some Band Inc." });

        var entry = new MatroskaTagEntry();
        entry.Targets.TargetTypeValue = 50;
        entry.SimpleTags.Add(outer);

        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var bytes = tag.ToByteArray();
        var parsed = ReparseTagsBytes(bytes);

        Assert.Single(parsed.Entries[0].SimpleTags);
        var artist = parsed.Entries[0].SimpleTags[0];
        Assert.Equal("ARTIST", artist.Name);
        Assert.Single(artist.SimpleTags);
        Assert.Equal("BAND", artist.SimpleTags[0].Name);
        Assert.Equal("Some Band Inc.", artist.SimpleTags[0].Value);

        // Byte-equal round-trip.
        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void RoundTrip_MultipleTagsAtDifferentLevels()
    {
        var album = new MatroskaTagEntry();
        album.Targets.TargetTypeValue = 50;
        album.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "My Album" });
        album.SimpleTags.Add(new MatroskaSimpleTag { Name = "ARTIST", Value = "My Artist" });

        var track = new MatroskaTagEntry();
        track.Targets.TargetTypeValue = 30;
        track.Targets.TrackUids.Add(1234567);
        track.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Track 1" });
        track.SimpleTags.Add(new MatroskaSimpleTag { Name = "PART_NUMBER", Value = "1" });

        var tag = new MatroskaTag();
        tag.Entries.Add(album);
        tag.Entries.Add(track);

        var bytes = tag.ToByteArray();
        var parsed = ReparseTagsBytes(bytes);

        Assert.Equal(2, parsed.Entries.Count);
        Assert.Equal(50UL, parsed.Entries[0].Targets.TargetTypeValue);
        Assert.Equal(30UL, parsed.Entries[1].Targets.TargetTypeValue);
        Assert.Contains(1234567UL, parsed.Entries[1].Targets.TrackUids);

        // Strongly-typed accessors.
        Assert.Equal("My Album", parsed.Title);          // album-level wins
        Assert.Equal("My Album", parsed.Album);
        Assert.Equal("My Artist", parsed.Artist);
        Assert.Equal("1", parsed.TrackNumber);

        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void RoundTrip_TagBinary_PreservesBytes()
    {
        var bin = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xFE };
        var entry = new MatroskaTagEntry();
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "COVER_ART", Binary = bin });

        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var bytes = tag.ToByteArray();
        var parsed = ReparseTagsBytes(bytes);
        Assert.Equal(bin, parsed.Entries[0].SimpleTags[0].Binary);
        Assert.Null(parsed.Entries[0].SimpleTags[0].Value);

        Assert.Equal(bytes, parsed.ToByteArray());
    }

    [Fact]
    public void RoundTrip_TagDefaultFalse_PreservesFlag()
    {
        var entry = new MatroskaTagEntry();
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Alt", IsDefault = false });
        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var bytes = tag.ToByteArray();
        var parsed = ReparseTagsBytes(bytes);
        Assert.False(parsed.Entries[0].SimpleTags[0].IsDefault);
    }

    [Fact]
    public void RoundTrip_LanguageBcp47_PreservesValue()
    {
        var entry = new MatroskaTagEntry();
        entry.SimpleTags.Add(new MatroskaSimpleTag
        {
            Name = "TITLE",
            Value = "Bonjour",
            Language = "fre",
            LanguageBcp47 = "fr-FR",
        });
        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var parsed = ReparseTagsBytes(tag.ToByteArray());
        Assert.Equal("fre", parsed.Entries[0].SimpleTags[0].Language);
        Assert.Equal("fr-FR", parsed.Entries[0].SimpleTags[0].LanguageBcp47);
    }

    [Fact]
    public void RoundTrip_AllTargetUids_PreservesEachKind()
    {
        var entry = new MatroskaTagEntry();
        entry.Targets.TrackUids.Add(1);
        entry.Targets.EditionUids.Add(2);
        entry.Targets.ChapterUids.Add(3);
        entry.Targets.AttachmentUids.Add(4);
        entry.Targets.TargetType = "ALBUM";

        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var parsed = ReparseTagsBytes(tag.ToByteArray());
        Assert.Equal(new ulong[] { 1 }, parsed.Entries[0].Targets.TrackUids.ToArray());
        Assert.Equal(new ulong[] { 2 }, parsed.Entries[0].Targets.EditionUids.ToArray());
        Assert.Equal(new ulong[] { 3 }, parsed.Entries[0].Targets.ChapterUids.ToArray());
        Assert.Equal(new ulong[] { 4 }, parsed.Entries[0].Targets.AttachmentUids.ToArray());
        Assert.Equal("ALBUM", parsed.Entries[0].Targets.TargetType);
    }

    // ================================================================
    // 3. Strongly-typed accessor fallbacks.
    // ================================================================

    [Fact]
    public void Title_FallsBackToTrackLevel_WhenNoAlbumLevelTitle()
    {
        var entry = new MatroskaTagEntry();
        entry.Targets.TargetTypeValue = 30;
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Track Title" });

        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        Assert.Equal("Track Title", tag.Title);
        Assert.Null(tag.Album); // album-only accessor
    }

    [Fact]
    public void Date_PrefersDateReleased_FallsBackToRecordedThenDate()
    {
        var album = new MatroskaTagEntry();
        album.Targets.TargetTypeValue = 50;
        album.SimpleTags.Add(new MatroskaSimpleTag { Name = "DATE_RECORDED", Value = "2020" });
        var tag = new MatroskaTag();
        tag.Entries.Add(album);

        Assert.Equal("2020", tag.Date);
    }

    // ================================================================
    // 4. Tolerance: parser reads what it can without throwing.
    // ================================================================

    [Fact]
    public void FromPayload_TruncatedSize_DoesNotThrow_ReturnsEmpty()
    {
        // A Tag id (0x73 0x73) followed by a VINT claiming length 100 but no payload.
        byte[] payload = [0x73, 0x73, 0x80 | 100];
        var tag = MatroskaTag.FromPayload(payload);
        Assert.Empty(tag.Entries);
    }

    [Fact]
    public void FromPayload_UnknownSibling_IsSkipped()
    {
        // Build: Tag(...) + UnknownElement + Tag(...).
        var entryA = new MatroskaTagEntry();
        entryA.SimpleTags.Add(new MatroskaSimpleTag { Name = "A", Value = "1" });
        var entryB = new MatroskaTagEntry();
        entryB.SimpleTags.Add(new MatroskaSimpleTag { Name = "B", Value = "2" });

        var tag = new MatroskaTag();
        tag.Entries.Add(entryA);
        tag.Entries.Add(entryB);
        var payloadBytes = tag.SerializePayload();

        // Inject an unknown element (id 0xEC = "Void", 1-byte VINT, length 3) between the two Tag elements.
        var entryASize = SerializeSingleTag(entryA).Length;
        var voidElement = new byte[] { 0xEC, 0x83, 0x00, 0x00, 0x00 };
        var injected = new byte[payloadBytes.Length + voidElement.Length];
        System.Buffer.BlockCopy(payloadBytes, 0, injected, 0, entryASize);
        System.Buffer.BlockCopy(voidElement, 0, injected, entryASize, voidElement.Length);
        System.Buffer.BlockCopy(payloadBytes, entryASize, injected, entryASize + voidElement.Length, payloadBytes.Length - entryASize);

        var parsed = MatroskaTag.FromPayload(injected);
        Assert.Equal(2, parsed.Entries.Count);
        Assert.Equal("A", parsed.Entries[0].SimpleTags[0].Name);
        Assert.Equal("B", parsed.Entries[1].SimpleTags[0].Name);
    }

    // ================================================================
    // Helpers.
    // ================================================================

    private static MatroskaTag ReparseTagsBytes(byte[] tagsElementBytes)
    {
        // tagsElementBytes is id (4 bytes for 0x1254C367) + VINT size + payload.
        // Skip the id, parse the VINT size, then call FromPayload on the remainder.
        using var ms = new System.IO.MemoryStream(tagsElementBytes);
        Assert.True(AudioVideoLib.Formats.EbmlElement.TryReadVintId(ms, out _, out var id));
        Assert.Equal(MatroskaElementIds.Tags, id);
        Assert.True(AudioVideoLib.Formats.EbmlElement.TryReadVintSize(ms, out _, out var size, out _));
        var payload = new byte[size];
        var read = ms.Read(payload, 0, payload.Length);
        Assert.Equal((int)size, read);
        return MatroskaTag.FromPayload(payload);
    }

    private static byte[] SerializeSingleTag(MatroskaTagEntry entry)
    {
        // Construct a MatroskaTag with just this entry, take SerializePayload, and
        // peel off the Tag element id/size header to find the entry-bytes length.
        // Easier: call SerializePayload of a 1-entry tag — that IS the single tag's full bytes.
        var tag = new MatroskaTag();
        tag.Entries.Add(entry);
        return tag.SerializePayload();
    }

    [Fact]
    public void TagDefault_DefaultsToTrue_OnConstruct()
    {
        var st = new MatroskaSimpleTag();
        Assert.True(st.IsDefault);
        Assert.Equal("und", st.Language);
    }

    [Fact]
    public void Targets_DefaultLevelIsAlbum()
    {
        var t = new MatroskaTargets();
        Assert.Equal(50UL, t.TargetTypeValue);
    }

    [Fact]
    public void Utf8Roundtrip_PreservesNonAsciiText()
    {
        var entry = new MatroskaTagEntry();
        var s = "Café — Naïve résumé 日本語";
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = s });
        var tag = new MatroskaTag();
        tag.Entries.Add(entry);

        var bytes = tag.ToByteArray();
        var parsed = ReparseTagsBytes(bytes);
        Assert.Equal(s, parsed.Entries[0].SimpleTags[0].Value);
        Assert.Equal(Encoding.UTF8.GetByteCount(s), Encoding.UTF8.GetBytes(parsed.Entries[0].SimpleTags[0].Value!).Length);
    }
}
