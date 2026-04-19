/*
 * Test suite for ASF metadata-bearing objects:
 *   Content Description Object (CDO),
 *   Extended Content Description Object (ECDO),
 *   Metadata Object (MO),
 *   Metadata Library Object (MLO).
 */
namespace AudioVideoLib.Tests;

using System;
using System.Linq;
using System.Text;

using AudioVideoLib.Tags;

using Xunit;

public class AsfMetadataTagTests
{
    // ================================================================
    // 1. GUID encoding sanity — the Guid binary form must match the
    //    standard ASF mixed-endian layout.
    // ================================================================

    [Fact]
    public void Guid_BinaryRoundTrip_MatchesAsfMixedEndianLayout()
    {
        // Header Object: 75B22630-668E-11CF-A6D9-00AA0062CE6C.
        // First 3 fields little-endian, last 2 big-endian (8-byte tail copied verbatim).
        byte[] expected =
        [
            0x30, 0x26, 0xB2, 0x75,
            0x8E, 0x66,
            0xCF, 0x11,
            0xA6, 0xD9,
            0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C,
        ];

        Assert.Equal(expected, AsfMetadataTag.HeaderObjectGuid.ToByteArray());
        Assert.Equal(AsfMetadataTag.HeaderObjectGuid, new Guid(expected));
    }

    // ================================================================
    // 2. Content Description Object (CDO)
    // ================================================================

    [Fact]
    public void Cdo_RoundTrips_AllFiveFields()
    {
        var tag = new AsfMetadataTag
        {
            Title = "My Title",
            Author = "Jane Doe",
            Copyright = "(c) 2026",
            Description = "A short description.",
            Rating = "PG",
        };

        var payload = tag.BuildContentDescriptionPayload();
        var parsed = new AsfMetadataTag();
        parsed.ParseContentDescription(payload);

        Assert.Equal("My Title", parsed.Title);
        Assert.Equal("Jane Doe", parsed.Author);
        Assert.Equal("(c) 2026", parsed.Copyright);
        Assert.Equal("A short description.", parsed.Description);
        Assert.Equal("PG", parsed.Rating);
    }

    [Fact]
    public void Cdo_OnlyTitleSet_OtherFieldsAreNull()
    {
        var tag = new AsfMetadataTag { Title = "Solo" };
        var parsed = new AsfMetadataTag();
        parsed.ParseContentDescription(tag.BuildContentDescriptionPayload());

        Assert.Equal("Solo", parsed.Title);
        Assert.Null(parsed.Author);
        Assert.Null(parsed.Copyright);
        Assert.Null(parsed.Description);
        Assert.Null(parsed.Rating);
    }

    [Fact]
    public void Cdo_MissingNullTerminator_StillDecodes()
    {
        // Fabricate a CDO payload by hand without the trailing UTF-16LE null.
        var titleBytes = Encoding.Unicode.GetBytes("NoNull");
        var payload = new byte[10 + titleBytes.Length];
        // titleLen = titleBytes.Length (no 2-byte terminator counted).
        payload[0] = (byte)(titleBytes.Length & 0xFF);
        payload[1] = (byte)((titleBytes.Length >> 8) & 0xFF);
        Buffer.BlockCopy(titleBytes, 0, payload, 10, titleBytes.Length);

        var tag = new AsfMetadataTag();
        tag.ParseContentDescription(payload);
        Assert.Equal("NoNull", tag.Title);
    }

    [Fact]
    public void Cdo_TruncatedTitleField_ReturnsTruncatedString()
    {
        // Declare 100 bytes of title but supply only 10 actual bytes (5 UTF-16 chars).
        var titleBytes = Encoding.Unicode.GetBytes("Hello"); // 10 bytes
        var payload = new byte[10 + titleBytes.Length];
        payload[0] = 100; // claim 100 bytes (oversized)
        Buffer.BlockCopy(titleBytes, 0, payload, 10, titleBytes.Length);

        var tag = new AsfMetadataTag();
        tag.ParseContentDescription(payload);
        Assert.Equal("Hello", tag.Title);
    }

    // ================================================================
    // 3. Extended Content Description Object (ECDO) — every data type
    // ================================================================

    [Fact]
    public void Ecdo_UnicodeStringValue_RoundTrips()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/AlbumTitle", AsfTypedValue.FromString("Greatest Hits"));
        var parsed = ParseEcdo(tag);

