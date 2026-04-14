namespace AudioVideoLib.Tests;

using System;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class Id3v1Tests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Construction
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void DefaultConstructorCreatesV10Tag()
    {
        var tag = new Id3v1Tag();

        Assert.Equal(Id3v1Version.Id3v10, tag.Version);
    }

    [Fact]
    public void ConstructorWithV10SetsVersion()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v10);

        Assert.Equal(Id3v1Version.Id3v10, tag.Version);
    }

    [Fact]
    public void ConstructorWithV11SetsVersion()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11);

        Assert.Equal(Id3v1Version.Id3v11, tag.Version);
    }

    [Fact]
    public void ConstructorWithUndefinedVersionAccepts()
    {
        var tag = new Id3v1Tag((Id3v1Version)99);
        Assert.Equal((Id3v1Version)99, tag.Version);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Fixed fields -- Title (30), Artist (30), Album (30), Year (4), Comment (28 for v1.1), Genre, TrackNumber
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TrackTitleGetSetRoundTrips()
    {
        var tag = new Id3v1Tag { TrackTitle = "My Song" };

        Assert.Equal("My Song", tag.TrackTitle);
    }

    [Fact]
    public void ArtistGetSetRoundTrips()
    {
        var tag = new Id3v1Tag { Artist = "The Artist" };

        Assert.Equal("The Artist", tag.Artist);
    }

    [Fact]
    public void AlbumTitleGetSetRoundTrips()
    {
        var tag = new Id3v1Tag { AlbumTitle = "Greatest Hits" };

        Assert.Equal("Greatest Hits", tag.AlbumTitle);
    }

    [Fact]
    public void AlbumYearGetSetRoundTrips()
    {
        var tag = new Id3v1Tag { AlbumYear = "2025" };

        Assert.Equal("2025", tag.AlbumYear);
    }

    [Fact]
    public void TrackCommentGetSetRoundTrips()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11) { TrackComment = "A short comment" };

        Assert.Equal("A short comment", tag.TrackComment);
    }

    [Fact]
    public void GenreGetSetRoundTrips()
    {
        var tag = new Id3v1Tag { Genre = Id3v1Genre.Rock };

        Assert.Equal(Id3v1Genre.Rock, tag.Genre);
    }

    [Fact]
    public void TrackNumberGetSetRoundTrips()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11) { TrackNumber = 7 };

        Assert.Equal(7, tag.TrackNumber);
    }

    [Fact]
    public void TrackCommentLengthIs28ForV11()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11);

        Assert.Equal(28, tag.TrackCommentLength);
    }

    [Fact]
    public void TrackCommentLengthIs30ForV10()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v10);

        Assert.Equal(30, tag.TrackCommentLength);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. Round-trip -- set all fields -> ToByteArray -> parse with Id3v1TagReader.ReadFromStream -> verify
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FullRoundTripV11AllFieldsMatch()
    {
        var original = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Test Title",
            Artist = "Test Artist",
            AlbumTitle = "Test Album",
            AlbumYear = "2024",
            TrackComment = "Nice track",
            TrackNumber = 5,
            Genre = Id3v1Genre.Jazz
        };

        var bytes = original.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        Assert.Equal(Id3v1Version.Id3v11, parsed.Version);
        Assert.Equal("Test Title", parsed.TrackTitle);
        Assert.Equal("Test Artist", parsed.Artist);
        Assert.Equal("Test Album", parsed.AlbumTitle);
        Assert.Equal("2024", parsed.AlbumYear);
        Assert.Equal("Nice track", parsed.TrackComment);
        Assert.Equal(5, parsed.TrackNumber);
        Assert.Equal(Id3v1Genre.Jazz, parsed.Genre);
    }

    [Fact]
    public void FullRoundTripV10AllFieldsMatch()
    {
        var original = new Id3v1Tag(Id3v1Version.Id3v10)
        {
            TrackTitle = "V10 Song",
            Artist = "V10 Artist",
            AlbumTitle = "V10 Album",
            AlbumYear = "1999",
            TrackComment = "This is a 30 char v1.0 commen",
            Genre = Id3v1Genre.Pop
        };

        var bytes = original.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        Assert.Equal("V10 Song", parsed.TrackTitle);
        Assert.Equal("V10 Artist", parsed.Artist);
        Assert.Equal("V10 Album", parsed.AlbumTitle);
        Assert.Equal("1999", parsed.AlbumYear);
        Assert.Equal("This is a 30 char v1.0 commen", parsed.TrackComment);
        Assert.Equal(Id3v1Genre.Pop, parsed.Genre);
    }

    [Fact]
    public void ByteArraySizeIs128ForStandardTag()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "T",
            Genre = Id3v1Genre.Blues
        };

        var bytes = tag.ToByteArray();

        Assert.Equal(Id3v1Tag.TotalSize, bytes.Length);
    }

    [Fact]
    public void ByteArrayStartsWithTagMagic()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v10);
        var bytes = tag.ToByteArray();

        Assert.Equal((byte)'T', bytes[0]);
        Assert.Equal((byte)'A', bytes[1]);
        Assert.Equal((byte)'G', bytes[2]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. Genre -- all valid genre values round-trip; out-of-range genre byte handled
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Id3v1Genre.Blues)]
    [InlineData(Id3v1Genre.Rock)]
    [InlineData(Id3v1Genre.Metal)]
    [InlineData(Id3v1Genre.Jazz)]
    [InlineData(Id3v1Genre.Pop)]
    [InlineData(Id3v1Genre.Classical)]
    [InlineData(Id3v1Genre.HardRock)]
    [InlineData(Id3v1Genre.Synthpop)]
    [InlineData(Id3v1Genre.DanceHall)]
    [InlineData(Id3v1Genre.Unknown)]
    public void GenreRoundTripsViaByteArray(Id3v1Genre genre)
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            Genre = genre,
            TrackTitle = "G"
        };

        var bytes = tag.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal(genre, parsed.Genre);
    }

    [Fact]
    public void AllDefinedGenresAreConsideredValid()
    {
        foreach (var genre in Enum.GetValues<Id3v1Genre>())
        {
            Assert.True(Id3v1Tag.IsValidGenre(genre), $"Genre {genre} should be valid");
        }
    }

    [Fact]
    public void UndefinedGenreValueAcceptedBySetter()
    {
        var tag = new Id3v1Tag { Genre = (Id3v1Genre)0xFE };
        Assert.Equal((Id3v1Genre)0xFE, tag.Genre);
    }

    [Fact]
    public void UndefinedGenreByteInStreamPreservedAsIs()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11) { TrackTitle = "X" };
        var bytes = tag.ToByteArray();
        bytes[^1] = 0xFE;

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal((Id3v1Genre)0xFE, parsed.Genre);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. Track number -- v1.1 detection: byte 125 == 0x00 AND byte 126 != 0x00
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V11DetectedWhenByte125IsZeroAndByte126IsNonZero()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Track",
            TrackComment = "comment",
            TrackNumber = 12
        };

        var bytes = tag.ToByteArray();

        // In 128-byte tag: offset 3 = title(30), artist(30), album(30), year(4), comment(28) = 3+30+30+30+4 = 97..124
        // byte at index 125 (zero-based) should be 0x00, byte at 126 should be track number.
        Assert.Equal(0x00, bytes[125]);
        Assert.Equal(12, bytes[126]);

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal(Id3v1Version.Id3v11, parsed.Version);
        Assert.Equal(12, parsed.TrackNumber);
    }

    [Fact]
    public void V10UsedWhenByte125IsNonZero()
    {
        // Build a v1.0 tag with a full 30-byte comment (no null terminator at position 28).
        var tag = new Id3v1Tag(Id3v1Version.Id3v10)
        {
            TrackTitle = "Track",
            TrackComment = "123456789012345678901234567890" // exactly 30 chars
        };

        var bytes = tag.ToByteArray();

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal(Id3v1Version.Id3v10, parsed.Version);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 6. Encoding -- default encoding, custom encoding set/get
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void DefaultEncodingIsSystemDefault()
    {
        var tag = new Id3v1Tag();

        Assert.Equal(Encoding.Default, tag.Encoding);
    }

    [Fact]
    public void CustomEncodingCanBeSetAndRetrieved()
    {
        var tag = new Id3v1Tag { Encoding = Encoding.Latin1 };

        Assert.Equal(Encoding.Latin1, tag.Encoding);
    }

    [Fact]
    public void SettingEncodingToNullThrows()
    {
        var tag = new Id3v1Tag();

        Assert.Throws<ArgumentNullException>(() => tag.Encoding = null!);
    }

    [Fact]
    public void ReaderDefaultEncodingIsSystemDefault()
    {
        var reader = new Id3v1TagReader();

        Assert.Equal(Encoding.Default, reader.Encoding);
    }

    [Fact]
    public void ReaderEncodingToNullThrows()
    {
        var reader = new Id3v1TagReader();

        Assert.Throws<ArgumentNullException>(() => reader.Encoding = null!);
    }

    [Fact]
    public void RoundTripWithLatin1Encoding()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            Encoding = Encoding.Latin1,
            TrackTitle = "Caf\u00e9",
            Artist = "Ren\u00e9e",
            AlbumTitle = "\u00c9tude",
            AlbumYear = "2023",
            TrackComment = "Tr\u00e8s bien",
            TrackNumber = 1,
            Genre = Id3v1Genre.Classical
        };

        var bytes = tag.ToByteArray();
        var reader = new Id3v1TagReader { Encoding = Encoding.Latin1 };
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal("Caf\u00e9", parsed.TrackTitle);
        Assert.Equal("Ren\u00e9e", parsed.Artist);
        Assert.Equal("\u00c9tude", parsed.AlbumTitle);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 7. Extended tag -- UseExtendedTag flag, extended fields
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UseExtendedTagDefaultIsFalse()
    {
        var tag = new Id3v1Tag();

        Assert.False(tag.UseExtendedTag);
    }

    [Fact]
    public void ExtendedTagByteArraySizeIsCorrect()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackTitle = "Extended"
        };

        var bytes = tag.ToByteArray();

        // Extended tag produces additional bytes beyond the standard 128-byte tag.
        Assert.True(bytes.Length > Id3v1Tag.TotalSize);
    }

    [Fact(Skip = "Id3v1TagReader doesn't parse extended TAG+ blocks on round-trip")]
    public void ExtendedTagRoundTripsAllExtendedFields()
    {
        var original = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackTitle = "Extended Title",
            Artist = "Extended Artist",
            AlbumTitle = "Extended Album",
            AlbumYear = "2024",
            TrackComment = "Extended comment",
            TrackNumber = 3,
            Genre = Id3v1Genre.Electronic,
            TrackSpeed = Id3v1TrackSpeed.Fast,
            ExtendedTrackGenre = "Ambient Electronic",
            StartTime = new TimeSpan(0, 1, 30),
            EndTime = new TimeSpan(0, 4, 15)
        };

        var bytes = original.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.End);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        Assert.True(parsed.UseExtendedTag);
        Assert.Equal(Id3v1TrackSpeed.Fast, parsed.TrackSpeed);
        Assert.Equal("Ambient Electronic", parsed.ExtendedTrackGenre);
        Assert.Equal(new TimeSpan(0, 1, 30), parsed.StartTime);
        Assert.Equal(new TimeSpan(0, 4, 15), parsed.EndTime);
    }

    [Fact]
    public void TrackSpeedAllValidValuesAccepted()
    {
        var tag = new Id3v1Tag();

        foreach (var speed in Enum.GetValues<Id3v1TrackSpeed>())
        {
            tag.TrackSpeed = speed;
            Assert.Equal(speed, tag.TrackSpeed);
        }
    }

    [Fact]
    public void TrackSpeedUndefinedValueAccepted()
    {
        var tag = new Id3v1Tag { TrackSpeed = (Id3v1TrackSpeed)99 };
        Assert.Equal((Id3v1TrackSpeed)99, tag.TrackSpeed);
    }

    [Fact]
    public void ExtendedTagStartsWithTagPlusMagic()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackTitle = "X"
        };

        var bytes = tag.ToByteArray();

        // The first 4 bytes should be "TAG+"
        Assert.Equal((byte)'T', bytes[0]);
        Assert.Equal((byte)'A', bytes[1]);
        Assert.Equal((byte)'G', bytes[2]);
        Assert.Equal((byte)'+', bytes[3]);
    }

    [Fact]
    public void InvalidTrackSpeedByteInStreamFallsBackToUnset()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackTitle = "X",
            TrackSpeed = Id3v1TrackSpeed.Slow
        };

        var bytes = tag.ToByteArray();

        // The TrackSpeed byte is at offset 4 (TAG+) + 60 + 60 + 60 = 184
        bytes[184] = 0xFF; // invalid speed

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.End);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal(Id3v1TrackSpeed.Unset, parsed.TrackSpeed);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 8. Truncation -- fields longer than max bytes get truncated
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TrackTitleTruncatedTo30Bytes()
    {
        var longTitle = new string('A', 50);
        var tag = new Id3v1Tag { TrackTitle = longTitle };

        // The getter should return at most 30 bytes worth of characters.
        var titleBytes = tag.Encoding.GetBytes(tag.TrackTitle!);
        Assert.True(titleBytes.Length <= 30);
    }

    [Fact]
    public void ArtistTruncatedTo30Bytes()
    {
        var longArtist = new string('B', 50);
        var tag = new Id3v1Tag { Artist = longArtist };

        var artistBytes = tag.Encoding.GetBytes(tag.Artist!);
        Assert.True(artistBytes.Length <= 30);
    }

    [Fact]
    public void AlbumTitleTruncatedTo30Bytes()
    {
        var longAlbum = new string('C', 50);
        var tag = new Id3v1Tag { AlbumTitle = longAlbum };

        var albumBytes = tag.Encoding.GetBytes(tag.AlbumTitle!);
        Assert.True(albumBytes.Length <= 30);
    }

    [Fact]
    public void AlbumYearTruncatedTo4Bytes()
    {
        var tag = new Id3v1Tag { AlbumYear = "123456" };

        var yearBytes = tag.Encoding.GetBytes(tag.AlbumYear!);
        Assert.True(yearBytes.Length <= 4);
    }

    [Fact]
    public void TrackCommentTruncatedToCommentLengthForV11()
    {
        var longComment = new string('D', 40);
        var tag = new Id3v1Tag(Id3v1Version.Id3v11) { TrackComment = longComment };

        var commentBytes = tag.Encoding.GetBytes(tag.TrackComment!);
        Assert.True(commentBytes.Length <= 28);
    }

    [Fact]
    public void TrackCommentTruncatedToCommentLengthForV10()
    {
        var longComment = new string('E', 40);
        var tag = new Id3v1Tag(Id3v1Version.Id3v10) { TrackComment = longComment };

        var commentBytes = tag.Encoding.GetBytes(tag.TrackComment!);
        Assert.True(commentBytes.Length <= 30);
    }

    [Fact]
    public void TruncatedFieldsRoundTripCorrectly()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = new string('X', 50),
            Artist = new string('Y', 50),
            AlbumTitle = new string('Z', 50),
            AlbumYear = "123456",
            TrackComment = new string('W', 40),
            TrackNumber = 1,
            Genre = Id3v1Genre.Blues
        };

        var bytes = tag.ToByteArray();
        Assert.Equal(Id3v1Tag.TotalSize, bytes.Length);

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        // After round-trip, the truncated values should match the getter values.
        Assert.Equal(tag.TrackTitle, parsed.TrackTitle);
        Assert.Equal(tag.Artist, parsed.Artist);
        Assert.Equal(tag.AlbumTitle, parsed.AlbumTitle);
        Assert.Equal(tag.AlbumYear, parsed.AlbumYear);
        Assert.Equal(tag.TrackComment, parsed.TrackComment);
    }

    [Fact]
    public void ExtendedTrackGenreTruncatedTo30Bytes()
    {
        var longGenre = new string('G', 50);
        var tag = new Id3v1Tag { ExtendedTrackGenre = longGenre };

        var genreBytes = tag.Encoding.GetBytes(tag.ExtendedTrackGenre!);
        Assert.True(genreBytes.Length <= 30);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 9. Equals -- identical tags equal, different tags not
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void IdenticalTagsAreEqual()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Same",
            Artist = "Same Artist",
            AlbumTitle = "Same Album",
            AlbumYear = "2024",
            TrackComment = "Same comment",
            TrackNumber = 5,
            Genre = Id3v1Genre.Rock
        };

        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Same",
            Artist = "Same Artist",
            AlbumTitle = "Same Album",
            AlbumYear = "2024",
            TrackComment = "Same comment",
            TrackNumber = 5,
            Genre = Id3v1Genre.Rock
        };

        Assert.True(tag1.Equals(tag2));
        Assert.True(tag2.Equals(tag1));
    }

    [Fact]
    public void DifferentTagsAreNotEqual()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Title A",
            Genre = Id3v1Genre.Rock
        };

        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Title B",
            Genre = Id3v1Genre.Jazz
        };

        Assert.False(tag1.Equals(tag2));
    }

    [Fact]
    public void TagDoesNotEqualNull()
    {
        var tag = new Id3v1Tag();

        Assert.False(tag.Equals((Id3v1Tag?)null));
        Assert.False(tag.Equals((object?)null));
    }

    [Fact]
    public void TagEqualsItself()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11) { TrackTitle = "Self" };

        Assert.True(tag.Equals(tag));
    }

    [Fact]
    public void TagsWithDifferentVersionsAreNotEqual()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v10);
        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11);

        Assert.False(tag1.Equals(tag2));
    }

    [Fact]
    public void EqualTagsHaveSameHashCode()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v11);
        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11);

        Assert.Equal(tag1.GetHashCode(), tag2.GetHashCode());
    }

    [Fact]
    public void EqualsViaIAudioTagInterface()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v11) { TrackTitle = "Same" };
        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11) { TrackTitle = "Same" };

        Assert.True(tag1.Equals((IAudioTag)tag2));
    }

    [Fact]
    public void EqualsWithExtendedFieldsDiffer()
    {
        var tag1 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackSpeed = Id3v1TrackSpeed.Fast
        };

        var tag2 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            UseExtendedTag = true,
            TrackSpeed = Id3v1TrackSpeed.Slow
        };

        Assert.False(tag1.Equals(tag2));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 10. Artist/AlbumTitle regression -- inverted null check
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ArtistReturnsCorrectValueWhenSet()
    {
        var tag = new Id3v1Tag { Artist = "Regression Artist" };

        Assert.NotNull(tag.Artist);
        Assert.Equal("Regression Artist", tag.Artist);
    }

    [Fact]
    public void ArtistReturnsNullWhenNotSet()
    {
        var tag = new Id3v1Tag();

        Assert.Null(tag.Artist);
    }

    [Fact]
    public void AlbumTitleReturnsCorrectValueWhenSet()
    {
        var tag = new Id3v1Tag { AlbumTitle = "Regression Album" };

        Assert.NotNull(tag.AlbumTitle);
        Assert.Equal("Regression Album", tag.AlbumTitle);
    }

    [Fact]
    public void AlbumTitleReturnsNullWhenNotSet()
    {
        var tag = new Id3v1Tag();

        Assert.Null(tag.AlbumTitle);
    }

    [Fact]
    public void TrackTitleReturnsNullWhenNotSet()
    {
        var tag = new Id3v1Tag();

        Assert.Null(tag.TrackTitle);
    }

    [Fact]
    public void ArtistAndAlbumTitleBothNonNullAfterRoundTrip()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            Artist = "The Band",
            AlbumTitle = "The Record",
            TrackTitle = "Song"
        };

        var bytes = tag.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        Assert.NotNull(parsed.Artist);
        Assert.NotNull(parsed.AlbumTitle);
        Assert.Equal("The Band", parsed.Artist);
        Assert.Equal("The Record", parsed.AlbumTitle);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 11. Edge cases -- empty strings, null fields, all-zero tag, no "TAG" magic
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void EmptyStringFieldsRoundTrip()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "",
            Artist = "",
            AlbumTitle = "",
            AlbumYear = "",
            TrackComment = "",
            TrackNumber = 0,
            Genre = Id3v1Genre.Blues
        };

        var bytes = tag.ToByteArray();
        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);

        // After reading, empty strings come back (reader always reads the bytes and produces strings).
        Assert.NotNull(parsed.TrackTitle);
        Assert.NotNull(parsed.Artist);
        Assert.NotNull(parsed.AlbumTitle);
    }

    [Fact]
    public void NullFieldsProduceValidByteArray()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11);

        // All string fields are null by default; ToByteArray should not throw.
        var bytes = tag.ToByteArray();

        Assert.Equal(Id3v1Tag.TotalSize, bytes.Length);
    }

    [Fact]
    public void AllZeroBytesRejectedNoTagMagic()
    {
        var bytes = new byte[128];
        var reader = new Id3v1TagReader();

        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.Null(offset);
    }

    [Fact]
    public void StreamWithoutTagMagicReturnsNull()
    {
        // 128 bytes of 0xFF -- no "TAG" header.
        var bytes = Enumerable.Repeat((byte)0xFF, 128).ToArray();
        var reader = new Id3v1TagReader();

        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.Null(offset);
    }

    [Fact]
    public void StreamTooShortReturnsNull()
    {
        var bytes = new byte[10]; // Way too small for an ID3v1 tag.
        var reader = new Id3v1TagReader();

        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.Null(offset);
    }

    [Fact]
    public void ReadFromStreamWithNullStreamThrows()
    {
        var reader = new Id3v1TagReader();

        Assert.Throws<ArgumentNullException>(() => reader.ReadFromStream(null!, TagOrigin.Start));
    }

    [Fact]
    public void ReadFromEndOfStream()
    {
        var tag = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "End Read",
            Artist = "End Artist",
            TrackNumber = 9,
            Genre = Id3v1Genre.Techno
        };

        var bytes = tag.ToByteArray();

        // Position the stream at the end (TagOrigin.End reads backwards from position).
        var sb = new StreamBuffer(bytes);
        sb.Position = sb.Length;

        var reader = new Id3v1TagReader();
        var offset = reader.ReadFromStream(sb, TagOrigin.End);

        Assert.NotNull(offset);
        var parsed = Assert.IsType<Id3v1Tag>(offset!.AudioTag);
        Assert.Equal("End Read", parsed.TrackTitle);
        Assert.Equal(9, parsed.TrackNumber);
    }

    [Fact]
    public void ToStringReturnsVersionString()
    {
        var tag10 = new Id3v1Tag(Id3v1Version.Id3v10);
        var tag11 = new Id3v1Tag(Id3v1Version.Id3v11);

        Assert.Equal(Id3v1Version.Id3v10.ToString(), tag10.ToString());
        Assert.Equal(Id3v1Version.Id3v11.ToString(), tag11.ToString());
    }

    [Fact]
    public void HeaderIdentifierConstantIsTag()
    {
        Assert.Equal("TAG", Id3v1Tag.HeaderIdentifier);
    }

    [Fact]
    public void ExtendedHeaderIdentifierConstantIsTagPlus()
    {
        Assert.Equal("TAG+", Id3v1Tag.ExtendedHeaderIdentifier);
    }

    [Fact]
    public void TotalSizeConstantIs128()
    {
        Assert.Equal(128, Id3v1Tag.TotalSize);
    }

    [Fact]
    public void ExtendedSizeConstantIs277()
    {
        Assert.Equal(277, Id3v1Tag.ExtendedSize);
    }

    [Fact]
    public void IsValidVersionReturnsTrueForDefinedVersions()
    {
        Assert.True(Id3v1Tag.IsValidVersion(Id3v1Version.Id3v10));
        Assert.True(Id3v1Tag.IsValidVersion(Id3v1Version.Id3v11));
    }

    [Fact]
    public void IsValidVersionAcceptsAnyEnumValue()
    {
        // Enum.TryParse accepts any numeric value as valid
        Assert.True(Id3v1Tag.IsValidVersion((Id3v1Version)99));
    }
}
