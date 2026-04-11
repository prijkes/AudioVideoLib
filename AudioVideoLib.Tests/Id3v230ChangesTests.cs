/*
 * Tests for differences between ID3v2.2.0 and ID3v2.3.0.
 * Spec: docs/id3v2_3_0-changes - ID3_org.mht
 *
 * Focus is on per-version divergences, not general v2.3 behavior.
 */
namespace AudioVideoLib.Tests;

using System.IO.Hashing;
using System.Linq;
using System.Text;

using AudioVideoLib.Tags;

using Xunit;

public class Id3v230ChangesTests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // 1. Frame identifier length: 3 chars (v2.2) vs 4 chars (v2.3)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameIdentifierLength_V220UsesThreeBytes_V230UsesFourBytes()
    {
        Assert.Equal(3, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v220));
        Assert.Equal(3, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v221));
        Assert.Equal(4, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v230));
        Assert.Equal(4, Id3v2Frame.GetIdentifierFieldLength(Id3v2Version.Id3v240));
    }

    [Fact]
    public void FrameIdentifierValidation_RejectsFourCharOnV220_AcceptsOnV230()
    {
        Assert.False(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "TIT2"));
        Assert.True(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v220, "TT2"));

        Assert.False(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v230, "TT2"));
        Assert.True(Id3v2Frame.IsValidIdentifier(Id3v2Version.Id3v230, "TIT2"));
    }

    [Fact]
    public void TextFrame_TrackTitle_V220_SerializesWithThreeCharId_TT2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220)
        {
            TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v220, "Hello"),
        };

        var bytes = tag.ToByteArray();
        var framesRegion = ExtractFramesRegion(bytes);

        // v2.2 frame layout: 3-byte ID followed by 3-byte big-endian size, no flags.
        Assert.Equal((byte)'T', framesRegion[0]);
        Assert.Equal((byte)'T', framesRegion[1]);
        Assert.Equal((byte)'2', framesRegion[2]);
    }

    [Fact]
    public void TextFrame_TrackTitle_V230_SerializesWithFourCharId_TIT2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230)
        {
            TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v230, "Hello"),
        };

        var bytes = tag.ToByteArray();
        var framesRegion = ExtractFramesRegion(bytes);

        Assert.Equal((byte)'T', framesRegion[0]);
        Assert.Equal((byte)'I', framesRegion[1]);
        Assert.Equal((byte)'T', framesRegion[2]);
        Assert.Equal((byte)'2', framesRegion[3]);
    }

    [Fact]
    public void TextFrame_Artist_EmitsTP1OnV220_AndTPE1OnV230()
    {
        Assert.Equal("TP1", Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v220, Id3v2TextFrameIdentifier.Artist));
        Assert.Equal("TPE1", Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v230, Id3v2TextFrameIdentifier.Artist));
    }

    [Fact]
    public void TextFrame_ComposerName_EmitsTCMOnV220_AndTCOMOnV230()
    {
        Assert.Equal("TCM", Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v220, Id3v2TextFrameIdentifier.ComposerName));
        Assert.Equal("TCOM", Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v230, Id3v2TextFrameIdentifier.ComposerName));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 2. Frame size field: 3 bytes (v2.2) vs 4 bytes (v2.3)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameHeaderSize_V220IsSixBytes_V230IsTenBytes()
    {
        // Build a minimal v2.2 frame to measure header overhead.
        var v220Frame = CreateSimpleTitle(Id3v2Version.Id3v220, "X");
        var v220Tag = new Id3v2Tag(Id3v2Version.Id3v220) { TrackTitle = v220Frame };
        var v220Frames = ExtractFramesRegion(v220Tag.ToByteArray());
        var v220FrameBytes = v220Frame.ToByteArray();

        var v230Frame = CreateSimpleTitle(Id3v2Version.Id3v230, "X");
        var v230Tag = new Id3v2Tag(Id3v2Version.Id3v230) { TrackTitle = v230Frame };
        var v230Frames = ExtractFramesRegion(v230Tag.ToByteArray());
        var v230FrameBytes = v230Frame.ToByteArray();

        // v2.2: 3 + 3 = 6-byte header, v2.3: 4 + 4 + 2 = 10-byte header.
        // The payload (encoding byte + "X") is identical, so the difference in total
        // bytes must be exactly 4 (10 - 6).
        Assert.Equal(4, v230FrameBytes.Length - v220FrameBytes.Length);
        Assert.Equal(4, v230Frames.Length - v220Frames.Length);
    }

    [Fact]
    public void V220Frame_SizeFieldIsThreeBytesBigEndian()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2");
        frame.Values.Add("Abc");

        var bytes = frame.ToByteArray();
        // 3-byte id + 3-byte size + payload
        Assert.True(bytes.Length >= 6);
        var payloadLength = bytes.Length - 6;
        // Read the 3-byte big-endian size.
        var size = (bytes[3] << 16) | (bytes[4] << 8) | bytes[5];
        Assert.Equal(payloadLength, size);
    }

    [Fact]
    public void V230Frame_SizeFieldIsFourBytesBigEndian()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2");
        frame.Values.Add("Abc");

        var bytes = frame.ToByteArray();
        // 4-byte id + 4-byte size + 2-byte flags + payload
        Assert.True(bytes.Length >= 10);
        var payloadLength = bytes.Length - 10;
        var size = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
        Assert.Equal(payloadLength, size);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 3. Extended header added in v2.3 (absent in v2.2)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ExtendedHeader_UseFlagIsIgnoredOnV220()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220) { ExtendedHeader = new Id3v2ExtendedHeader() };

        // v2.2 has no extended header concept; UseExtendedHeader must stay false.
        Assert.False(tag.UseExtendedHeader);

        // Round-trip should not emit an extended header nor set the v2.3
        // extended-header flag bit (0x40) in the header flags byte.
        var bytes = tag.ToByteArray();
        Assert.Equal(0, bytes[5] & 0x40);
    }

    [Fact]
    public void ExtendedHeader_EnabledOnV230_SetsHeaderFlagBit()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230)
        {
            ExtendedHeader = new Id3v2ExtendedHeader(),
            TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v230, "Hello"),
        };

        Assert.True(tag.UseExtendedHeader);

        var bytes = tag.ToByteArray();
        Assert.Equal(0x40, bytes[5] & 0x40);
    }

    [Fact]
    public void ExtendedHeader_GetHeaderSize_IsZeroForV220_SixForV230Minimal()
    {
        var header = new Id3v2ExtendedHeader();
        Assert.Equal(0, header.GetHeaderSize(Id3v2Version.Id3v220));
        Assert.Equal(0, header.GetHeaderSize(Id3v2Version.Id3v221));
        Assert.Equal(6, header.GetHeaderSize(Id3v2Version.Id3v230));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 4. CRC (optional, 32-bit) added in v2.3 extended header
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CalculateCrc32_IsZeroOnV220()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220) { TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v220, "Hello") };

        Assert.Equal(0, tag.CalculateCrc32());
    }

    [Fact]
    public void CalculateCrc32_IsNonZeroOnV230WithFrames_AndMatchesFrameRegionHash()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230) { TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v230, "Hello") };

        var calculated = tag.CalculateCrc32();

        // Compute the expected CRC over the serialized frames only, matching
        // Id3v2.3.0 spec: "the CRC should be calculated before unsynchronization
        // on the data between the extended header and the padding, i.e. the
        // frames and only the frames."
        var framesOnly = tag.Frames
            .Select(f => f.ToByteArray())
            .SelectMany(b => b)
            .ToArray();
        var expected = (int)Crc32.HashToUInt32(framesOnly);

        Assert.Equal(expected, calculated);
        Assert.NotEqual(0, calculated);
    }

    [Fact]
    public void ExtendedHeader_WithCrc_WritesCrcAfterExtendedHeaderFlags_OnV230()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230)
        {
            ExtendedHeader = new Id3v2ExtendedHeader { CrcDataPresent = true },
            TrackTitle = CreateSimpleTitle(Id3v2Version.Id3v230, "CRC!"),
        };

        var bytes = tag.ToByteArray();
        var calculated = tag.CalculateCrc32();

        // Layout after 10-byte tag header:
        //   4 bytes extended header size
        //   2 bytes extended flags
        //   4 bytes padding size
        //   4 bytes CRC-32  <-- we want this
        var crcOffset = 10 + 4 + 2 + 4;
        Assert.True(bytes.Length >= crcOffset + 4);
        var writtenCrc = (bytes[crcOffset] << 24)
                        | (bytes[crcOffset + 1] << 16)
                        | (bytes[crcOffset + 2] << 8)
                        | bytes[crcOffset + 3];
        Assert.Equal(calculated, writtenCrc);

        // Extended flags field must have the CRC-present bit set (top bit of the
        // first flags byte, %1xxxxxxx 00000000 per spec).
        Assert.Equal(0x80, bytes[14] & 0x80);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 5. Per-frame flags added in v2.3
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameFlags_AreNotWrittenOnV220()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2");
        frame.Values.Add("X");

        var bytes = frame.ToByteArray();
        // 3 + 3 = 6 byte header only; next byte should be the payload (encoding).
        // On v2.3 this position would be part of the 2-byte flags word.
        // Default (ISO-8859-1) encoding marker is 0x00.
        Assert.Equal(0x00, bytes[6]);
    }

    [Fact]
    public void FrameFlags_V230_TagAlterPreservation_IsBit15_OfFlagsWord()
    {
        // v2.3 TagAlterPreservation == 0x8000; the frame header Flags word is
        // written at bytes 8..9 (after 4-id + 4-size). TagAlterPreservation uses
        // "0 = preserve", so setting the property to false should set the bit.
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2") { TagAlterPreservation = false };
        frame.Values.Add("X");

        var bytes = frame.ToByteArray();
        Assert.True(bytes.Length >= 10);
        var flags = (bytes[8] << 8) | bytes[9];
        Assert.Equal(0x8000, flags & 0x8000);
    }

    [Fact]
    public void FrameFlags_V230_FileAlterPreservation_IsBit14_OfFlagsWord()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2") { FileAlterPreservation = false };
        frame.Values.Add("X");

        var bytes = frame.ToByteArray();
        var flags = (bytes[8] << 8) | bytes[9];
        Assert.Equal(0x4000, flags & 0x4000);
    }

    [Fact]
    public void FrameFlags_V230_ReadOnly_IsBit13_OfFlagsWord()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2") { IsReadOnly = true };
        frame.Values.Add("X");

        var bytes = frame.ToByteArray();
        var flags = (bytes[8] << 8) | bytes[9];
        Assert.Equal(0x2000, flags & 0x2000);
    }

    [Fact]
    public void FrameFlags_V230_Compressed_Encrypted_Grouped_AreLowByteBits()
    {
        // Per spec: %abc00000 %ijk00000 where i=compressed(0x80), j=encrypted(0x40), k=grouping(0x20).
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2")
        {
            UseCompression = true,
            UseEncryption = true,
            UseGroupingIdentity = true,
        };
        frame.Values.Add("X");

        var bytes = frame.ToByteArray();
        var flags = (bytes[8] << 8) | bytes[9];
        Assert.Equal(0x80, flags & 0x80);
        Assert.Equal(0x40, flags & 0x40);
        Assert.Equal(0x20, flags & 0x20);
    }

    [Fact]
    public void FrameFlags_V220_CannotSetPerFrameFlags()
    {
        // v2.2 frames must silently reject alter/preservation flags — the v2.3+-only
        // getters always return false on a v2.2 frame regardless of setter calls.
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2")
        {
            TagAlterPreservation = true,
            FileAlterPreservation = true,
            IsReadOnly = true,
            UseCompression = true,
            UseEncryption = true,
            UseGroupingIdentity = true,
        };

        Assert.False(frame.TagAlterPreservation);
        Assert.False(frame.FileAlterPreservation);
        Assert.False(frame.IsReadOnly);
        Assert.False(frame.UseCompression);
        Assert.False(frame.UseEncryption);
        Assert.False(frame.UseGroupingIdentity);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 6. PIC -> APIC: image format (3 chars) vs MIME type (latin-1 string)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void AttachedPictureFrame_V220_IdentifierIsPIC()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            ImageFormat = "JPG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = [0xFF, 0xD8, 0xFF, 0xE0],
        };

        Assert.Equal("PIC", pic.Identifier);
    }

    [Fact]
    public void AttachedPictureFrame_V230_IdentifierIsAPIC()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v230)
        {
            ImageFormat = "image/jpeg",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = [0xFF, 0xD8, 0xFF, 0xE0],
        };

        Assert.Equal("APIC", pic.Identifier);
    }

    [Fact]
    public void AttachedPictureFrame_V220_ImageFormatIsExactlyThreeBytes()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            ImageFormat = "JPG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = [0x01],
        };

        var data = pic.Data;
        // Layout: [encoding(1)] + [ImageFormat(3 bytes for v2.2)] + [PictureType(1)] + ...
        Assert.Equal((byte)'J', data[1]);
        Assert.Equal((byte)'P', data[2]);
        Assert.Equal((byte)'G', data[3]);
        Assert.Equal((byte)Id3v2AttachedPictureType.CoverFront, data[4]);
    }

    [Fact]
    public void AttachedPictureFrame_V220_TrimsImageFormatToThreeChars()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            ImageFormat = "image/jpeg",
        };

        // v2.2 spec: image format is a fixed 3-byte field; the setter must trim.
        Assert.Equal(3, pic.ImageFormat.Length);
    }

    [Fact]
    public void AttachedPictureFrame_V230_ImageFormatIsNulTerminatedMimeType()
    {
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v230)
        {
            ImageFormat = "image/jpeg",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = [0x01],
        };

        var data = pic.Data;
        // Layout: [encoding(1)] + [MIME(10 bytes "image/jpeg")] + [0x00] + [PictureType(1)] + ...
        var mime = Encoding.ASCII.GetString(data, 1, "image/jpeg".Length);
        Assert.Equal("image/jpeg", mime);
        Assert.Equal(0x00, data[1 + "image/jpeg".Length]);
        Assert.Equal((byte)Id3v2AttachedPictureType.CoverFront, data[1 + "image/jpeg".Length + 1]);
    }

    [Fact]
    public void AttachedPictureFrame_V220AndV230_ProduceDifferentSerializedData()
    {
        byte[] imageBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02];

        var v220 = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            ImageFormat = "JPG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = imageBytes,
        };

        var v230 = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v230)
        {
            ImageFormat = "image/jpeg",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = imageBytes,
        };

        Assert.NotEqual(v220.Data.Length, v230.Data.Length);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 7. Text encoding: UTF-16 with BOM accepted on v2.3
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TextFrame_V230_AcceptsUtf16LittleEndianEncoding_AndEmitsBom()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2")
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
        };
        frame.Values.Add("Hi");

        var data = frame.Data;
        // First byte is the encoding marker: 0x01 for UTF-16 LE with BOM.
        Assert.Equal(0x01, data[0]);
        // Bytes 1..2 are the UTF-16 LE BOM: FF FE.
        Assert.Equal(0xFF, data[1]);
        Assert.Equal(0xFE, data[2]);
    }

    [Fact]
    public void TextFrame_V220_AndV230_UseSameEncodingByteForDefault()
    {
        // v2.2 only supports encoding 0x00 (ISO-8859-1) and 0x01 (UCS-2 / UTF-16 LE).
        // We at least assert the default encoding byte is stable across versions.
        var v220 = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2");
        v220.Values.Add("Hi");
        var v230 = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2");
        v230.Values.Add("Hi");

        Assert.Equal(v220.Data[0], v230.Data[0]);
        Assert.Equal(0x00, v220.Data[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 8. Removed frames: the v2.3 changes doc explicitly deletes only the encrypted meta frame (CRM).
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void EncryptedMetaFrame_CRM_IsRejectedForV230Construction()
    {
        // v2.2/v2.2.1 only; v2.3 construction must throw InvalidVersionException.
        _ = new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v220);
        _ = new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v221);
        Assert.Throws<InvalidVersionException>(() => new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v230));
        Assert.Throws<InvalidVersionException>(() => new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v240));
    }

    [Fact]
    public void EncryptedMetaFrame_NotSupportedOnV230Versioning()
    {
        var v220Frame = new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v220);
        Assert.True(v220Frame.IsVersionSupported(Id3v2Version.Id3v220));
        Assert.True(v220Frame.IsVersionSupported(Id3v2Version.Id3v221));
        Assert.False(v220Frame.IsVersionSupported(Id3v2Version.Id3v230));
        Assert.False(v220Frame.IsVersionSupported(Id3v2Version.Id3v240));
    }

    [Fact]
    public void EncryptedMetaFrame_IsRemoved_WhenUpgradingTagFromV220ToV230()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var emf = new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v220)
        {
            OwnerIdentifier = "http://id3.org/dummy/crm.html",
            ContentExplanation = "test",
            EncryptedDataBlock = [0x01, 0x02, 0x03],
        };
        tag.SetFrame(emf);
        Assert.Single(tag.Frames);

        // Upgrading the tag version must drop frames that don't support the new version.
        tag.Version = Id3v2Version.Id3v230;
        Assert.DoesNotContain(tag.Frames, f => f is Id3v2EncryptedMetaFrame);
    }

    [Fact]
    public void CompressedDataMetaFrame_CDM_IsV221Only()
    {
        // CDM is defined for v2.2.1 experimental only; both v2.2.0 and v2.3+ reject it.
        var cdm = new Id3v2CompressedDataMetaFrame();
        Assert.True(cdm.IsVersionSupported(Id3v2Version.Id3v221));
        Assert.False(cdm.IsVersionSupported(Id3v2Version.Id3v220));
        Assert.False(cdm.IsVersionSupported(Id3v2Version.Id3v230));
        Assert.False(cdm.IsVersionSupported(Id3v2Version.Id3v240));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 9. Renamed frames: v2.2 3-char ids map to v2.3 4-char equivalents
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Id3v2TextFrameIdentifier.TrackTitle, "TT2", "TIT2")]
    [InlineData(Id3v2TextFrameIdentifier.Artist, "TP1", "TPE1")]
    [InlineData(Id3v2TextFrameIdentifier.ArtistExtra, "TP2", "TPE2")]
    [InlineData(Id3v2TextFrameIdentifier.ConductorName, "TP3", "TPE3")]
    [InlineData(Id3v2TextFrameIdentifier.ContentGroupDescription, "TT1", "TIT1")]
    [InlineData(Id3v2TextFrameIdentifier.ContentType, "TCO", "TCON")]
    [InlineData(Id3v2TextFrameIdentifier.AlbumTitle, "TAL", "TALB")]
    [InlineData(Id3v2TextFrameIdentifier.BeatsPerMinute, "TBP", "TBPM")]
    [InlineData(Id3v2TextFrameIdentifier.ComposerName, "TCM", "TCOM")]
    public void TextFrameIdentifier_MapsThreeCharToFourChar(Id3v2TextFrameIdentifier id, string v220Id, string v230Id)
    {
        Assert.Equal(v220Id, Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v220, id));
        Assert.Equal(v230Id, Id3v2TextFrame.GetIdentifier(Id3v2Version.Id3v230, id));
    }

    [Fact]
    public void RecommendedBufferSize_BUF_OnV220_RBUF_OnV230()
    {
        var v220 = new Id3v2RecommendedBufferSizeFrame(Id3v2Version.Id3v220);
        var v230 = new Id3v2RecommendedBufferSizeFrame(Id3v2Version.Id3v230);

        Assert.Equal("BUF", v220.Identifier);
        Assert.Equal("RBUF", v230.Identifier);
    }

    [Fact]
    public void RelativeVolumeAdjustment_RVA_OnV220_RVAD_OnV230()
    {
        var v220 = new Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version.Id3v220);
        var v230 = new Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version.Id3v230);

        Assert.Equal("RVA", v220.Identifier);
        Assert.Equal("RVAD", v230.Identifier);
    }

    [Fact]
    public void Equalisation_EQU_OnV220_EQUA_OnV230()
    {
        var v220 = new Id3v2EqualisationFrame(Id3v2Version.Id3v220);
        var v230 = new Id3v2EqualisationFrame(Id3v2Version.Id3v230);

        Assert.Equal("EQU", v220.Identifier);
        Assert.Equal("EQUA", v230.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // 10. Version upgrade: v2.2 -> v2.3 translates identifiers
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void VersionUpgrade_V220ToV230_DropsTextFramesWithV220OnlyIdentifiers()
    {
        // The Version setter iterates frames and removes any whose IsVersionSupported()
        // returns false for the new version. Id3v2TextFrame's per-version dictionary
        // does NOT list "TT2" under v2.3 (v2.3 uses "TIT2"), so the upgrade drops the
        // frame rather than translating the identifier.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var title = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2");
        title.Values.Add("Track");
        tag.SetFrame(title);

        tag.Version = Id3v2Version.Id3v230;

        Assert.Null(tag.GetTextFrame(Id3v2TextFrameIdentifier.TrackTitle));
        Assert.Empty(tag.Frames);
    }

    [Fact]
    public void VersionUpgrade_V220ToV230_TranslatesAttachedPictureIdentifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var pic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v220)
        {
            ImageFormat = "JPG",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "cover",
            PictureData = [0xFF, 0xD8, 0xFF, 0xE0],
        };
        tag.SetFrame(pic);

        tag.Version = Id3v2Version.Id3v230;

        var upgraded = tag.GetFrame<Id3v2AttachedPictureFrame>();
        Assert.NotNull(upgraded);
        Assert.Equal("APIC", upgraded!.Identifier);
        Assert.Equal(Id3v2Version.Id3v230, upgraded.Version);
    }

    [Fact]
    public void VersionUpgrade_V220ToV230_TranslatesRecommendedBufferSizeIdentifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var buf = new Id3v2RecommendedBufferSizeFrame(Id3v2Version.Id3v220);
        tag.SetFrame(buf);

        tag.Version = Id3v2Version.Id3v230;

        var upgraded = tag.GetFrame<Id3v2RecommendedBufferSizeFrame>();
        Assert.NotNull(upgraded);
        Assert.Equal("RBUF", upgraded!.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static Id3v2TextFrame CreateSimpleTitle(Id3v2Version version, string value)
    {
        var id = Id3v2TextFrame.GetIdentifier(version, Id3v2TextFrameIdentifier.TrackTitle)!;
        var frame = new Id3v2TextFrame(version, id);
        frame.Values.Add(value);
        return frame;
    }

    // Returns bytes after the 10-byte ID3v2 header.
    private static byte[] ExtractFramesRegion(byte[] tagBytes)
    {
        Assert.True(tagBytes.Length >= 10);
        Assert.Equal((byte)'I', tagBytes[0]);
        Assert.Equal((byte)'D', tagBytes[1]);
        Assert.Equal((byte)'3', tagBytes[2]);
        return [.. tagBytes.Skip(10)];
    }
}