        Assert.Single(parsed.ExtendedItems);
        Assert.Equal("WM/AlbumTitle", parsed.ExtendedItems[0].Key);
        Assert.Equal(AsfDataType.UnicodeString, parsed.ExtendedItems[0].Value.Type);
        Assert.Equal("Greatest Hits", parsed.ExtendedItems[0].Value.AsString);
    }

    [Fact]
    public void Ecdo_BytesValue_RoundTrips()
    {
        var bytes = new byte[] { 1, 2, 3, 0xFF, 0xFE };
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/Picture", AsfTypedValue.FromBytes(bytes));
        var parsed = ParseEcdo(tag);

        Assert.Equal(AsfDataType.Bytes, parsed.ExtendedItems[0].Value.Type);
        Assert.Equal(bytes, parsed.ExtendedItems[0].Value.AsBytes);
    }

    [Fact]
    public void Ecdo_BoolValue_StoredAs32Bit_RoundTrips()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("IsCompilation", AsfTypedValue.FromBool(true));
        var bytes = tag.BuildExtendedContentDescriptionPayload();

        // Value length WORD must be 4 (BOOL on disk is 32-bit).
        // Layout: [0..1]=count(1), [2..3]=nameLen, name..., [..]=dataType=2, [..]=valueLen=4, [..]=value=01000000
        // Find the trailing 4-byte value: it should equal {0x01, 0, 0, 0}.
        Assert.Equal(0x01, bytes[^4]);
        Assert.Equal(0, bytes[^3]);
        Assert.Equal(0, bytes[^2]);
        Assert.Equal(0, bytes[^1]);

        var parsed = ParseEcdo(tag);
        Assert.True(parsed.ExtendedItems[0].Value.AsBool);
    }

    [Fact]
    public void Ecdo_DwordValue_RoundTrips()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/TrackNumber", AsfTypedValue.FromDword(0xDEADBEEF));
        var parsed = ParseEcdo(tag);

        Assert.Equal(AsfDataType.Dword, parsed.ExtendedItems[0].Value.Type);
        Assert.Equal(0xDEADBEEFu, parsed.ExtendedItems[0].Value.AsDword);
    }

    [Fact]
    public void Ecdo_QwordValue_RoundTrips()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/EncodingTime", AsfTypedValue.FromQword(0x0102030405060708UL));
        var parsed = ParseEcdo(tag);

        Assert.Equal(AsfDataType.Qword, parsed.ExtendedItems[0].Value.Type);
        Assert.Equal(0x0102030405060708UL, parsed.ExtendedItems[0].Value.AsQword);
    }

    [Fact]
    public void Ecdo_WordValue_RoundTrips()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/Year16", AsfTypedValue.FromWord(2026));
        var parsed = ParseEcdo(tag);

        Assert.Equal(AsfDataType.Word, parsed.ExtendedItems[0].Value.Type);
        Assert.Equal((ushort)2026, parsed.ExtendedItems[0].Value.AsWord);
    }

    [Fact]
    public void Ecdo_AllSixDataTypes_RoundTripTogether()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("Name", AsfTypedValue.FromString("Hi"));
        tag.AddExtended("Pic", AsfTypedValue.FromBytes([0xAA, 0xBB]));
        tag.AddExtended("Flag", AsfTypedValue.FromBool(false));
        tag.AddExtended("Count", AsfTypedValue.FromDword(42));
        tag.AddExtended("Time", AsfTypedValue.FromQword(1234567890123UL));
        tag.AddExtended("Year", AsfTypedValue.FromWord(2026));

        var parsed = ParseEcdo(tag);
        Assert.Equal(6, parsed.ExtendedItems.Count);

        Assert.Equal("Hi", parsed.ExtendedItems[0].Value.AsString);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, parsed.ExtendedItems[1].Value.AsBytes);
        Assert.False(parsed.ExtendedItems[2].Value.AsBool);
        Assert.Equal(42u, parsed.ExtendedItems[3].Value.AsDword);
        Assert.Equal(1234567890123UL, parsed.ExtendedItems[4].Value.AsQword);
        Assert.Equal((ushort)2026, parsed.ExtendedItems[5].Value.AsWord);
    }

    [Fact]
    public void Ecdo_DuplicateKeys_BothPreservedInList_FirstWinsInDictionary()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/Genre", AsfTypedValue.FromString("Rock"));
        tag.AddExtended("WM/Genre", AsfTypedValue.FromString("Metal"));

        var parsed = ParseEcdo(tag);
        Assert.Equal(2, parsed.ExtendedItems.Count);
        Assert.Equal("Rock", parsed.ExtendedItems[0].Value.AsString);
        Assert.Equal("Metal", parsed.ExtendedItems[1].Value.AsString);

        var dict = parsed.Extended;
        Assert.Single(dict);
        Assert.Equal("Rock", dict["WM/Genre"].AsString);
    }

    [Fact]
    public void Ecdo_OversizedCount_ReturnsAvailableItemsWithoutThrowing()
    {
        // Build a valid 1-item ECDO payload then bump the count word to 99.
        var tag = new AsfMetadataTag();
        tag.AddExtended("Name", AsfTypedValue.FromString("Only"));
        var payload = tag.BuildExtendedContentDescriptionPayload();
        payload[0] = 99;
        payload[1] = 0;

        var parsed = new AsfMetadataTag();
        parsed.ParseExtendedContentDescription(payload);

        // Walker should stop after the first valid item without throwing.
        Assert.Single(parsed.ExtendedItems);
        Assert.Equal("Only", parsed.ExtendedItems[0].Value.AsString);
    }

    [Fact]
    public void Ecdo_TruncatedNameLength_StopsCleanly()
    {
        // count=1, nameLen=100 (oversized), then we abruptly end the payload.
        byte[] payload = [0x01, 0x00, 0x64, 0x00];
        var tag = new AsfMetadataTag();
        tag.ParseExtendedContentDescription(payload);
        Assert.Empty(tag.ExtendedItems);
    }

    // ================================================================
    // 4. Metadata Object / Metadata Library Object
    // ================================================================

    [Fact]
    public void MetadataObject_StreamNumberAndLanguageIndex_RoundTrip()
    {
        var tag = new AsfMetadataTag();
        tag.AddMetadata(new AsfMetadataItem(0, 7, "WM/PartOfSet", AsfTypedValue.FromDword(3)));
        tag.AddMetadata(new AsfMetadataItem(0, 0, "WM/Mood", AsfTypedValue.FromString("Calm")));

        var bytes = AsfMetadataTag.BuildMetadataPayload(tag.MetadataItems);
        var parsed = new AsfMetadataTag();
        parsed.ParseMetadata(bytes, library: false);

        Assert.Equal(2, parsed.MetadataItems.Count);
        Assert.Equal((ushort)7, parsed.MetadataItems[0].StreamNumber);
        Assert.Equal("WM/PartOfSet", parsed.MetadataItems[0].Name);
        Assert.Equal(3u, parsed.MetadataItems[0].Value.AsDword);
        Assert.Equal((ushort)0, parsed.MetadataItems[1].StreamNumber);
        Assert.Equal("Calm", parsed.MetadataItems[1].Value.AsString);
    }

    [Fact]
    public void MetadataLibraryObject_NonZeroLanguageIndexAndStream_RoundTrip()
    {
        var tag = new AsfMetadataTag();
        tag.AddMetadataLibrary(new AsfMetadataItem(2, 5, "WM/Lyrics", AsfTypedValue.FromString("la la")));

        var bytes = AsfMetadataTag.BuildMetadataPayload(tag.MetadataLibraryItems);
        var parsed = new AsfMetadataTag();
        parsed.ParseMetadata(bytes, library: true);

        Assert.Single(parsed.MetadataLibraryItems);
        Assert.Equal((ushort)2, parsed.MetadataLibraryItems[0].LanguageListIndex);
        Assert.Equal((ushort)5, parsed.MetadataLibraryItems[0].StreamNumber);
        Assert.Equal("WM/Lyrics", parsed.MetadataLibraryItems[0].Name);
        Assert.Equal("la la", parsed.MetadataLibraryItems[0].Value.AsString);
    }

    // ================================================================
    // 5. ToByteArrays — full canonical sequence
    // ================================================================

    [Fact]
    public void ToByteArrays_EmittsCdoEcdoMoMlo_InCanonicalOrder_WithGuidsAndSizes()
    {
        var tag = new AsfMetadataTag
        {
            Title = "T",
        };
        tag.AddExtended("k", AsfTypedValue.FromString("v"));
        tag.AddMetadata(new AsfMetadataItem(0, 0, "m", AsfTypedValue.FromDword(1)));
        tag.AddMetadataLibrary(new AsfMetadataItem(0, 0, "l", AsfTypedValue.FromDword(2)));

        var arrays = tag.ToByteArrays();
        Assert.Equal(4, arrays.Length);

        AssertWrappedObject(arrays[0], AsfMetadataTag.ContentDescriptionObjectGuid);
        AssertWrappedObject(arrays[1], AsfMetadataTag.ExtendedContentDescriptionObjectGuid);
        AssertWrappedObject(arrays[2], AsfMetadataTag.MetadataObjectGuid);
        AssertWrappedObject(arrays[3], AsfMetadataTag.MetadataLibraryObjectGuid);
    }

    [Fact]
    public void ToByteArrays_OmitsEmptyObjects()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("k", AsfTypedValue.FromString("v"));

        var arrays = tag.ToByteArrays();
        Assert.Single(arrays);
        AssertWrappedObject(arrays[0], AsfMetadataTag.ExtendedContentDescriptionObjectGuid);
    }

    [Fact]
    public void ToByteArrays_ByteIdentical_RoundTripForAllFourObjects()
    {
        var tag = new AsfMetadataTag
        {
            Title = "Round Trip",
            Author = "Tester",
            Copyright = null,
            Description = "Desc",
            Rating = "G",
        };
        tag.AddExtended("WM/AlbumTitle", AsfTypedValue.FromString("Album"));
        tag.AddExtended("WM/Track", AsfTypedValue.FromDword(7));
        tag.AddMetadata(new AsfMetadataItem(0, 1, "Stream1Note", AsfTypedValue.FromString("note")));
        tag.AddMetadataLibrary(new AsfMetadataItem(0, 0, "GlobalNote", AsfTypedValue.FromString("global")));

        var first = tag.ToByteArrays();

        // Reparse each object payload and re-serialise; bytes must be identical.
        var rebuilt = new AsfMetadataTag
        {
            Title = "Round Trip",
            Author = "Tester",
            Description = "Desc",
            Rating = "G",
        };
        rebuilt.ParseExtendedContentDescription(StripObjectHeader(first[1]));
        rebuilt.ParseMetadata(StripObjectHeader(first[2]), library: false);
        rebuilt.ParseMetadata(StripObjectHeader(first[3]), library: true);

        var second = rebuilt.ToByteArrays();
        Assert.Equal(first.Length, second.Length);
        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    // ================================================================
    // 6. Dictionary view stability
    // ================================================================

    [Fact]
    public void Extended_DictionaryView_OrdinalCaseSensitive()
    {
        var tag = new AsfMetadataTag();
        tag.AddExtended("WM/Genre", AsfTypedValue.FromString("Rock"));
        tag.AddExtended("wm/genre", AsfTypedValue.FromString("Pop"));

        var dict = tag.Extended;
        Assert.Equal(2, dict.Count);
        Assert.Equal("Rock", dict["WM/Genre"].AsString);
        Assert.Equal("Pop", dict["wm/genre"].AsString);
    }

    // ================================================================
    // 7. AsfTypedValue argument handling
    // ================================================================

    [Fact]
    public void TypedValue_FromString_NullBecomesEmpty()
    {
        var v = AsfTypedValue.FromString(null);
        Assert.Equal(AsfDataType.UnicodeString, v.Type);
        Assert.Equal(string.Empty, v.AsString);
    }

    [Fact]
    public void TypedValue_FromBytes_NullBecomesEmpty()
    {
        var v = AsfTypedValue.FromBytes(null);
        Assert.Equal(AsfDataType.Bytes, v.Type);
        Assert.Empty(v.AsBytes!);
    }

    [Fact]
    public void TypedValue_AccessorsReturnDefault_ForOtherTypes()
    {
        var v = AsfTypedValue.FromDword(5);
        Assert.Null(v.AsString);
        Assert.Null(v.AsBytes);
        Assert.False(v.AsBool);
        Assert.Equal(0UL, v.AsQword);
        Assert.Equal((ushort)0, v.AsWord);
        Assert.Equal(5u, v.AsDword);
    }

    [Fact]
    public void Tag_AddExtended_NullArgs_Throws()
    {
        var tag = new AsfMetadataTag();
        Assert.Throws<ArgumentNullException>(() => tag.AddExtended(null!, AsfTypedValue.FromDword(0)));
        Assert.Throws<ArgumentNullException>(() => tag.AddExtended("k", null!));
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static AsfMetadataTag ParseEcdo(AsfMetadataTag source)
    {
        var bytes = source.BuildExtendedContentDescriptionPayload();
        var parsed = new AsfMetadataTag();
        parsed.ParseExtendedContentDescription(bytes);
        return parsed;
    }

    private static void AssertWrappedObject(byte[] bytes, Guid expectedGuid)
    {
        Assert.True(bytes.Length >= 24);
        var guid = new Guid([.. bytes.Take(16)]);
        Assert.Equal(expectedGuid, guid);
        // Size field is little-endian QWORD that includes the header.
        ulong size = 0;
        for (var i = 7; i >= 0; i--)
        {
            size = (size << 8) | bytes[16 + i];
        }

        Assert.Equal((ulong)bytes.Length, size);
    }

    private static byte[] StripObjectHeader(byte[] wrapped)
    {
        var payload = new byte[wrapped.Length - 24];
        Buffer.BlockCopy(wrapped, 24, payload, 0, payload.Length);
        return payload;
    }
}
