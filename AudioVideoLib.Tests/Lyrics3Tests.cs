namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class Lyrics3Tests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Lyrics3 v1 — construction, Lyrics property, round-trip, header + footer
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V1_NewTag_LyricsDefaultsToEmpty()
    {
        var tag = new Lyrics3Tag();

        Assert.Null(tag.Lyrics);
    }

    [Fact]
    public void V1_SetLyrics_RoundTripsValue()
    {
        var tag = new Lyrics3Tag { Lyrics = "Hello world" };

        Assert.Equal("Hello world", tag.Lyrics);
    }

    [Fact]
    public void V1_ToByteArray_ContainsHeaderAndFooter()
    {
        var tag = new Lyrics3Tag { Lyrics = "Test" };
        var bytes = tag.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        Assert.StartsWith(Lyrics3Tag.HeaderIdentifier, text);
        Assert.EndsWith(Lyrics3Tag.FooterIdentifier, text);
    }

    [Fact]
    public void V1_ToByteArray_LyricsAppearsBeforeFooter()
    {
        var lyrics = "Some lyrics here";
        var tag = new Lyrics3Tag { Lyrics = lyrics };
        var bytes = tag.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        var headerEnd = Lyrics3Tag.HeaderIdentifier.Length;
        var footerStart = text.IndexOf(Lyrics3Tag.FooterIdentifier, StringComparison.Ordinal);
        var embeddedLyrics = text[headerEnd..footerStart];

        Assert.Equal(lyrics, embeddedLyrics);
    }

    [Fact]
    public void V1_HeaderIdentifier_IsLYRICSBEGIN()
    {
        Assert.Equal("LYRICSBEGIN", Lyrics3Tag.HeaderIdentifier);
    }

    [Fact]
    public void V1_FooterIdentifier_IsLYRICSEND()
    {
        Assert.Equal("LYRICSEND", Lyrics3Tag.FooterIdentifier);
    }

    [Fact]
    public void V1_MaxLyricsSize_Is5100()
    {
        Assert.Equal(5100, Lyrics3Tag.MaxLyricsSize);
    }

    [Fact]
    public void V1_RoundTrip_ToByteArrayThenReadFromStream()
    {
        var original = new Lyrics3Tag { Lyrics = "Round trip test" };
        var bytes = original.ToByteArray();

        var reader = new Lyrics3TagReader();
        var sb = new StreamBuffer(bytes);
        sb.Position = sb.Length;
        var result = reader.ReadFromStream(sb, TagOrigin.End);

        Assert.NotNull(result);
        var parsed = result!.AudioTag as Lyrics3Tag;
        Assert.NotNull(parsed);
        Assert.Equal(original.Lyrics, parsed!.Lyrics);
    }

    [Fact]
    public void V1_SetLyrics_ContainingHeaderIdentifier_Throws()
    {
        var tag = new Lyrics3Tag();

        Assert.Throws<InvalidDataException>(() => tag.Lyrics = "before LYRICSBEGIN after");
    }

    [Fact]
    public void V1_SetLyrics_ContainingFooterIdentifier_Throws()
    {
        var tag = new Lyrics3Tag();

        Assert.Throws<InvalidDataException>(() => tag.Lyrics = "before LYRICSEND after");
    }

    [Fact]
    public void V1_SetLyrics_WithByte255_Throws()
    {
        // Build a string that contains character 0xFF (\u00FF).
        var tag = new Lyrics3Tag { Encoding = Encoding.Latin1 };

        Assert.Throws<InvalidDataException>(() => tag.Lyrics = "abc\u00FFdef");
    }

    [Fact]
    public void V1_LyricsTruncatedToMaxSize()
    {
        var longLyrics = new string('A', Lyrics3Tag.MaxLyricsSize + 500);
        var tag = new Lyrics3Tag { Lyrics = longLyrics };
        var bytes = tag.ToByteArray();

        // The lyrics portion should not exceed MaxLyricsSize bytes.
        var lyricsBytes = bytes.Length - Lyrics3Tag.HeaderIdentifier.Length - Lyrics3Tag.FooterIdentifier.Length;
        Assert.True(lyricsBytes <= Lyrics3Tag.MaxLyricsSize);
    }

    [Fact]
    public void V1_Equals_SameLyrics_ReturnsTrue()
    {
        var tag1 = new Lyrics3Tag { Lyrics = "same" };
        var tag2 = new Lyrics3Tag { Lyrics = "same" };

        Assert.True(tag1.Equals(tag2));
    }

    [Fact]
    public void V1_Equals_DifferentLyrics_ReturnsFalse()
    {
        var tag1 = new Lyrics3Tag { Lyrics = "one" };
        var tag2 = new Lyrics3Tag { Lyrics = "two" };

        Assert.False(tag1.Equals(tag2));
    }

    [Fact]
    public void V1_Equals_Null_ReturnsFalse()
    {
        var tag = new Lyrics3Tag { Lyrics = "test" };

        Assert.False(tag.Equals((Lyrics3Tag?)null));
    }

    [Fact]
    public void V1_Equals_SameReference_ReturnsTrue()
    {
        var tag = new Lyrics3Tag { Lyrics = "test" };

        Assert.True(tag.Equals(tag));
    }

    [Fact]
    public void V1_ToString_ReturnsLyrics3()
    {
        var tag = new Lyrics3Tag();

        Assert.Equal("Lyrics3", tag.ToString());
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Lyrics3 v2 — construction, field count, round-trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_NewTag_HasNoFields()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_SetField_IncreasesFieldCount()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        tag.SetField(field);

        Assert.Single(tag.Fields);
    }

    [Fact]
    public void V2_RoundTrip_ToByteArrayThenReadFromStream()
    {
        var original = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "Artist" };
        original.SetField(field);

        var bytes = original.ToByteArray();
        var reader = new Lyrics3v2TagReader();
        var sb = new StreamBuffer(bytes) { Position = 0 };
        var result = reader.ReadFromStream(sb, TagOrigin.Start);

        Assert.NotNull(result);
        var parsed = result!.AudioTag as Lyrics3v2Tag;
        Assert.NotNull(parsed);
        Assert.Single(parsed!.Fields);
    }

    [Fact]
    public void V2_HeaderIdentifier_IsLYRICSBEGIN()
    {
        Assert.Equal("LYRICSBEGIN", Lyrics3v2Tag.HeaderIdentifier);
    }

    [Fact]
    public void V2_FooterIdentifier_IsLYRICS200()
    {
        Assert.Equal("LYRICS200", Lyrics3v2Tag.FooterIdentifier);
    }

    [Fact]
    public void V2_ToString_ReturnsLyrics3v2()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Equal("Lyrics3v2", tag.ToString());
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. v2 text fields — Lyrics3v2TextField with known identifiers, Value, round-trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Lyrics3v2TextFieldIdentifier.AdditionalInformation, "INF")]
    [InlineData(Lyrics3v2TextFieldIdentifier.LyricsAuthorName, "AUT")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName, "EAL")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedArtistName, "EAR")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle, "ETT")]
    public void V2TextField_ConstructedWithEnum_HasCorrectIdentifier(Lyrics3v2TextFieldIdentifier identifier, string expectedId)
    {
        var field = new Lyrics3v2TextField(identifier);

        Assert.Equal(expectedId, field.Identifier);
    }

    [Fact]
    public void V2TextField_Value_SetAndGet()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "My Song" };

        Assert.Equal("My Song", field.Value);
    }

    [Fact]
    public void V2TextField_Data_ReflectsValueAsAscii()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Album" };
        var data = field.Data;

        Assert.NotNull(data);
        Assert.Equal("Album", Encoding.ASCII.GetString(data!));
    }

    [Fact]
    public void V2TextField_ToByteArray_ContainsIdentifierSizeAndData()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "Artist" };
        var bytes = field.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        // First 3 chars: identifier.
        Assert.Equal("EAR", text[..3]);
        // Next 5 chars: size (zero-padded).
        Assert.Equal("00006", text[3..8]);
        // Remaining: data.
        Assert.Equal("Artist", text[8..]);
    }

    [Fact]
    public void V2TextField_RoundTrip_FieldToBytesThenReadFromStream()
    {
        var original = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.LyricsAuthorName) { Value = "Author" };
        var bytes = original.ToByteArray();

        var sb = new StreamBuffer(bytes);
        var parsed = Lyrics3v2Field.ReadFromStream(sb, bytes.Length);

        Assert.NotNull(parsed);
        var textField = parsed as Lyrics3v2TextField;
        Assert.NotNull(textField);
        Assert.Equal("Author", textField!.Value);
        Assert.Equal("AUT", textField.Identifier);
    }

    [Fact]
    public void V2TextField_ConstructedWithString_HasCorrectIdentifier()
    {
        var field = new Lyrics3v2TextField("INF") { Value = "Info" };

        Assert.Equal("INF", field.Identifier);
        Assert.Equal("Info", field.Value);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. v2 field identifiers — GetIdentifier mapping
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Lyrics3v2TextFieldIdentifier.AdditionalInformation, "INF")]
    [InlineData(Lyrics3v2TextFieldIdentifier.LyricsAuthorName, "AUT")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName, "EAL")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedArtistName, "EAR")]
    [InlineData(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle, "ETT")]
    [InlineData(Lyrics3v2TextFieldIdentifier.Genre, "GRE")]
    public void V2TextField_GetIdentifier_MapsEnumToString(Lyrics3v2TextFieldIdentifier identifier, string expected)
    {
        var result = Lyrics3v2TextField.GetIdentifier(identifier);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void V2TextField_GetIdentifier_UnknownEnum_ReturnsNull()
    {
        var result = Lyrics3v2TextField.GetIdentifier((Lyrics3v2TextFieldIdentifier)999);

        Assert.Null(result);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. v2 SetField/RemoveField — add, replace, remove
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_SetField_AddsNewField()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        tag.SetField(field);

        Assert.Single(tag.Fields);
        Assert.Equal("Title", tag.GetField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle)?.Value);
    }

    [Fact]
    public void V2_SetField_ReplacesExistingFieldWithSameIdentifier()
    {
        var tag = new Lyrics3v2Tag();
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Old" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "New" };
        tag.SetField(field1);
        tag.SetField(field2);

        Assert.Single(tag.Fields);
        Assert.Equal("New", tag.GetField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle)?.Value);
    }

    [Fact]
    public void V2_RemoveField_ByIdentifierString()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Album" };
        tag.SetField(field);
        tag.RemoveField("EAL");

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_RemoveField_ByReference()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "Artist" };
        tag.SetField(field);
        tag.RemoveField(field);

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_RemoveFields_ByIdentifierString()
    {
        var tag = new Lyrics3v2Tag();
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Album1" };
        tag.SetField(field1);
        tag.RemoveFields("EAL");

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_RemoveFields_ByType()
    {
        var tag = new Lyrics3v2Tag();
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Album" };
        tag.SetField(field1);
        tag.SetField(field2);
        tag.RemoveFields<Lyrics3v2TextField>();

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_SetFields_AddsMultipleFields()
    {
        var tag = new Lyrics3v2Tag();
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Album" };
        tag.SetFields([field1, field2]);

        Assert.Equal(2, tag.Fields.Count());
    }

    [Fact]
    public void V2_RemoveField_NonExistentIdentifier_DoesNotThrow()
    {
        var tag = new Lyrics3v2Tag();

        tag.RemoveField("ZZZ");

        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void V2_GetField_ByGenericType_ReturnsNull_WhenEmpty()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Null(tag.GetField<Lyrics3v2TextField>());
    }

    [Fact]
    public void V2_GetField_ByGenericTypeAndIdentifier_ReturnsField()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        tag.SetField(field);

        var result = tag.GetField<Lyrics3v2TextField>("ETT");

        Assert.NotNull(result);
        Assert.Equal("Title", result!.Value);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 6. v2 typed properties — AdditionalInformation, ExtendedAlbumName, etc.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_AdditionalInformation_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.AdditionalInformation) { Value = "Info text" };
        tag.AdditionalInformation = field;

        Assert.NotNull(tag.AdditionalInformation);
        Assert.Equal("Info text", tag.AdditionalInformation!.Value);
    }

    [Fact]
    public void V2_ExtendedAlbumName_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Great Album" };
        tag.ExtendedAlbumName = field;

        Assert.NotNull(tag.ExtendedAlbumName);
        Assert.Equal("Great Album", tag.ExtendedAlbumName!.Value);
    }

    [Fact]
    public void V2_ExtendedArtistName_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "The Artist" };
        tag.ExtendedArtistName = field;

        Assert.NotNull(tag.ExtendedArtistName);
        Assert.Equal("The Artist", tag.ExtendedArtistName!.Value);
    }

    [Fact]
    public void V2_ExtendedTrackTitle_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Track One" };
        tag.ExtendedTrackTitle = field;

        Assert.NotNull(tag.ExtendedTrackTitle);
        Assert.Equal("Track One", tag.ExtendedTrackTitle!.Value);
    }

    [Fact]
    public void V2_LyricsAuthorName_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.LyricsAuthorName) { Value = "Lyricist" };
        tag.LyricsAuthorName = field;

        Assert.NotNull(tag.LyricsAuthorName);
        Assert.Equal("Lyricist", tag.LyricsAuthorName!.Value);
    }

    [Fact]
    public void V2_Lyrics_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var lyricsField = new Lyrics3v2LyricsField();
        lyricsField.LyricLines.Add(new Lyrics3v2LyricLine { LyricLine = "First line" });
        tag.Lyrics = lyricsField;

        Assert.NotNull(tag.Lyrics);
        Assert.Single(tag.Lyrics!.LyricLines);
        Assert.Equal("First line", tag.Lyrics.LyricLines[0].LyricLine);
    }

    [Fact]
    public void V2_Genre_SetAndGet()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.Genre) { Value = "Rock" };
        tag.Genre = field;

        Assert.NotNull(tag.Genre);
        Assert.Equal("Rock", tag.Genre!.Value);
    }

    [Fact]
    public void V2_TypedProperty_ReturnsNull_WhenNotSet()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Null(tag.AdditionalInformation);
        Assert.Null(tag.ExtendedAlbumName);
        Assert.Null(tag.ExtendedArtistName);
        Assert.Null(tag.ExtendedTrackTitle);
        Assert.Null(tag.LyricsAuthorName);
        Assert.Null(tag.Lyrics);
        Assert.Null(tag.Genre);
        Assert.Null(tag.ImageFile);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 7. v2 field validation — IsValidData, IsValidString
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2Field_IsValidData_AllBytesInRange_ReturnsTrue()
    {
        byte[] data = [0x01, 0x41, 0x7F, 0xFE];

        Assert.True(Lyrics3v2Field.IsValidData(data));
    }

    [Fact]
    public void V2Field_IsValidData_ByteFF_ReturnsFalse()
    {
        byte[] data = [0x41, 0xFF, 0x42];

        Assert.False(Lyrics3v2Field.IsValidData(data));
    }

    [Fact]
    public void V2Field_IsValidData_ByteZeroNotAtEnd_ReturnsFalse()
    {
        byte[] data = [0x00, 0x41, 0x42];

        Assert.False(Lyrics3v2Field.IsValidData(data));
    }

    [Fact]
    public void V2Field_IsValidData_ByteZeroAtEnd_ReturnsTrue()
    {
        byte[] data = [0x41, 0x42, 0x00];

        Assert.True(Lyrics3v2Field.IsValidData(data));
    }

    [Fact]
    public void V2Field_IsValidData_EmptyArray_ReturnsTrue()
    {
        byte[] data = [];

        Assert.True(Lyrics3v2Field.IsValidData(data));
    }

    [Fact]
    public void V2Field_IsValidString_AllAsciiInRange_ReturnsTrue()
    {
        Assert.True(Lyrics3v2Field.IsValidString("Hello World"));
    }

    [Fact]
    public void V2Field_IsValidString_CharFF_ReturnsFalse()
    {
        Assert.False(Lyrics3v2Field.IsValidString("abc\u00FFdef"));
    }

    [Fact]
    public void V2Field_IsValidString_CharZeroNotAtEnd_ReturnsFalse()
    {
        Assert.False(Lyrics3v2Field.IsValidString("\0abc"));
    }

    [Fact]
    public void V2Field_IsValidString_CharZeroAtEnd_ReturnsTrue()
    {
        Assert.True(Lyrics3v2Field.IsValidString("abc\0"));
    }

    [Fact]
    public void V2Field_IsValidString_EmptyString_ReturnsTrue()
    {
        Assert.True(Lyrics3v2Field.IsValidString(string.Empty));
    }

    [Fact]
    public void V2TextField_SetValue_InvalidChars_Throws()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle);

        Assert.Throws<InvalidDataException>(() => field.Value = "bad\u00FFvalue");
    }

    [Fact]
    public void V2TextField_SetValue_EmptyString_DoesNotThrow()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = string.Empty };

        Assert.Equal(string.Empty, field.Value);
    }

    [Fact]
    public void V2TextField_SetValue_Null_DoesNotThrow()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = null! };

        Assert.Null(field.Value);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 8. v2 tag size — TagSizeLength, size encoding
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_TagSizeLength_IsSix()
    {
        Assert.Equal(6, Lyrics3v2Tag.TagSizeLength);
    }

    [Fact]
    public void V2_ToByteArray_EndsWithSixDigitSizeAndFooter()
    {
        var tag = new Lyrics3v2Tag();
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        tag.SetField(field);

        var bytes = tag.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        // The footer is "LYRICS200".
        Assert.EndsWith(Lyrics3v2Tag.FooterIdentifier, text);

        // The 6 characters before the footer should be a numeric size.
        var footerStart = text.LastIndexOf(Lyrics3v2Tag.FooterIdentifier, StringComparison.Ordinal);
        var sizeStr = text.Substring(footerStart - Lyrics3v2Tag.TagSizeLength, Lyrics3v2Tag.TagSizeLength);
        Assert.True(long.TryParse(sizeStr, out var parsedSize));

        // The size should equal the total length minus the 6-digit size and the footer.
        var expectedSize = bytes.Length - Lyrics3v2Tag.TagSizeLength - Lyrics3v2Tag.FooterIdentifier.Length;
        Assert.Equal(expectedSize, parsedSize);
    }

    [Fact]
    public void V2_ToByteArray_StartsWithHeader()
    {
        var tag = new Lyrics3v2Tag();
        var bytes = tag.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        Assert.StartsWith(Lyrics3v2Tag.HeaderIdentifier, text);
    }

    [Fact]
    public void V2_FieldIdentifierLength_IsThree()
    {
        Assert.Equal(3, Lyrics3v2Field.FieldIdentifierLength);
    }

    [Fact]
    public void V2_FieldSizeLength_IsFive()
    {
        Assert.Equal(5, Lyrics3v2Field.FieldSizeLength);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 9. Equals — identical v2 tags equal
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_Equals_BothEmpty_ReturnsTrue()
    {
        var tag1 = new Lyrics3v2Tag();
        var tag2 = new Lyrics3v2Tag();

        Assert.True(tag1.Equals(tag2));
    }

    [Fact]
    public void V2_Equals_SameReference_ReturnsTrue()
    {
        var tag = new Lyrics3v2Tag();

        Assert.True(tag.Equals(tag));
    }

    [Fact]
    public void V2_Equals_Null_ReturnsFalse()
    {
        var tag = new Lyrics3v2Tag();

        Assert.False(tag.Equals((Lyrics3v2Tag?)null));
    }

    [Fact]
    public void V2_Equals_IAudioTag_Null_ReturnsFalse()
    {
        var tag = new Lyrics3v2Tag();

        Assert.False(tag.Equals((IAudioTag?)null));
    }

    [Fact]
    public void V2_Equals_ObjectOverload_ReturnsTrueForEqualTag()
    {
        var tag1 = new Lyrics3v2Tag();
        var tag2 = new Lyrics3v2Tag();

        Assert.True(tag1.Equals((object)tag2));
    }

    [Fact]
    public void V2Field_Equals_SameIdentifierAndData_ReturnsTrue()
    {
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };

        Assert.True(field1.Equals(field2));
    }

    [Fact]
    public void V2Field_Equals_DifferentValue_ReturnsFalse()
    {
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title1" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title2" };

        Assert.False(field1.Equals(field2));
    }

    [Fact]
    public void V2Field_Equals_DifferentIdentifier_ReturnsFalse()
    {
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Same" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Same" };

        Assert.False(field1.Equals(field2));
    }

    [Fact]
    public void V2Field_Equals_Null_ReturnsFalse()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };

        Assert.False(field.Equals((Lyrics3v2TextField?)null));
    }

    [Fact]
    public void V2Field_Equals_SameReference_ReturnsTrue()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Title" };

        Assert.True(field.Equals(field));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 10. Edge cases — empty tag, empty fields, max-length lyrics, indications field, lyrics field
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V2_EmptyTag_ToByteArray_ContainsOnlyHeaderSizeFooter()
    {
        var tag = new Lyrics3v2Tag();
        var bytes = tag.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        // Should be: "LYRICSBEGIN" + 6-digit size + "LYRICS200"
        var expectedLength = Lyrics3v2Tag.HeaderIdentifier.Length + Lyrics3v2Tag.TagSizeLength + Lyrics3v2Tag.FooterIdentifier.Length;
        Assert.Equal(expectedLength, bytes.Length);
        Assert.StartsWith(Lyrics3v2Tag.HeaderIdentifier, text);
        Assert.EndsWith(Lyrics3v2Tag.FooterIdentifier, text);
    }

    [Fact]
    public void V2TextField_EmptyValue_ToByteArray_HasZeroSize()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = string.Empty };
        var bytes = field.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        // Identifier (3) + size (5) + data (0).
        Assert.Equal(8, bytes.Length);
        Assert.Equal("ETT00000", text);
    }

    [Fact]
    public void V2TextField_NullData_ToByteArray_HasZeroSize()
    {
        var field = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = null! };
        var bytes = field.ToByteArray();
        var text = Encoding.ASCII.GetString(bytes);

        // When Data returns null, the field serializes with 00000 size.
        Assert.Equal("ETT00000", text);
    }

    [Fact]
    public void V2_IndicationsField_DefaultValues()
    {
        var field = new Lyrics3v2IndicationsField();

        Assert.Equal("IND", field.Identifier);
        Assert.False(field.LyricsFieldPresent);
        Assert.False(field.LyricsContainTimeStamp);
        Assert.False(field.InhibitTrack);
    }

    [Fact]
    public void V2_IndicationsField_SetProperties_ReflectedInData()
    {
        var field = new Lyrics3v2IndicationsField
        {
            LyricsFieldPresent = true,
            LyricsContainTimeStamp = false,
            InhibitTrack = true
        };

        var data = field.Data;
        Assert.NotNull(data);
        var text = Encoding.ASCII.GetString(data!);
        Assert.Equal("101", text);
    }

    [Fact]
    public void V2_IndicationsField_RoundTrip()
    {
        var original = new Lyrics3v2IndicationsField
        {
            LyricsFieldPresent = true,
            LyricsContainTimeStamp = true,
            InhibitTrack = false
        };

        var bytes = original.ToByteArray();
        var sb = new StreamBuffer(bytes);
        var parsed = Lyrics3v2Field.ReadFromStream(sb, bytes.Length);

        Assert.NotNull(parsed);
        var indField = parsed as Lyrics3v2IndicationsField;
        Assert.NotNull(indField);
        Assert.True(indField!.LyricsFieldPresent);
        Assert.True(indField.LyricsContainTimeStamp);
        Assert.False(indField.InhibitTrack);
    }

    [Fact]
    public void V2_LyricsField_EmptyLines()
    {
        var field = new Lyrics3v2LyricsField();

        Assert.Equal("LYR", field.Identifier);
        Assert.Empty(field.LyricLines);
    }

    [Fact]
    public void V2_LyricsField_WithTimestamps_RoundTrip()
    {
        var original = new Lyrics3v2LyricsField();
        var line = new Lyrics3v2LyricLine { LyricLine = "Hello world" };
        line.TimeStamps.Add(new TimeSpan(0, 1, 30));
        original.LyricLines.Add(line);

        var bytes = original.ToByteArray();
        var sb = new StreamBuffer(bytes);
        var parsed = Lyrics3v2Field.ReadFromStream(sb, bytes.Length);

        Assert.NotNull(parsed);
        var lyrField = parsed as Lyrics3v2LyricsField;
        Assert.NotNull(lyrField);
        Assert.NotEmpty(lyrField!.LyricLines);
        Assert.Equal("Hello world", lyrField.LyricLines[0].LyricLine);
        Assert.NotEmpty(lyrField.LyricLines[0].TimeStamps);
        Assert.Equal(new TimeSpan(0, 1, 30), lyrField.LyricLines[0].TimeStamps[0]);
    }

    [Fact]
    public void V2_FullTag_MultipleFields_RoundTrip()
    {
        var original = new Lyrics3v2Tag();
        original.SetField(new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Song" });
        original.SetField(new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "Band" });
        original.SetField(new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Disc" });

        var bytes = original.ToByteArray();
        var reader = new Lyrics3v2TagReader();
        var sb = new StreamBuffer(bytes) { Position = 0 };
        var result = reader.ReadFromStream(sb, TagOrigin.Start);

        Assert.NotNull(result);
        var parsed = result!.AudioTag as Lyrics3v2Tag;
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed!.Fields.Count());
    }

    [Fact]
    public void V2_LargeTextField_RoundTrips()
    {
        // A reasonably large text value just under the 99999-byte field limit.
        var largeValue = new string('X', 5000);
        var original = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.AdditionalInformation) { Value = largeValue };

        var bytes = original.ToByteArray();
        var sb = new StreamBuffer(bytes);
        var parsed = Lyrics3v2Field.ReadFromStream(sb, bytes.Length);

        Assert.NotNull(parsed);
        var textField = parsed as Lyrics3v2TextField;
        Assert.NotNull(textField);
        Assert.Equal(largeValue, textField!.Value);
    }

    [Fact]
    public void V2_NewLine_IsCrLf()
    {
        Assert.Equal("\r\n", Lyrics3v2Tag.NewLine);
    }

    [Fact]
    public void V1_Equals_CaseInsensitive()
    {
        var tag1 = new Lyrics3Tag { Lyrics = "HELLO" };
        var tag2 = new Lyrics3Tag { Lyrics = "hello" };

        Assert.True(tag1.Equals(tag2));
    }

    [Fact]
    public void V2Field_GetHashCode_SameIdentifier_SameHash()
    {
        var field1 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "A" };
        var field2 = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "B" };

        Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
    }

    [Fact]
    public void V2_ImageFileField_HasCorrectIdentifier()
    {
        var field = new Lyrics3v2ImageFileField();

        Assert.Equal("IMG", field.Identifier);
        Assert.Empty(field.ImageFiles);
    }

    [Fact]
    public void V2Field_IsValidData_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Lyrics3v2Field.IsValidData(null!));
    }

    [Fact]
    public void V2Field_IsValidString_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Lyrics3v2Field.IsValidString(null!));
    }

    [Fact]
    public void V2_SetField_NullArgument_Throws()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Throws<ArgumentNullException>(() => tag.SetField(null));
    }

    [Fact]
    public void V2_RemoveFieldByString_NullArgument_Throws()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Throws<ArgumentNullException>(() => tag.RemoveField((string)null!));
    }

    [Fact]
    public void V2_RemoveFieldByRef_NullArgument_Throws()
    {
        var tag = new Lyrics3v2Tag();

        Assert.Throws<ArgumentNullException>(() => tag.RemoveField((Lyrics3v2Field)null!));
    }

    [Fact]
    public void V2_GetFieldByIdentifierString_NullArgument_Throws()
    {
        var tag = new Lyrics3v2Tag();

        Assert.ThrowsAny<ArgumentException>(() => tag.GetField<Lyrics3v2TextField>(null!));
    }
}
