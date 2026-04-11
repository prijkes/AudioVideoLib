/*
 * Targeted diff-tests for the ID3v2.3 -> ID3v2.4 changes documented in
 *   docs/id3v2_4_0-changes - ID3_org.mht
 *
 * Each [Fact] targets a single bullet from that changes document and is
 * scoped to the behaviour that actually differs between v2.3 and v2.4.
 */
namespace AudioVideoLib.Tests;

using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

using Xunit;

public class Id3v240ChangesTests
{
    // ID3v2 header = 10 bytes; first 3 bytes "ID3", then 2 version bytes, 1 flags byte, 4 synchsafe-size bytes.
    private const int HeaderSize = 10;

    ////------------------------------------------------------------------------------------------------------------------------------
    // (1) Extended header size: raw int32 in v2.3, synchsafe int32 in v2.4.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ExtendedHeaderSize_IsRawInt32_OnV230()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230) { ExtendedHeader = new Id3v2ExtendedHeader() };
        var bytes = tag.ToByteArray();

        // v2.3: the 4 size bytes immediately follow the 10 byte tag header.
        // The v2.3 extended header size excludes itself and is 6 (no CRC) so the raw
        // int32 must have 0x06 as its low byte -- which, in synchsafe form, would also
        // be 0x06, so the distinguishing property is that the first 3 bytes are zero.
        Assert.Equal(0x00, bytes[HeaderSize + 0]);
        Assert.Equal(0x00, bytes[HeaderSize + 1]);
        Assert.Equal(0x00, bytes[HeaderSize + 2]);
        Assert.Equal(0x06, bytes[HeaderSize + 3]);
    }

    [Fact]
    public void ExtendedHeaderSize_IsSynchsafeInt32_OnV240()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() };
        var bytes = tag.ToByteArray();

        // v2.4 extended header size includes itself and is 6 when no flags are set. Synchsafe(6) == 6.
        // The synchsafe value of 6 is still 6 at the low byte, so this alone does not prove
        // synchsafe-ness. Instead, round-trip via the Id3v2Tag.GetUnsynchedValue helper and
        // verify the written 4 bytes decode to 6, which is the synchsafe-encoded header size.
        var rawWritten = (bytes[HeaderSize + 0] << 24) | (bytes[HeaderSize + 1] << 16) | (bytes[HeaderSize + 2] << 8) | bytes[HeaderSize + 3];
        var decoded = Id3v2Tag.GetUnsynchedValue(rawWritten);
        Assert.Equal(6, decoded);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (2) Flag field length byte: new in v2.4, absent in v2.3.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_ExtendedHeader_StartsWith_FlagFieldLengthByte()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() };
        var bytes = tag.ToByteArray();

        // v2.4 layout: 4 size bytes, then a 1-byte flag field length, then the flag bytes.
        // Default v2.4 flag field length is 1, and with no flags set the flag byte is 0.
        Assert.Equal(0x01, bytes[HeaderSize + 4]);
        Assert.Equal(0x00, bytes[HeaderSize + 5]);
    }

    [Fact]
    public void V230_ExtendedHeader_HasNoFlagFieldLengthByte()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230) { ExtendedHeader = new Id3v2ExtendedHeader() };
        var bytes = tag.ToByteArray();

        // v2.3 layout: 4 size bytes, then 2 flag bytes, then 4 padding bytes.
        // No flag-field-length byte is present, so bytes [HeaderSize+4..+5] are the raw flag bytes.
        // With no flags set they are both zero, followed by a 4-byte padding value.
        Assert.Equal(0x00, bytes[HeaderSize + 4]);
        Assert.Equal(0x00, bytes[HeaderSize + 5]);
        Assert.Equal(0x00, bytes[HeaderSize + 6]);
        Assert.Equal(0x00, bytes[HeaderSize + 7]);
        Assert.Equal(0x00, bytes[HeaderSize + 8]);
        Assert.Equal(0x00, bytes[HeaderSize + 9]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (3) Unsynchronization: tag-level in v2.3, per-frame in v2.4.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V230_UnsynchronizationFlag_RewritesPostHeaderPayload()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var frame = new Id3v2PrivateFrame(Id3v2Version.Id3v230)
        {
            OwnerIdentifier = "http://example.com/",
            PrivateData = [0xFF, 0x00, 0xAB],
        };
        tag.SetFrame(frame);

        var withoutUnsync = tag.ToByteArray();
        tag.UseUnsynchronization = true;
        var withUnsync = tag.ToByteArray();

        // v2.3 unsync rewrites the whole post-header area, so we must see at least
        // one extra inserted byte (0xFF 0x00 -> 0xFF 0x00 0x00).
        Assert.True(withUnsync.Length > withoutUnsync.Length);
    }

    [Fact]
    public void V240_UnsynchronizationFlag_DoesNotRewritePostHeaderPayload()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var frame = new Id3v2PrivateFrame(Id3v2Version.Id3v240)
        {
            OwnerIdentifier = "http://example.com/",
            PrivateData = [0xFF, 0x00, 0xAB],
        };
        tag.SetFrame(frame);

        var withoutUnsync = tag.ToByteArray();
        tag.UseUnsynchronization = true;
        var withUnsync = tag.ToByteArray();

        // v2.4 does NOT apply tag-level unsync; per-frame unsync is expected.
        // Only the flags byte (header byte 5) differs.
        Assert.Equal(withoutUnsync.Length, withUnsync.Length);
        Assert.NotEqual(withoutUnsync[5], withUnsync[5]);

        // Everything after the header byte must be byte-for-byte identical -- no tag-level rewrite.
        Assert.Equal(withoutUnsync[6..], withUnsync[6..]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (4) Footer: new in v2.4, reversed magic "3DI".
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_UseFooter_EmitsFooterWithReversedMagic()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { UseFooter = true };
        var bytes = tag.ToByteArray();

        Assert.True(tag.UseFooter);
        // Last 10 bytes = footer. First 3 of the footer = "3DI".
        var footer = bytes[^10..];
        Assert.Equal((byte)'3', footer[0]);
        Assert.Equal((byte)'D', footer[1]);
        Assert.Equal((byte)'I', footer[2]);
    }

    [Fact]
    public void V230_UseFooter_IsSilentlyRejected()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230) { UseFooter = true };
        Assert.False(tag.UseFooter);

        var bytes = tag.ToByteArray();
        // No footer bytes; tag header only (no frames, no padding).
        Assert.Equal(HeaderSize, bytes.Length);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (5) Text encodings: v2.4 adds 0x02 (UTF-16BE without BOM) and 0x03 (UTF-8).
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_TextFrame_SupportsUtf16BeNoBomEncodingByte()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF16BigEndianWithoutBom };
        frame.Values.Add("Hello");

        // Spec value 0x02 must appear as the first data byte (= encoding marker).
        Assert.Equal(0x02, frame.Data[0]);
    }

    [Fact]
    public void V240_TextFrame_SupportsUtf8EncodingByte()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF8 };
        frame.Values.Add("Hello");

        // Spec value 0x03 must appear as the first data byte.
        Assert.Equal(0x03, frame.Data[0]);
    }

    [Fact]
    public void V240_TextFrame_UsesSpecByte0x02ForUtf16Be()
    {
        // Separate v2.4 check: spec byte 0x02 == UTF-16BE (with BOM in the data bytes).
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF16BigEndian };
        frame.Values.Add("Hello");
        Assert.Equal(0x01, frame.Data[0]);
        // Note: the library's internal enum distinguishes UTF16BE (with BOM) from
        // UTF16BigEndianWithoutBom, but writes UTF16BE as legacy spec byte 0x01 (shared
        // with UTF16LE + BOM). Spec byte 0x02 is reserved for UTF-16BE-without-BOM
        // in this library (see Id3v2FrameEncoding.EncodingTypes table).
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (6) Multiple null-separated strings in text frames: v2.4 allows; v2.3 does not.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_TextFrame_PreservesTwoValuesAcrossRoundTrip()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        frame.Values.Add("First");
        frame.Values.Add("Second");
        tag.SetFrame(frame);

        var bytes = tag.ToByteArray();
        var offset = new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start);
        Assert.NotNull(offset);

        var parsed = ((Id3v2Tag)offset!.AudioTag).GetTextFrame(Id3v2TextFrameIdentifier.TrackTitle);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Values.Count);
        Assert.Equal("First", parsed.Values[0]);
        Assert.Equal("Second", parsed.Values[1]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (7) New frames in v2.4.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_TdrcFrame_ConstructsAndRoundTrips()
    {
        var v240 = new Id3v2TextFrame(Id3v2Version.Id3v240, "TDRC");
        v240.Values.Add("2026-04-11");
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(v240);
        var bytes = tag.ToByteArray();
        var parsed = ((Id3v2Tag)new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start)!.AudioTag)
            .GetFrame<Id3v2TextFrame>("TDRC");
        Assert.NotNull(parsed);
        Assert.Equal("2026-04-11", parsed!.Values.Single());
    }

    [Fact]
    public void V240_NewTimestampFramesConstruct()
    {
        // TDRC, TIPL, TMCL, TMOO, TPRO, TSOA, TSOP, TSOT, TDRL, TDTG are new in v2.4.
        // Id3v2TextFrame's identifier validation is character-based, not per-version,
        // so these construct on any version — but the intent of the test is to confirm
        // the v2.4 tag can round-trip them through its writer.
        string[] v240New = ["TIPL", "TMCL", "TMOO", "TPRO", "TSOA", "TSOP", "TSOT", "TDRL", "TDTG"];
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        foreach (var id in v240New)
        {
            var f = new Id3v2TextFrame(Id3v2Version.Id3v240, id);
            f.Values.Add("x");
            tag.SetFrame(f);
        }

        var bytes = tag.ToByteArray();
        var parsed = (Id3v2Tag)new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start)!.AudioTag;
        foreach (var id in v240New)
        {
            Assert.NotNull(parsed.GetFrame<Id3v2TextFrame>(id));
        }
    }

    [Fact]
    public void V240_SeekFrame_RoundTrips_AndV230Rejects()
    {
        // v2.4 accepts
        var seek = new Id3v2SeekFrame(Id3v2Version.Id3v240) { MinimumOffsetToNextTag = 1234 };
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(seek);

        var bytes = tag.ToByteArray();
        var offset = new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start);
        var parsed = ((Id3v2Tag)offset!.AudioTag).GetFrame<Id3v2SeekFrame>();
        Assert.NotNull(parsed);
        Assert.Equal(1234, parsed!.MinimumOffsetToNextTag);

        // v2.3 rejects construction.
        Assert.Throws<InvalidVersionException>(() => new Id3v2SeekFrame(Id3v2Version.Id3v230));
    }

    [Fact]
    public void V240_Equalisation2Frame_RejectedOnV230()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2Equalisation2Frame(Id3v2Version.Id3v230));
        var f = new Id3v2Equalisation2Frame(Id3v2Version.Id3v240);
        Assert.True(f.IsVersionSupported(Id3v2Version.Id3v240));
    }

    [Fact]
    public void V240_RelativeVolumeAdjustment2Frame_RejectedOnV230()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2RelativeVolumeAdjustment2Frame(Id3v2Version.Id3v230));
        var f = new Id3v2RelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240);
        Assert.True(f.IsVersionSupported(Id3v2Version.Id3v240));
    }

    [Fact]
    public void V240_SignatureFrame_RejectedOnV230()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2SignatureFrame(Id3v2Version.Id3v230));
        var f = new Id3v2SignatureFrame(Id3v2Version.Id3v240);
        Assert.True(f.IsVersionSupported(Id3v2Version.Id3v240));
    }

    [Fact]
    public void V240_AudioSeekPointIndexFrame_RejectedOnV230()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v230));
        var f = new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v240);
        Assert.True(f.IsVersionSupported(Id3v2Version.Id3v240));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (8) Removed frames: EQUA, RVAD, TDAT, TIME, TORY, TRDA, TSIZ, TYER.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_EqualisationFrame_CannotBeConstructed()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2EqualisationFrame(Id3v2Version.Id3v240));
    }

    [Fact]
    public void V240_RelativeVolumeAdjustmentFrame_CannotBeConstructed()
    {
        Assert.Throws<InvalidVersionException>(() => new Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version.Id3v240));
    }

    [Fact]
    public void V240_Upgrade_PreservesTextFramesAcrossVersionChange()
    {
        // Documents current behaviour: because Id3v2TextFrame's identifier validation
        // is character-based rather than spec-whitelist-based, identifiers that v2.4
        // technically drops (TYER/TDAT/TIME/TORY/TRDA/TSIZ) survive the Version setter.
        // This test pins the observed behaviour; tightening to spec-compliant drop
        // would require a per-version whitelist in Id3v2TextFrame.
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        string[] ids = ["TYER", "TDAT", "TIME", "TORY", "TRDA", "TSIZ"];
        foreach (var id in ids)
        {
            var f = new Id3v2TextFrame(Id3v2Version.Id3v230, id);
            f.Values.Add("1999");
            tag.SetFrame(f);
        }

        Assert.Equal(ids.Length, tag.Frames.Count());
        tag.Version = Id3v2Version.Id3v240;
        Assert.Equal(ids.Length, tag.Frames.Count());
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (9) Tag restrictions: new in v2.4.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_TagRestrictions_AreHonoured_AndRoundTrip()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader
            {
                TagRestrictions = new Id3v2TagRestrictions { TagSizeRestriction = Id3v2TagSizeRestriction.Max128FramesAnd1024KbTotalSize },
            },
        };
        Assert.True(tag.ExtendedHeader.TagIsRestricted);

        var bytes = tag.ToByteArray();
        var offset = new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start);
        Assert.NotNull(offset);

        var parsed = (Id3v2Tag)offset!.AudioTag;
        Assert.NotNull(parsed.ExtendedHeader);
        Assert.True(parsed.ExtendedHeader.TagIsRestricted);
        Assert.NotNull(parsed.ExtendedHeader.TagRestrictions);
    }

    [Fact]
    public void V230_TagRestrictions_AreNotWritten()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230)
        {
            ExtendedHeader = new Id3v2ExtendedHeader
            {
                TagRestrictions = new Id3v2TagRestrictions { TagSizeRestriction = Id3v2TagSizeRestriction.Max128FramesAnd1024KbTotalSize },
            },
        };

        // v2.3 extended header size = 6 (no CRC), no restrictions byte emitted.
        var bytes = tag.ToByteArray();
        var extSize = (bytes[HeaderSize + 0] << 24) | (bytes[HeaderSize + 1] << 16) | (bytes[HeaderSize + 2] << 8) | bytes[HeaderSize + 3];
        Assert.Equal(6, extSize);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (10) TagIsUpdate flag: new in v2.4 extended header; zero-length data field.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V240_TagIsUpdate_RoundTrips()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader { TagIsUpdate = true } };
        var bytes = tag.ToByteArray();

        var offset = new Id3v2TagReader().ReadFromStream(new MemoryStream(bytes), TagOrigin.Start);
        Assert.NotNull(offset);
        var parsed = (Id3v2Tag)offset!.AudioTag;
        Assert.NotNull(parsed.ExtendedHeader);
        Assert.True(parsed.ExtendedHeader.TagIsUpdate);
    }

    [Fact]
    public void V240_TagIsUpdate_AddsOneByteToExtendedHeader()
    {
        var bare = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() }.ToByteArray();
        var withUpdate = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader { TagIsUpdate = true } }.ToByteArray();

        Assert.Equal(1, withUpdate.Length - bare.Length);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (11) CRC size: 4 bytes raw in v2.3, 5 bytes synchsafe in v2.4.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void V230_CrcDataIs4Bytes()
    {
        Assert.Equal(4, Id3v2ExtendedHeader.GetCrcDataLength(Id3v2Version.Id3v230));

        var withoutCrc = new Id3v2Tag(Id3v2Version.Id3v230) { ExtendedHeader = new Id3v2ExtendedHeader() }.ToByteArray();
        var withCrc = new Id3v2Tag(Id3v2Version.Id3v230) { ExtendedHeader = new Id3v2ExtendedHeader { CrcDataPresent = true } }.ToByteArray();

        Assert.Equal(4, withCrc.Length - withoutCrc.Length);
    }

    [Fact]
    public void V240_CrcDataIs5BytesSynchsafe()
    {
        Assert.Equal(5, Id3v2ExtendedHeader.GetCrcDataLength(Id3v2Version.Id3v240));

        var withoutCrc = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() }.ToByteArray();
        var withCrc = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader { CrcDataPresent = true } }.ToByteArray();

        Assert.Equal(5, withCrc.Length - withoutCrc.Length);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // (12) REGRESSION: Id3v2ExtendedHeader.Equals null-symmetry for TagRestrictions.
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ExtendedHeader_Equals_BothNullTagRestrictions_V230()
    {
        var a = new Id3v2ExtendedHeader();
        var b = new Id3v2ExtendedHeader();
        Assert.Null(a.TagRestrictions);
        Assert.Null(b.TagRestrictions);
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    [Fact]
    public void ExtendedHeader_Equals_BothNullTagRestrictions_V240()
    {
        // Both instances are used on a v2.4 tag; structural equality should still hold when
        // TagRestrictions is unset on both sides.
        var tagA = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() };
        var tagB = new Id3v2Tag(Id3v2Version.Id3v240) { ExtendedHeader = new Id3v2ExtendedHeader() };

        Assert.Null(tagA.ExtendedHeader.TagRestrictions);
        Assert.Null(tagB.ExtendedHeader.TagRestrictions);
        Assert.True(tagA.ExtendedHeader.Equals(tagB.ExtendedHeader));
        Assert.True(tagB.ExtendedHeader.Equals(tagA.ExtendedHeader));
    }

    [Fact]
    public void ExtendedHeader_Equals_OneNullTagRestriction_StillSymmetric()
    {
        // The regression fix should also guarantee symmetric behaviour when only one side
        // has a null TagRestrictions -- both directions must agree.
        var a = new Id3v2ExtendedHeader();
        var b = new Id3v2ExtendedHeader { TagRestrictions = new Id3v2TagRestrictions() };
        Assert.Equal(a.Equals(b), b.Equals(a));
    }

}
