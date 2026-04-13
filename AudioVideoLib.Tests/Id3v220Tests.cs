namespace AudioVideoLib.Tests;

using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class Id3v220Tests
{
    private const byte IsoEncoding = 0x00;

    private const byte Ucs2Encoding = 0x01;

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.1: ID3v2 header identification.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void HeaderBytesStartWithId3MagicAndVersion020()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220) { TrackTitle = MakeTextFrame("TT2", "X") };

        var bytes = tag.ToByteArray();

        // "ID3"
        Assert.Equal((byte)'I', bytes[0]);
        Assert.Equal((byte)'D', bytes[1]);
        Assert.Equal((byte)'3', bytes[2]);

        // Version: 02 00 (major 2, revision 0).
        Assert.Equal(0x02, bytes[3]);
        Assert.Equal(0x00, bytes[4]);

        // No flags set.
        Assert.Equal(0x00, bytes[5]);

        // Size: 4-byte synchsafe int (last 4 bytes of the 10-byte header).
        // Every MSB should be 0 in a well-formed synchsafe integer.
        Assert.Equal(0x00, bytes[6] & 0x80);
        Assert.Equal(0x00, bytes[7] & 0x80);
        Assert.Equal(0x00, bytes[8] & 0x80);
        Assert.Equal(0x00, bytes[9] & 0x80);
    }

    [Fact]
    public void HeaderReadsBackViaTagReaderWithCorrectVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220) { TrackTitle = MakeTextFrame("TT2", "Hello") };

        var bytes = tag.ToByteArray();
        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);

        Assert.NotNull(offset);
        var read = Assert.IsType<Id3v2Tag>(offset!.AudioTag);
        Assert.Equal(Id3v2Version.Id3v220, read.Version);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.1: Unsynchronization flag (bit 7) round-trip.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnsynchronizationFlagOffByDefault()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220) { TrackTitle = MakeTextFrame("TT2", "Song") };

        var bytes = tag.ToByteArray();
        // Flag byte at offset 5; unsynchronization bit is 0x80.
        Assert.Equal(0x00, bytes[5] & 0x80);
    }

    [Fact]
    public void UnsynchronizationFlagSetPersistsRoundTrip()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220)
        {
            UseUnsynchronization = true,
            TrackTitle = MakeTextFrame("TT2", "UnsynchTest")
        };

        var bytes = tag.ToByteArray();
        Assert.Equal(0x80, bytes[5] & 0x80);

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;
        Assert.True(read.UseUnsynchronization);
        Assert.Equal(Id3v2Version.Id3v220, read.Version);
        Assert.NotNull(read.TrackTitle);
        Assert.Equal("UnsynchTest", read.TrackTitle!.Values[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.1: Compression flag (bit 6) - no compression scheme was defined,
    // so the whole tag must be ignored when bit 6 is set.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CompressionFlagCanBeWrittenInHeader()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220)
        {
            UseCompression = true,
            TrackTitle = MakeTextFrame("TT2", "Comp")
        };

        Assert.True(tag.UseCompression);
        var bytes = tag.ToByteArray();
        // Bit 6 = 0x40.
        Assert.Equal(0x40, bytes[5] & 0x40);
    }

    [Fact]
    public void CompressionFlagIsOnlyValidBelowId3v230()
    {
        // Per spec: compression bit is only defined for v2.2 / v2.2.1.
        // For v2.3.0 and later the setter must be a no-op.
        var v230 = new Id3v2Tag(Id3v2Version.Id3v230) { UseCompression = true };
        Assert.False(v230.UseCompression);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.1: Synchsafe integer encoding/decoding.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void SynchsafeValueOf257EncodesTo00000201()
    {
        // Spec: "a 257 bytes long tag is represented as $00 00 02 01".
        var encoded = Id3v2Tag.GetSynchsafeValue(257);
        var bytes = new[]
        {
            (byte)((encoded >> 24) & 0xFF),
            (byte)((encoded >> 16) & 0xFF),
            (byte)((encoded >> 8) & 0xFF),
            (byte)(encoded & 0xFF)
        };
        Assert.Equal([0x00, 0x00, 0x02, 0x01], bytes);
    }

    [Fact]
    public void SynchsafeRoundTripKnownValues()
    {
        int[] values = [0, 1, 127, 128, 257, 16384, 0x0FFFFFFF];
        foreach (var v in values)
        {
            var encoded = Id3v2Tag.GetSynchsafeValue(v);
            var decoded = Id3v2Tag.GetUnsynchedValue(encoded);
            Assert.Equal(v, decoded);
        }
    }

    [Fact]
    public void SynchsafeEncodingMsbsAreAlwaysZero()
    {
        // Top bit of every byte must be cleared in a synchsafe integer.
        int[] values = [1, 127, 128, 256, 257, 0x0FFFFFFF];
        foreach (var v in values)
        {
            var encoded = Id3v2Tag.GetSynchsafeValue(v);
            Assert.Equal(0, encoded & unchecked((int)0x80808080));
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.2: 3-character frame identifiers (TT2, TP1, TAL, TYE, TRK, COM, UFI, PIC, ...).
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("TT2")]
    [InlineData("TP1")]
    [InlineData("TAL")]
    [InlineData("TYE")]
    [InlineData("TRK")]
    [InlineData("TCO")]
    [InlineData("TCM")]
    [InlineData("TEN")]
    public void TextFrameIdentifierIsAcceptedForV220(string identifier)
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, identifier) { TextEncoding = Id3v2FrameEncodingType.Default };
        frame.Values.Add("value");
        Assert.Equal(identifier, frame.Identifier);
    }

    [Fact]
    public void IsValidIdentifierAcceptsThreeCharacterIdForV220()
    {
        Assert.True(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "TT2"));
        Assert.True(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "PIC"));
        Assert.True(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "UFI"));
        // 4-char identifiers are not valid in v2.2.
        Assert.False(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "TIT2"));
    }

    [Fact]
    public void GetIdentifierFieldLengthIsThreeForV220()
    {
        Assert.Equal(3, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v220));
        Assert.Equal(4, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v230));
    }

    [Fact]
    public void TextFrameRoundTripsAcrossMultipleIdentifiers()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        title.Values.Add("The Title");
        var artist = new Id3v2TextFrame(Id3v2Version.Id3v220, "TP1") { TextEncoding = Id3v2FrameEncodingType.Default };
        artist.Values.Add("The Artist");
        var album = new Id3v2TextFrame(Id3v2Version.Id3v220, "TAL") { TextEncoding = Id3v2FrameEncodingType.Default };
        album.Values.Add("The Album");
        var year = new Id3v2TextFrame(Id3v2Version.Id3v220, "TYE") { TextEncoding = Id3v2FrameEncodingType.Default };
        year.Values.Add("2026");
        var track = new Id3v2TextFrame(Id3v2Version.Id3v220, "TRK") { TextEncoding = Id3v2FrameEncodingType.Default };
        track.Values.Add("1/10");

        tag.TrackTitle = title;
        tag.Artist = artist;
        tag.AlbumTitle = album;
        tag.YearRecording = year;
        tag.TrackNumber = track;

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(tag.ToByteArray()), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;

        Assert.Equal("The Title", read.TrackTitle!.Values[0]);
        Assert.Equal("The Artist", read.Artist!.Values[0]);
        Assert.Equal("The Album", read.AlbumTitle!.Values[0]);
        Assert.Equal("2026", read.YearRecording!.Values[0]);
        Assert.Equal("1/10", read.TrackNumber!.Values[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.2: Frame size is 3 bytes big-endian in v2.2 (not synchsafe; that's v2.3+).
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameSizeFieldIsThreeBytesBigEndianForV220()
    {
        // Construct the smallest possible payload: a single text frame with a short ASCII body.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        frame.Values.Add("Hi");
        tag.TrackTitle = frame;

        var bytes = tag.ToByteArray();

        // Tag header is 10 bytes. Frame starts at offset 10 with "TT2" identifier.
        Assert.Equal((byte)'T', bytes[10]);
        Assert.Equal((byte)'T', bytes[11]);
        Assert.Equal((byte)'2', bytes[12]);

        // Next 3 bytes = size (big endian). Then data. No 2-byte flags field in v2.2.
        var size = (bytes[13] << 16) | (bytes[14] << 8) | bytes[15];

        // Expected payload is: 1 encoding byte + "Hi" bytes = 3.
        Assert.Equal(3, size);

        // Immediately after the 6-byte frame header, we must see the encoding byte (0x00) then "Hi".
        Assert.Equal(0x00, bytes[16]);
        Assert.Equal((byte)'H', bytes[17]);
        Assert.Equal((byte)'i', bytes[18]);
    }

    [Fact]
    public void FrameSizeIsNotSynchsafeInV220()
    {
        // Build a v2.2 text frame whose data length has bit 7 set in at least one byte.
        // If the writer was (incorrectly) synchsafe-encoding the size, such a raw length
        // could not appear in the stream. We assert the raw 3 bytes match plain big-endian.
        var payload = new string('A', 200); // "A" is 0x41 in ISO-8859-1.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        frame.Values.Add(payload);
        tag.TrackTitle = frame;

        var bytes = tag.ToByteArray();

        // Size = 1 (encoding byte) + 200 = 201 = 0xC9. Bit 7 is set -> not synchsafe-representable as-is.
        var size = (bytes[13] << 16) | (bytes[14] << 8) | bytes[15];
        Assert.Equal(201, size);
        Assert.Equal(0x00, bytes[13]);
        Assert.Equal(0x00, bytes[14]);
        Assert.Equal(0xC9, bytes[15]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 3.3 / 4.2: Text encoding byte - v2.2 supports 0x00 ISO-8859-1 and 0x01 UCS-2.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TextFrameWritesIsoEncodingByteAsZero()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        frame.Values.Add("ASCII");
        tag.TrackTitle = frame;

        var bytes = tag.ToByteArray();
        // offset 16 is the first data byte (see previous test).
        Assert.Equal(IsoEncoding, bytes[16]);
    }

    [Fact]
    public void TextFrameIsoEncodingRoundTrip()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        frame.Values.Add("Caf\u00e9"); // ISO-8859-1 compatible.
        tag.TrackTitle = frame;

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(tag.ToByteArray()), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;
        Assert.Equal(Id3v2FrameEncodingType.Default, read.TrackTitle!.TextEncoding);
        Assert.Equal("Caf\u00e9", read.TrackTitle!.Values[0]);
    }

    [Fact]
    public void TextFrameWritesUcs2EncodingByteAsOne()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian };
        frame.Values.Add("Hi");
        tag.TrackTitle = frame;

        var bytes = tag.ToByteArray();
        Assert.Equal(Ucs2Encoding, bytes[16]);
    }

    [Fact]
    public void TextFrameUcs2EncodingRoundTrip()
    {
        // UCS-2 is represented in this library as UTF-16 LE with BOM, matching encoding byte 0x01.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian };
        frame.Values.Add("\u65e5\u672c\u8a9e"); // Japanese.
        tag.TrackTitle = frame;

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(tag.ToByteArray()), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;
        Assert.Equal(Id3v2FrameEncodingType.UTF16LittleEndian, read.TrackTitle!.TextEncoding);
        Assert.Equal("\u65e5\u672c\u8a9e", read.TrackTitle!.Values[0]);
    }

    [Fact]
    public void TextFrameWithInvalidEncodingByteIsDowngradedToIsoOnRead()
    {
        // Build a v2.2 tag by hand with an invalid encoding byte (anything >= 2 is not defined).
        // The reader should downgrade to default (ISO-8859-1) rather than NRE or throw.
        var bytes = BuildRawV220TagWithTt2("ABC", encodingByte: 0x05);
        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;
        Assert.NotNull(read.TrackTitle);
        Assert.Equal(Id3v2FrameEncodingType.Default, read.TrackTitle!.TextEncoding);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Section 4.15: PIC frame - v2.2 uses 3-byte image format, not a MIME string.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void AttachedPictureFrameIdentifierIsPicForV220()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220);
        Assert.Equal("PIC", pic.Identifier);
    }

    [Fact]
    public void AttachedPictureFrameIdentifierIsApicForV230()
    {
        var apic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v230);
        Assert.Equal("APIC", apic.Identifier);
    }

    [Fact]
    public void PicFrameRoundTripUsesThreeByteImageFormat()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "JPG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "Cover",
            PictureData = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46]
        };
        tag.AttachedPictures.Add(pic);

        var bytes = tag.ToByteArray();

        // Find the PIC identifier in the raw bytes and verify the layout:
        // identifier(3) + size(3) + encoding(1) + image format(3 = "JPG") + picture type(1) + description bytes.
        var idx = FindIdentifier(bytes, "PIC");
        Assert.True(idx > 0);
        // encoding byte
        Assert.Equal(IsoEncoding, bytes[idx + 6]);
        // 3-byte image format: "JPG"
        Assert.Equal((byte)'J', bytes[idx + 7]);
        Assert.Equal((byte)'P', bytes[idx + 8]);
        Assert.Equal((byte)'G', bytes[idx + 9]);
        // picture type (CoverFront = 0x03)
        Assert.Equal((byte)Id3v2AttachedPictureType.CoverFront, bytes[idx + 10]);

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;
        var readPic = read.AttachedPictures.FirstOrDefault();
        Assert.NotNull(readPic);
        Assert.Equal("JPG", readPic!.ImageFormat);
        Assert.Equal(Id3v2AttachedPictureType.CoverFront, readPic.PictureType);
        Assert.Equal("Cover", readPic.Description);
        Assert.Equal(pic.PictureData, readPic.PictureData);
    }

    [Fact]
    public void PicFrameImageFormatIsTrimmedToThreeCharactersForV220()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220) { ImageFormat = "JPEG" };
        Assert.Equal("JPE", pic.ImageFormat);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Property accessors materialize to correct v2.2 frame identifiers.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void PropertyAccessorsMaterializeV220TextFrameIdentifiers()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        title.Values.Add("t");
        var artist = new Id3v2TextFrame(Id3v2Version.Id3v220, "TP1") { TextEncoding = Id3v2FrameEncodingType.Default };
        artist.Values.Add("a");
        var album = new Id3v2TextFrame(Id3v2Version.Id3v220, "TAL") { TextEncoding = Id3v2FrameEncodingType.Default };
        album.Values.Add("al");
        var year = new Id3v2TextFrame(Id3v2Version.Id3v220, "TYE") { TextEncoding = Id3v2FrameEncodingType.Default };
        year.Values.Add("2026");
        var track = new Id3v2TextFrame(Id3v2Version.Id3v220, "TRK") { TextEncoding = Id3v2FrameEncodingType.Default };
        track.Values.Add("3");

        tag.TrackTitle = title;
        tag.Artist = artist;
        tag.AlbumTitle = album;
        tag.YearRecording = year;
        tag.TrackNumber = track;

        Assert.Equal("TT2", tag.TrackTitle!.Identifier);
        Assert.Equal("TP1", tag.Artist!.Identifier);
        Assert.Equal("TAL", tag.AlbumTitle!.Identifier);
        Assert.Equal("TYE", tag.YearRecording!.Identifier);
        Assert.Equal("TRK", tag.TrackNumber!.Identifier);

        // Spot-check that all identifiers stored in the tag are 3 chars (v2.2).
        Assert.All(tag.Frames, f => Assert.Equal(3, f.Identifier!.Length));
    }

    [Fact]
    public void CommentFrameIdentifierIsComForV220()
    {
        var c = new Id3v2CommentFrame(Id3v2Version.Id3v220);
        Assert.Equal("COM", c.Identifier);
    }

    [Fact]
    public void UniqueFileIdentifierFrameIdentifierIsUfiForV220()
    {
        var ufi = new Id3v2UniqueFileIdentifierFrame(Id3v2Version.Id3v220);
        Assert.Equal("UFI", ufi.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Regression: Id3v2Tag.Equals must not NRE on empty tags whose ExtendedHeader is null.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void EmptyV220TagsAreEqualWithoutNullReferenceException()
    {
        var a = new Id3v2Tag(Id3v2Version.Id3v220);
        var b = new Id3v2Tag(Id3v2Version.Id3v220);

        // Default ExtendedHeader is null (from the field initializer); comparing two nulls must be fine.
        Assert.Null(a.ExtendedHeader);
        Assert.Null(b.ExtendedHeader);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
        Assert.True(a.Equals((object?)b));
    }

    [Fact]
    public void V220TagEqualsReflexive()
    {
        var a = new Id3v2Tag(Id3v2Version.Id3v220);
        Assert.True(a.Equals(a));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Unknown/unregistered frame identifier handling.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnknownFrameIdentifierIsSkippedGracefully()
    {
        // Build a v2.2 tag with a known TT2 frame preceded by a fake frame whose identifier
        // is made up of valid uppercase characters but unknown to the factory.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        title.Values.Add("Song");
        tag.TrackTitle = title;
        var tagBytes = tag.ToByteArray();

        // Inject an unknown frame "ZZZ" with 1 byte of data after the header,
        // before the real TT2 frame.
        var known = new byte[tagBytes.Length - 10]; // body after the 10-byte header
        System.Array.Copy(tagBytes, 10, known, 0, known.Length);
        var fake = new byte[] { (byte)'Z', (byte)'Z', (byte)'Z', 0x00, 0x00, 0x01, 0x42 };
        var body = new byte[fake.Length + known.Length];
        System.Array.Copy(fake, 0, body, 0, fake.Length);
        System.Array.Copy(known, 0, body, fake.Length, known.Length);

        // Rebuild header with the new size.
        var header = new byte[10];
        header[0] = (byte)'I';
        header[1] = (byte)'D';
        header[2] = (byte)'3';
        header[3] = 0x02;
        header[4] = 0x00;
        header[5] = 0x00;
        var synchsafe = Id3v2Tag.GetSynchsafeValue(body.Length);
        header[6] = (byte)((synchsafe >> 24) & 0xFF);
        header[7] = (byte)((synchsafe >> 16) & 0xFF);
        header[8] = (byte)((synchsafe >> 8) & 0xFF);
        header[9] = (byte)(synchsafe & 0xFF);

        var combined = new byte[header.Length + body.Length];
        System.Array.Copy(header, 0, combined, 0, header.Length);
        System.Array.Copy(body, 0, combined, header.Length, body.Length);

        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(combined), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;

        // The known frame must still be readable after the unknown one.
        Assert.NotNull(read.TrackTitle);
        Assert.Equal("Song", read.TrackTitle!.Values[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Full round-trip with multiple v2.2 frames.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FullTagRoundTripPreservesAllFrames()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);

        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        title.Values.Add("Round Trip Title");
        tag.TrackTitle = title;

        var artist = new Id3v2TextFrame(Id3v2Version.Id3v220, "TP1") { TextEncoding = Id3v2FrameEncodingType.Default };
        artist.Values.Add("Round Trip Artist");
        tag.Artist = artist;

        var album = new Id3v2TextFrame(Id3v2Version.Id3v220, "TAL") { TextEncoding = Id3v2FrameEncodingType.Default };
        album.Values.Add("Round Trip Album");
        tag.AlbumTitle = album;

        var year = new Id3v2TextFrame(Id3v2Version.Id3v220, "TYE") { TextEncoding = Id3v2FrameEncodingType.Default };
        year.Values.Add("2026");
        tag.YearRecording = year;

        var track = new Id3v2TextFrame(Id3v2Version.Id3v220, "TRK") { TextEncoding = Id3v2FrameEncodingType.Default };
        track.Values.Add("7/12");
        tag.TrackNumber = track;

        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "PNG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "cover",
            PictureData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        };
        tag.AttachedPictures.Add(pic);

        var bytes = tag.ToByteArray();
        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(bytes), TagOrigin.Start);
        Assert.NotNull(offset);
        var read = (Id3v2Tag)offset!.AudioTag;

        Assert.Equal(Id3v2Version.Id3v220, read.Version);
        Assert.Equal("Round Trip Title", read.TrackTitle!.Values[0]);
        Assert.Equal("Round Trip Artist", read.Artist!.Values[0]);
        Assert.Equal("Round Trip Album", read.AlbumTitle!.Values[0]);
        Assert.Equal("2026", read.YearRecording!.Values[0]);
        Assert.Equal("7/12", read.TrackNumber!.Values[0]);

        var readPic = read.AttachedPictures.FirstOrDefault();
        Assert.NotNull(readPic);
        Assert.Equal("PNG", readPic!.ImageFormat);
        Assert.Equal(Id3v2AttachedPictureType.CoverFront, readPic.PictureType);
        Assert.Equal("cover", readPic.Description);
        Assert.Equal(pic.PictureData, readPic.PictureData);
    }

    [Fact]
    public void RoundTripProducesByteIdenticalOutput()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2") { TextEncoding = Id3v2FrameEncodingType.Default };
        title.Values.Add("Stable");
        tag.TrackTitle = title;

        var first = tag.ToByteArray();
        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(new StreamBuffer(first), TagOrigin.Start);
        Assert.NotNull(offset);
        var reparsed = (Id3v2Tag)offset!.AudioTag;
        var second = reparsed.ToByteArray();

        Assert.Equal(first, second);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers.
    ////------------------------------------------------------------------------------------------------------------------------------

    private static Id3v2TextFrame MakeTextFrame(string identifier, string value, Id3v2FrameEncodingType encoding = Id3v2FrameEncodingType.Default)
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, identifier) { TextEncoding = encoding };
        frame.Values.Add(value);
        return frame;
    }

    private static byte[] BuildRawV220TagWithTt2(string text, byte encodingByte)
    {
        // Construct: ID3 header + TT2 frame carrying (encodingByte, text bytes in ISO-8859-1).
        var textBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
        var frameData = new byte[1 + textBytes.Length];
        frameData[0] = encodingByte;
        System.Array.Copy(textBytes, 0, frameData, 1, textBytes.Length);

        // v2.2 frame header: 3-char id + 3-byte size (big endian).
        var frame = new byte[6 + frameData.Length];
        frame[0] = (byte)'T';
        frame[1] = (byte)'T';
        frame[2] = (byte)'2';
        frame[3] = (byte)((frameData.Length >> 16) & 0xFF);
        frame[4] = (byte)((frameData.Length >> 8) & 0xFF);
        frame[5] = (byte)(frameData.Length & 0xFF);
        System.Array.Copy(frameData, 0, frame, 6, frameData.Length);

        var header = new byte[10];
        header[0] = (byte)'I';
        header[1] = (byte)'D';
        header[2] = (byte)'3';
        header[3] = 0x02;
        header[4] = 0x00;
        header[5] = 0x00;
        var synchsafe = Id3v2Tag.GetSynchsafeValue(frame.Length);
        header[6] = (byte)((synchsafe >> 24) & 0xFF);
        header[7] = (byte)((synchsafe >> 16) & 0xFF);
        header[8] = (byte)((synchsafe >> 8) & 0xFF);
        header[9] = (byte)(synchsafe & 0xFF);

        var result = new byte[header.Length + frame.Length];
        System.Array.Copy(header, 0, result, 0, header.Length);
        System.Array.Copy(frame, 0, result, header.Length, frame.Length);
        return result;
    }

    private static int FindIdentifier(byte[] bytes, string identifier)
    {
        // Skip the first 10 bytes (tag header). Search for the ASCII identifier after that.
        var id = Encoding.ASCII.GetBytes(identifier);
        for (var i = 10; i <= bytes.Length - id.Length; i++)
        {
            var match = true;
            for (var j = 0; j < id.Length; j++)
            {
                if (bytes[i + j] != id[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }
}
