/*
 * Driven by the ID3v2.3.0 spec at docs/id3v2_3_0 - ID3_org.mht.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using AudioVideoLib.Tags;
using Xunit;

public class Id3v230Tests
{
    private const Id3v2Version Version = Id3v2Version.Id3v230;

    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static Id3v2Tag RoundTrip(Id3v2Tag tag)
    {
        var bytes = tag.ToByteArray();
        var reader = new Id3v2TagReader();
        using var stream = new MemoryStream(bytes);
        var offset = reader.ReadFromStream(stream, TagOrigin.Start);
        Assert.NotNull(offset);
        var result = offset!.AudioTag as Id3v2Tag;
        Assert.NotNull(result);
        return result!;
    }

    private static Id3v2TextFrame MakeTextFrame(string identifier, string value, Id3v2FrameEncodingType encoding = Id3v2FrameEncodingType.Default)
    {
        var frame = new Id3v2TextFrame(Version, identifier) { TextEncoding = encoding };
        frame.Values.Add(value);
        return frame;
    }

    private static Id3v2CommentFrame MakeComment(string language, string description, string text, Id3v2FrameEncodingType encoding = Id3v2FrameEncodingType.Default)
    {
        return new Id3v2CommentFrame(Version)
        {
            TextEncoding = encoding,
            Language = language,
            ShortContentDescription = description,
            Text = text
        };
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Header
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void HeaderStartsWithId3Marker()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal((byte)'I', bytes[0]);
        Assert.Equal((byte)'D', bytes[1]);
        Assert.Equal((byte)'3', bytes[2]);
    }

    [Fact]
    public void HeaderContainsVersion3AndRevision0()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal(0x03, bytes[3]);
        Assert.Equal(0x00, bytes[4]);
    }

    [Fact]
    public void HeaderSizeIsStoredAsSynchsafeInteger()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        // Size bytes 6..9 must all have MSB cleared.
        Assert.Equal(0, bytes[6] & 0x80);
        Assert.Equal(0, bytes[7] & 0x80);
        Assert.Equal(0, bytes[8] & 0x80);
        Assert.Equal(0, bytes[9] & 0x80);

        var decoded = Id3v2Tag.GetUnsynchedValue(BitConverter.ToInt32([bytes[9], bytes[8], bytes[7], bytes[6]], 0));
        Assert.Equal(bytes.Length - Id3v2Tag.HeaderSize, decoded);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Header flags
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnsynchronizationFlagIsBit7()
    {
        var tag = new Id3v2Tag(Version) { UseUnsynchronization = true };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal(0x80, bytes[5] & 0x80);
    }

    [Fact]
    public void ExtendedHeaderFlagIsBit6()
    {
        var tag = new Id3v2Tag(Version) { ExtendedHeader = new Id3v2ExtendedHeader() };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal(0x40, bytes[5] & 0x40);
    }

    [Fact]
    public void ExperimentalFlagIsBit5()
    {
        var tag = new Id3v2Tag(Version) { TagIsExperimental = true };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal(0x20, bytes[5] & 0x20);
    }

    [Fact]
    public void LowerFlagBitsAreZeroInV230()
    {
        var tag = new Id3v2Tag(Version)
        {
            UseUnsynchronization = true,
            ExtendedHeader = new Id3v2ExtendedHeader(),
            TagIsExperimental = true
        };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        Assert.Equal(0xE0, bytes[5]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. Extended header v2.3
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ExtendedHeaderWithoutCrcHasSixByteSize()
    {
        var ext = new Id3v2ExtendedHeader();

        Assert.Equal(6, ext.GetHeaderSize(Version));
    }

    [Fact]
    public void ExtendedHeaderWithCrcHasTenByteSize()
    {
        var ext = new Id3v2ExtendedHeader { CrcDataPresent = true };

        Assert.Equal(10, ext.GetHeaderSize(Version));
    }

    [Fact]
    public void ExtendedHeaderFlagsFieldIsTwoBytesInV230()
    {
        var ext = new Id3v2ExtendedHeader();

        Assert.Equal(2, ext.GetFlagsFieldLength(Version));
    }

    [Fact]
    public void ExtendedHeaderSizeIsStoredAsPlainBigEndianInt32NotSynchsafeInV230()
    {
        var tag = new Id3v2Tag(Version) { ExtendedHeader = new Id3v2ExtendedHeader() };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var bytes = tag.ToByteArray();

        // Extended header starts at offset 10. Size is 4 big-endian bytes, value is 6 (excludes itself).
        Assert.Equal(0x00, bytes[10]);
        Assert.Equal(0x00, bytes[11]);
        Assert.Equal(0x00, bytes[12]);
        Assert.Equal(0x06, bytes[13]);
    }

    [Fact]
    public void ExtendedHeaderRoundTripPreservesPaddingSize()
    {
        var ext = new Id3v2ExtendedHeader { PaddingSize = 64 };
        var tag = new Id3v2Tag(Version) { ExtendedHeader = ext };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));

        var parsed = RoundTrip(tag);

        Assert.NotNull(parsed.ExtendedHeader);
        Assert.Equal(64, parsed.ExtendedHeader.PaddingSize);
    }

    [Fact]
    public void ExtendedHeaderCrcRoundTripValidatesAgainstCalculateCrc32()
    {
        var ext = new Id3v2ExtendedHeader { CrcDataPresent = true };
        var tag = new Id3v2Tag(Version) { ExtendedHeader = ext };
        tag.SetFrame(MakeTextFrame("TIT2", "Title"));
        tag.SetFrame(MakeTextFrame("TPE1", "Artist"));

        // CalculateCrc32 hashes the concatenated frame byte output; the on-disk CRC at
        // the end of the extended header must match what the write path would produce.
        var expectedCrc = tag.CalculateCrc32();
        var bytes = tag.ToByteArray();

        // Extended header layout: size (4) + flags (2) + padding size (4) + crc (4)
        var crcOffset = Id3v2Tag.HeaderSize + 4 + 2 + 4;
        var onDiskCrc = (bytes[crcOffset] << 24) | (bytes[crcOffset + 1] << 16) | (bytes[crcOffset + 2] << 8) | bytes[crcOffset + 3];
        Assert.Equal(expectedCrc, onDiskCrc);

        // And the parser should accept the tag without throwing.
        var parsed = RoundTrip(tag);
        Assert.NotNull(parsed.ExtendedHeader);
        Assert.True(parsed.ExtendedHeader.CrcDataPresent);
    }

    [Fact]
    public void ExtendedHeaderCrcCalculationUsesCrc32OverFrameBytes()
    {
        var ext = new Id3v2ExtendedHeader { CrcDataPresent = true };
        var tag = new Id3v2Tag(Version) { ExtendedHeader = ext };
        var titleFrame = MakeTextFrame("TIT2", "Title");
        tag.SetFrame(titleFrame);

        var expected = (int)Crc32.HashToUInt32(titleFrame.ToByteArray());

        Assert.Equal(expected, tag.CalculateCrc32());
    }

    [Fact]
    public void ExtendedHeaderEqualsHandlesBothSidesNullTagRestrictions()
    {
        // Regression: Id3v2ExtendedHeader.Equals must treat two extended headers
        // whose TagRestrictions are both null as equal.
        var a = new Id3v2ExtendedHeader();
        var b = new Id3v2ExtendedHeader();

        Assert.Null(a.TagRestrictions);
        Assert.Null(b.TagRestrictions);
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. Synchsafe size
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(127, 127)]
    [InlineData(128, 0x00000100)]
    [InlineData(257, 0x00000201)]
    [InlineData(0x0FFFFFFF, 0x7F7F7F7F)]
    public void SynchsafeEncodeDecodeKnownPairs(int plain, int synchsafe)
    {
        Assert.Equal(synchsafe, Id3v2Tag.GetSynchsafeValue(plain));
        Assert.Equal(plain, Id3v2Tag.GetUnsynchedValue(synchsafe));
    }

    [Fact]
    public void SynchsafeEncodingNeverSetsMsbOfAnyByte()
    {
        for (var plain = 0; plain < 0x0FFFFFFF; plain += 0x1FFFF)
        {
            var synchsafe = Id3v2Tag.GetSynchsafeValue(plain);
            var b0 = (synchsafe >> 24) & 0xFF;
            var b1 = (synchsafe >> 16) & 0xFF;
            var b2 = (synchsafe >> 8) & 0xFF;
            var b3 = synchsafe & 0xFF;
            Assert.Equal(0, b0 & 0x80);
            Assert.Equal(0, b1 & 0x80);
            Assert.Equal(0, b2 & 0x80);
            Assert.Equal(0, b3 & 0x80);
        }
    }

    [Fact]
    public void SynchsafeMaximumIs28EffectiveBits()
    {
        // 0x0FFFFFFF (~256MB - 1) is the maximum ID3v2 tag size.
        Assert.Equal(Id3v2Tag.MaxAllowedSize, 0x0FFFFFFF);

        var synchsafe = Id3v2Tag.GetSynchsafeValue(Id3v2Tag.MaxAllowedSize);
        Assert.Equal(0x7F7F7F7F, synchsafe);
    }

    [Fact]
    public void RejectsTagWithWrongMagicBytes()
    {
        byte[] invalid =
        [
            (byte)'X', (byte)'Y', (byte)'Z', 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];
        using var stream = new MemoryStream(invalid);
        var reader = new Id3v2TagReader();
        Assert.Null(reader.ReadFromStream(stream, TagOrigin.Start));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. 4-char frame identifiers - round trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("TIT2", "Title")]
    [InlineData("TPE1", "Artist")]
    [InlineData("TALB", "Album")]
    [InlineData("TYER", "2026")]
    [InlineData("TRCK", "3/12")]
    [InlineData("TCON", "Rock")]
    [InlineData("TCOM", "Composer")]
    public void TextFrameRoundTripsKnown4CharIdentifiers(string identifier, string value)
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame(identifier, value));

        var parsed = RoundTrip(tag);

        var textFrame = parsed.GetFrame<Id3v2TextFrame>(identifier);
        Assert.NotNull(textFrame);
        Assert.Equal(identifier, textFrame!.Identifier);
        Assert.Equal(value, textFrame.Values.Single());
    }

    [Fact]
    public void CommFrameHasFourCharIdentifier()
    {
        var frame = MakeComment("eng", "desc", "hello");

        Assert.Equal("COMM", frame.Identifier);
        Assert.Equal(4, frame.Identifier!.Length);
    }

    [Fact]
    public void ApicFrameHasFourCharIdentifier()
    {
        var frame = new Id3v2AttachedPictureFrame(Version);

        Assert.Equal("APIC", frame.Identifier);
    }

    [Fact]
    public void UfidFrameHasFourCharIdentifier()
    {
        var frame = new Id3v2UniqueFileIdentifierFrame(Version);

        Assert.Equal("UFID", frame.Identifier);
    }

    [Fact]
    public void UserFrameHasFourCharIdentifier()
    {
        var frame = new Id3v2TermsOfUseFrame(Version);

        Assert.Equal("USER", frame.Identifier);
    }

    [Fact]
    public void GridFrameHasFourCharIdentifier()
    {
        var frame = new Id3v2GroupIdentificationRegistrationFrame(Version);

        Assert.Equal("GRID", frame.Identifier);
    }

    [Fact]
    public void EtcoFrameHasFourCharIdentifier()
    {
        var frame = new Id3v2EventTimingCodesFrame(Version);

        Assert.Equal("ETCO", frame.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 6. Frame header layout
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameHeaderUsesFourByteBigEndianSizeNotSynchsafe()
    {
        // Use a payload larger than 127 so that the distinction between plain BE
        // and synchsafe is observable.
        var value = new string('A', 200);
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", value));

        var bytes = tag.ToByteArray();

        // Locate the TIT2 frame header (after 10-byte tag header).
        var frameOffset = Id3v2Tag.HeaderSize;
        Assert.Equal((byte)'T', bytes[frameOffset]);
        Assert.Equal((byte)'I', bytes[frameOffset + 1]);
        Assert.Equal((byte)'T', bytes[frameOffset + 2]);
        Assert.Equal((byte)'2', bytes[frameOffset + 3]);

        // v2.3 frame size is plain big-endian int32: value + 1 encoding byte = 201.
        var frameSize = (bytes[frameOffset + 4] << 24)
                       | (bytes[frameOffset + 5] << 16)
                       | (bytes[frameOffset + 6] << 8)
                       | bytes[frameOffset + 7];
        Assert.Equal(201, frameSize);

        // Flags field: 2 bytes after size. Default construction leaves
        // TagAlterPreservation and FileAlterPreservation false, which the writer
        // emits as 0xC000 (bit set = discard-on-alter, per spec).
        Assert.Equal(0xC0, bytes[frameOffset + 8]);
        Assert.Equal(0x00, bytes[frameOffset + 9]);
    }

    [Fact]
    public void FrameHeaderIdentifierFieldLengthIsFourInV230()
    {
        Assert.Equal(4, Id3v2Frame.GetIdentifierFieldLength(Version));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 7. Frame flags v2.3
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameTagAlterPreservationRoundTrips()
    {
        var tag = new Id3v2Tag(Version);
        var frame = MakeTextFrame("TIT2", "Title");
        frame.TagAlterPreservation = false;
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(roundTripped);
        Assert.False(roundTripped!.TagAlterPreservation);
    }

    [Fact]
    public void FrameFileAlterPreservationRoundTrips()
    {
        var tag = new Id3v2Tag(Version);
        var frame = MakeTextFrame("TIT2", "Title");
        frame.FileAlterPreservation = false;
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(roundTripped);
        Assert.False(roundTripped!.FileAlterPreservation);
    }

    [Fact]
    public void FrameReadOnlyRoundTrips()
    {
        var tag = new Id3v2Tag(Version);
        var frame = MakeTextFrame("TIT2", "Title");
        frame.IsReadOnly = true;
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.IsReadOnly);
    }

    [Fact]
    public void FrameCompressionEncryptionGroupingFlagsAreV23Compatible()
    {
        // The v2.3 frame header flags struct defines compression/encryption/grouping
        // at 0x80/0x40/0x20 respectively in the low byte. Verify that the
        // property setters accept those values without error when Version is v2.3.
        var frame = MakeTextFrame("TIT2", "Title");

        frame.UseCompression = true;
        frame.UseEncryption = true;
        frame.UseGroupingIdentity = true;

        Assert.True(frame.UseCompression);
        Assert.True(frame.UseEncryption);
        Assert.True(frame.UseGroupingIdentity);
    }

    [Fact]
    public void FrameFlagDefaultsAreFalse()
    {
        var frame = MakeTextFrame("TIT2", "Title");

        // Default-constructed preservation flags are false; that serializes to bit-set
        // in the wire format, which per spec means "discard this frame on tag/file alter".
        Assert.False(frame.TagAlterPreservation);
        Assert.False(frame.FileAlterPreservation);
        Assert.False(frame.IsReadOnly);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 8. Text encodings
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TextEncoding00IsIso88591()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Hello", Id3v2FrameEncodingType.Default));

        var bytes = tag.ToByteArray();

        // Locate frame data: 10 header + 10 frame header.
        var dataOffset = Id3v2Tag.HeaderSize + 10;
        Assert.Equal(0x00, bytes[dataOffset]); // encoding byte
        Assert.Equal((byte)'H', bytes[dataOffset + 1]);
        Assert.Equal((byte)'o', bytes[dataOffset + 5]);
    }

    [Fact]
    public void TextEncoding01IsUtf16LittleEndianByDefaultWithBom()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Hi", Id3v2FrameEncodingType.UTF16LittleEndian));

        var bytes = tag.ToByteArray();

        var dataOffset = Id3v2Tag.HeaderSize + 10;
        Assert.Equal(0x01, bytes[dataOffset]); // encoding byte
        // LE BOM: FF FE
        Assert.Equal(0xFF, bytes[dataOffset + 1]);
        Assert.Equal(0xFE, bytes[dataOffset + 2]);
    }

    [Fact]
    public void BigEndianBomIsDetectedWhenReadingUtf16Frame()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "Hi", Id3v2FrameEncodingType.UTF16BigEndian));

        var bytes = tag.ToByteArray();

        var dataOffset = Id3v2Tag.HeaderSize + 10;
        Assert.Equal(0x01, bytes[dataOffset]); // Still encoding 0x01 (v2.3 has no 0x02 on the wire)
        // BE BOM: FE FF
        Assert.Equal(0xFE, bytes[dataOffset + 1]);
        Assert.Equal(0xFF, bytes[dataOffset + 2]);

        var parsed = RoundTrip(tag);
        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(roundTripped);
        Assert.Equal("Hi", roundTripped!.Values.Single());
    }

    [Fact]
    public void Utf16FrameRoundTripsUnicodeText()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "日本語", Id3v2FrameEncodingType.UTF16LittleEndian));

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(roundTripped);
        Assert.Equal("日本語", roundTripped!.Values.Single());
    }

    [Fact]
    public void Iso88591FrameRoundTripsLatin1Text()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TPE1", "Björk", Id3v2FrameEncodingType.Default));

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TextFrame>("TPE1");
        Assert.NotNull(roundTripped);
        Assert.Equal("Björk", roundTripped!.Values.Single());
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 9. APIC attached picture frame
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ApicFrameWritesMimeTypeAsNullTerminatedLatin1String()
    {
        var frame = new Id3v2AttachedPictureFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "image/png",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "cover",
            PictureData = [0x89, 0x50, 0x4E, 0x47]
        };

        var tag = new Id3v2Tag(Version);
        tag.SetFrame(frame);

        var bytes = tag.ToByteArray();

        // Data begins after the 10-byte tag header + 10-byte frame header.
        var dataOffset = Id3v2Tag.HeaderSize + 10;
        Assert.Equal(0x00, bytes[dataOffset]); // encoding
        var mimeBytes = "image/png"u8.ToArray();
        for (var i = 0; i < mimeBytes.Length; i++)
        {
            Assert.Equal(mimeBytes[i], bytes[dataOffset + 1 + i]);
        }

        // v2.3 APIC is NOT the fixed-length 3-byte PIC format from v2.2.
        Assert.Equal(0x00, bytes[dataOffset + 1 + mimeBytes.Length]); // null terminator after MIME
    }

    [Fact]
    public void ApicFrameRoundTripsAllFields()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03 };
        var frame = new Id3v2AttachedPictureFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "image/png",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "front cover",
            PictureData = png
        };

        var tag = new Id3v2Tag(Version);
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2AttachedPictureFrame>("APIC");
        Assert.NotNull(roundTripped);
        Assert.Equal("image/png", roundTripped!.ImageFormat);
        Assert.Equal(Id3v2AttachedPictureType.CoverFront, roundTripped.PictureType);
        Assert.Equal("front cover", roundTripped.Description);
        Assert.Equal(png, roundTripped.PictureData);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 10. USER terms-of-use frame
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UserFrameRoundTripsEncodingLanguageAndText()
    {
        var frame = new Id3v2TermsOfUseFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Language = "eng",
            Text = "You may use this file freely."
        };

        var tag = new Id3v2Tag(Version);
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2TermsOfUseFrame>("USER");
        Assert.NotNull(roundTripped);
        Assert.Equal("eng", roundTripped!.Language);
        Assert.Equal("You may use this file freely.", roundTripped.Text);
    }

    [Fact]
    public void UserFrameDataLayoutIsEncodingLanguageText()
    {
        var frame = new Id3v2TermsOfUseFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Language = "eng",
            Text = "terms"
        };

        var data = frame.Data;

        Assert.Equal(0x00, data[0]); // encoding
        Assert.Equal((byte)'e', data[1]);
        Assert.Equal((byte)'n', data[2]);
        Assert.Equal((byte)'g', data[3]);
        Assert.Equal((byte)'t', data[4]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 11. GRID group identification registration frame
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void GridFrameRoundTripsOwnerSymbolAndData()
    {
        var frame = new Id3v2GroupIdentificationRegistrationFrame(Version)
        {
            OwnerIdentifier = "http://example.com/grid",
            GroupSymbol = 0x80,
            GroupDependentData = [0x11, 0x22, 0x33]
        };

        var tag = new Id3v2Tag(Version);
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2GroupIdentificationRegistrationFrame>("GRID");
        Assert.NotNull(roundTripped);
        Assert.Equal("http://example.com/grid", roundTripped!.OwnerIdentifier);
        Assert.Equal(0x80, roundTripped.GroupSymbol);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, roundTripped.GroupDependentData);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 12. COMM comment frame
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CommentFrameRoundTripsIso88591()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeComment("eng", "desc", "the comment"));

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2CommentFrame>("COMM");
        Assert.NotNull(roundTripped);
        Assert.Equal("eng", roundTripped!.Language);
        Assert.Equal("desc", roundTripped.ShortContentDescription);
        Assert.Equal("the comment", roundTripped.Text);
    }

    [Fact]
    public void CommentFrameRoundTripsUtf16()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeComment("jpn", "説明", "コメント", Id3v2FrameEncodingType.UTF16LittleEndian));

        var parsed = RoundTrip(tag);

        var roundTripped = parsed.GetFrame<Id3v2CommentFrame>("COMM");
        Assert.NotNull(roundTripped);
        Assert.Equal("jpn", roundTripped!.Language);
        Assert.Equal("説明", roundTripped.ShortContentDescription);
        Assert.Equal("コメント", roundTripped.Text);
    }

    [Fact]
    public void CommentFrameDataStartsWithEncodingAndThreeByteLanguage()
    {
        var frame = MakeComment("eng", "d", "t");

        var data = frame.Data;

        Assert.Equal(0x00, data[0]); // encoding
        Assert.Equal((byte)'e', data[1]);
        Assert.Equal((byte)'n', data[2]);
        Assert.Equal((byte)'g', data[3]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 13. Multiple frames of the same identifier
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void MultipleCommentsWithDifferentDescriptorsArePreservedInOrder()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeComment("eng", "one", "first"));
        tag.SetFrame(MakeComment("eng", "two", "second"));
        tag.SetFrame(MakeComment("eng", "three", "third"));

        var parsed = RoundTrip(tag);

        var comments = parsed.GetFrames<Id3v2CommentFrame>("COMM").ToList();
        Assert.Equal(3, comments.Count);
        Assert.Equal("one", comments[0].ShortContentDescription);
        Assert.Equal("two", comments[1].ShortContentDescription);
        Assert.Equal("three", comments[2].ShortContentDescription);
    }

    [Fact]
    public void MultipleTxxxFramesWithDifferentDescriptionsArePreserved()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(new Id3v2UserDefinedTextInformationFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Description = "KeyA",
            Value = "ValueA"
        });
        tag.SetFrame(new Id3v2UserDefinedTextInformationFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Description = "KeyB",
            Value = "ValueB"
        });

        var parsed = RoundTrip(tag);

        var txxx = parsed.GetFrames<Id3v2UserDefinedTextInformationFrame>("TXXX").ToList();
        Assert.Equal(2, txxx.Count);
        Assert.Equal("KeyA", txxx[0].Description);
        Assert.Equal("KeyB", txxx[1].Description);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 14. Id3v2Tag.Equals — no NRE when ExtendedHeader is null
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TagEqualsDoesNotThrowWhenNeitherSideHasExtendedHeader()
    {
        var left = new Id3v2Tag(Version);
        var right = new Id3v2Tag(Version);

        Assert.Null(left.ExtendedHeader);
        Assert.Null(right.ExtendedHeader);

        var equal = left.Equals(right);

        Assert.True(equal);
    }

    [Fact]
    public void TagEqualsReturnsTrueForTwoEmptyV230Tags()
    {
        var a = new Id3v2Tag(Version);
        var b = new Id3v2Tag(Version);

        Assert.Equal(a, b);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 15. Full round-trip
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FullTagRoundTripPreservesAllFrameContent()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "My Song"));
        tag.SetFrame(MakeTextFrame("TPE1", "The Artist"));
        tag.SetFrame(MakeTextFrame("TALB", "The Album"));
        tag.SetFrame(MakeTextFrame("TYER", "2026"));
        tag.SetFrame(MakeTextFrame("TRCK", "5/12"));
        tag.SetFrame(MakeComment("eng", "note", "A short comment."));
        tag.SetFrame(new Id3v2AttachedPictureFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "image/jpeg",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "cover",
            PictureData = [.. Enumerable.Range(0, 32).Select(i => (byte)i)]
        });
        tag.SetFrame(new Id3v2UniqueFileIdentifierFrame(Version)
        {
            OwnerIdentifier = "http://www.id3.org/dummy/ufid.html",
            IdentifierData = Encoding.ASCII.GetBytes("uniq-id-1")
        });
        tag.SetFrame(new Id3v2TermsOfUseFrame(Version)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Language = "eng",
            Text = "Terms apply."
        });

        var parsed = RoundTrip(tag);

        Assert.Equal("My Song", parsed.GetFrame<Id3v2TextFrame>("TIT2")!.Values.Single());
        Assert.Equal("The Artist", parsed.GetFrame<Id3v2TextFrame>("TPE1")!.Values.Single());
        Assert.Equal("The Album", parsed.GetFrame<Id3v2TextFrame>("TALB")!.Values.Single());
        Assert.Equal("2026", parsed.GetFrame<Id3v2TextFrame>("TYER")!.Values.Single());
        Assert.Equal("5/12", parsed.GetFrame<Id3v2TextFrame>("TRCK")!.Values.Single());

        var comm = parsed.GetFrame<Id3v2CommentFrame>("COMM");
        Assert.NotNull(comm);
        Assert.Equal("A short comment.", comm!.Text);

        var apic = parsed.GetFrame<Id3v2AttachedPictureFrame>("APIC");
        Assert.NotNull(apic);
        Assert.Equal("image/jpeg", apic!.ImageFormat);
        Assert.Equal(32, apic.PictureData.Length);

        var ufid = parsed.GetFrame<Id3v2UniqueFileIdentifierFrame>("UFID");
        Assert.NotNull(ufid);
        Assert.Equal("http://www.id3.org/dummy/ufid.html", ufid!.OwnerIdentifier);

        var user = parsed.GetFrame<Id3v2TermsOfUseFrame>("USER");
        Assert.NotNull(user);
        Assert.Equal("Terms apply.", user!.Text);
    }

    [Fact]
    public void FullTagRoundTripReadsBackIdenticalByteStreamOnSecondWrite()
    {
        var tag = new Id3v2Tag(Version);
        tag.SetFrame(MakeTextFrame("TIT2", "My Song"));
        tag.SetFrame(MakeTextFrame("TPE1", "The Artist"));
        tag.SetFrame(MakeTextFrame("TALB", "The Album"));

        var first = tag.ToByteArray();
        var parsed = RoundTrip(tag);
        var second = parsed.ToByteArray();

        Assert.Equal(first, second);
    }
}
