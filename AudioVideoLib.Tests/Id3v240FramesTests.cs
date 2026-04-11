/*
 * Date: 2026-04-11
 * Sources used:
 *  docs/id3v2_4_0-frames - ID3_org.mht
 */
namespace AudioVideoLib.Tests;

using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// Tests covering ID3v2.4.0 frame encoding, decoding, round-trip and flag handling.
/// Tests drive end-to-end via <see cref="Id3v2Tag.ToByteArray"/> followed by
/// <see cref="Id3v2TagReader.ReadFromStream"/> so that the serializer, header, frame
/// identifier dispatch and factory-based parsing are all exercised together.
/// </summary>
public class Id3v240FramesTests
{
    private const Id3v2Version V240 = Id3v2Version.Id3v240;

    ////------------------------------------------------------------------------------------------------------------------------------
    //// Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static Id3v2Tag RoundTrip(Id3v2Tag tag)
    {
        var bytes = tag.ToByteArray();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > Id3v2Tag.HeaderSize, "serialized tag must contain at least the header");

        var reader = new Id3v2TagReader();
        using var stream = new MemoryStream(bytes);
        var offset = reader.ReadFromStream(stream, TagOrigin.Start);
        Assert.NotNull(offset);
        var roundTripped = Assert.IsType<Id3v2Tag>(offset!.AudioTag);
        Assert.Equal(V240, roundTripped.Version);
        return roundTripped;
    }

    private static Id3v2TextFrame MakeTextFrame(string identifier, Id3v2FrameEncodingType encoding, params string[] values)
    {
        var frame = new Id3v2TextFrame(V240, identifier) { TextEncoding = encoding };
        foreach (var value in values)
        {
            frame.Values.Add(value);
        }
        return frame;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 1. Text frames (T???)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Id3v2FrameEncodingType.Default)]
    [InlineData(Id3v2FrameEncodingType.UTF16LittleEndian)]
    [InlineData(Id3v2FrameEncodingType.UTF16BigEndianWithoutBom)]
    [InlineData(Id3v2FrameEncodingType.UTF8)]
    public void TextFrame_Tit2_RoundTrips_AllFourEncodings(Id3v2FrameEncodingType encoding)
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TIT2", encoding, "Hello World Title"));

        var parsed = RoundTrip(tag);

        var tit2 = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(tit2);
        Assert.Equal(encoding, tit2!.TextEncoding);
        Assert.Equal("Hello World Title", tit2.Values.Single());
    }

    [Fact]
    public void TextFrame_Tpe1_MultipleNullSeparatedValues_V240_Preserved()
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TPE1", Id3v2FrameEncodingType.UTF8, "Artist A", "Artist B", "Artist C"));

        var parsed = RoundTrip(tag);

        var tpe1 = parsed.GetFrame<Id3v2TextFrame>("TPE1");
        Assert.NotNull(tpe1);
        Assert.Equal(new[] { "Artist A", "Artist B", "Artist C" }, tpe1!.Values.ToArray());
    }

    [Fact]
    public void TextFrame_Talb_RoundTrips()
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TALB", Id3v2FrameEncodingType.UTF16LittleEndian, "An Album \u00E9\u00F4"));

        var parsed = RoundTrip(tag);

        var talb = parsed.GetFrame<Id3v2TextFrame>("TALB");
        Assert.NotNull(talb);
        Assert.Equal("An Album \u00E9\u00F4", talb!.Values.Single());
    }

    [Fact]
    public void TextFrame_Tdrc_ReplacesTyerInV240()
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TDRC", Id3v2FrameEncodingType.Default, "2026-04-11T12:00"));

        var parsed = RoundTrip(tag);

        var tdrc = parsed.GetFrame<Id3v2TextFrame>("TDRC");
        Assert.NotNull(tdrc);
        Assert.Equal("2026-04-11T12:00", tdrc!.Values.Single());
    }

    [Fact]
    public void TextFrame_Tcon_Tlan_Trck_RoundTrip()
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TCON", Id3v2FrameEncodingType.UTF8, "Progressive Rock"));
        tag.SetFrame(MakeTextFrame("TLAN", Id3v2FrameEncodingType.Default, "eng"));
        tag.SetFrame(MakeTextFrame("TRCK", Id3v2FrameEncodingType.Default, "3/12"));

        var parsed = RoundTrip(tag);

        Assert.Equal("Progressive Rock", parsed.GetFrame<Id3v2TextFrame>("TCON")!.Values.Single());
        Assert.Equal("eng", parsed.GetFrame<Id3v2TextFrame>("TLAN")!.Values.Single());
        Assert.Equal("3/12", parsed.GetFrame<Id3v2TextFrame>("TRCK")!.Values.Single());
    }

    [Fact]
    public void TextFrame_EmptyValue_IsHandledGracefully()
    {
        // An empty-string text frame must not crash the serializer. The emitted
        // frame carries only the encoding byte, and the parser reconstructs it with
        // an empty Values collection.
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(MakeTextFrame("TIT2", Id3v2FrameEncodingType.UTF8, string.Empty));

        var bytes = tag.ToByteArray();
        Assert.NotNull(bytes);

        var parsed = RoundTrip(tag);
        var tit2 = parsed.GetFrame<Id3v2TextFrame>("TIT2");
        Assert.NotNull(tit2);
        Assert.Empty(tit2!.Values);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 2. URL frames (W???)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("WCOM", "http://example.com/commercial")]
    [InlineData("WCOP", "http://example.com/copyright")]
    [InlineData("WOAF", "http://example.com/audiofile")]
    [InlineData("WOAR", "http://example.com/artist")]
    [InlineData("WOAS", "http://example.com/source")]
    [InlineData("WPAY", "http://example.com/pay")]
    [InlineData("WPUB", "http://example.com/publisher")]
    public void UrlLinkFrame_RoundTrip(string identifier, string url)
    {
        var tag = new Id3v2Tag(V240);
        var frame = new Id3v2UrlLinkFrame(V240, identifier) { Url = url };
        tag.SetFrame(frame);

        var parsed = RoundTrip(tag);

        var parsedFrame = parsed.GetFrame<Id3v2UrlLinkFrame>(identifier);
        Assert.NotNull(parsedFrame);
        Assert.Equal(url, parsedFrame!.Url);
    }

    [Fact]
    public void UrlLinkFrame_Wxxx_UserDefined_RoundTripsDescriptionAndUrl()
    {
        var tag = new Id3v2Tag(V240);
        var wxxx = new Id3v2UserDefinedUrlLinkFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            Description = "Homepage",
            Url = "http://example.com/homepage"
        };
        tag.SetFrame(wxxx);

        var parsed = RoundTrip(tag);

        var parsedWxxx = parsed.GetFrame<Id3v2UserDefinedUrlLinkFrame>();
        Assert.NotNull(parsedWxxx);
        Assert.Equal("Homepage", parsedWxxx!.Description);
        Assert.Equal("http://example.com/homepage", parsedWxxx.Url);
        Assert.Equal(Id3v2FrameEncodingType.UTF8, parsedWxxx.TextEncoding);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 3. APIC - attached picture
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Apic_AttachedPicture_RoundTripsAllFields()
    {
        var pictureData = new byte[100];
        for (var i = 0; i < pictureData.Length; i++)
        {
            pictureData[i] = (byte)((i * 7) & 0xFF);
        }

        var tag = new Id3v2Tag(V240);
        var apic = new Id3v2AttachedPictureFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            ImageFormat = "image/png",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "Front Cover",
            PictureData = pictureData
        };
        tag.SetFrame(apic);

        var parsed = RoundTrip(tag);

        var parsedApic = parsed.GetFrame<Id3v2AttachedPictureFrame>();
        Assert.NotNull(parsedApic);
        Assert.Equal("APIC", parsedApic!.Identifier);
        Assert.Equal("image/png", parsedApic.ImageFormat);
        Assert.Equal(Id3v2AttachedPictureType.CoverFront, parsedApic.PictureType);
        Assert.Equal("Front Cover", parsedApic.Description);
        Assert.Equal(pictureData, parsedApic.PictureData);
    }

    [Fact]
    public void Apic_PictureType_EnumRoundTripsNonDefault()
    {
        var tag = new Id3v2Tag(V240);
        var apic = new Id3v2AttachedPictureFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            ImageFormat = "image/jpeg",
            PictureType = Id3v2AttachedPictureType.ArtistPerformer,
            Description = "Artist Photo",
            PictureData = [0xDE, 0xAD, 0xBE, 0xEF]
        };
        tag.SetFrame(apic);

        var parsed = RoundTrip(tag);

        var parsedApic = parsed.GetFrame<Id3v2AttachedPictureFrame>();
        Assert.NotNull(parsedApic);
        Assert.Equal(Id3v2AttachedPictureType.ArtistPerformer, parsedApic!.PictureType);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 4. COMM - comments
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Id3v2FrameEncodingType.Default)]
    [InlineData(Id3v2FrameEncodingType.UTF8)]
    public void Comm_CommentFrame_RoundTrips(Id3v2FrameEncodingType encoding)
    {
        var tag = new Id3v2Tag(V240);
        var comm = new Id3v2CommentFrame(V240)
        {
            TextEncoding = encoding,
            Language = "eng",
            ShortContentDescription = "desc",
            Text = "This is a comment"
        };
        tag.SetFrame(comm);

        var parsed = RoundTrip(tag);

        var parsedComm = parsed.GetFrame<Id3v2CommentFrame>();
        Assert.NotNull(parsedComm);
        Assert.Equal(encoding, parsedComm!.TextEncoding);
        Assert.Equal("eng", parsedComm.Language);
        Assert.Equal("desc", parsedComm.ShortContentDescription);
        Assert.Equal("This is a comment", parsedComm.Text);
        Assert.Equal("COMM", parsedComm.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 5. USLT - unsynchronized lyrics
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(Id3v2FrameEncodingType.Default)]
    [InlineData(Id3v2FrameEncodingType.UTF8)]
    public void Uslt_UnsynchronizedLyricsFrame_RoundTrips(Id3v2FrameEncodingType encoding)
    {
        var tag = new Id3v2Tag(V240);
        var uslt = new Id3v2UnsynchronizedLyricsFrame(V240)
        {
            TextEncoding = encoding,
            Language = "eng",
            ContentDescriptor = "chorus",
            // Spec: Default (ISO-8859-1) allows only '\n' as newline — '\r' is rejected.
            Lyrics = "Line 1\nLine 2"
        };
        tag.SetFrame(uslt);

        var parsed = RoundTrip(tag);

        var parsedUslt = parsed.GetFrame<Id3v2UnsynchronizedLyricsFrame>();
        Assert.NotNull(parsedUslt);
        Assert.Equal(encoding, parsedUslt!.TextEncoding);
        Assert.Equal("eng", parsedUslt.Language);
        Assert.Equal("chorus", parsedUslt.ContentDescriptor);
        Assert.Equal("Line 1\nLine 2", parsedUslt.Lyrics);
        Assert.Equal("USLT", parsedUslt.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 6. UFID - unique file identifier
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Ufid_UniqueFileIdentifierFrame_RoundTrips()
    {
        byte[] identifierData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        var tag = new Id3v2Tag(V240);
        var ufid = new Id3v2UniqueFileIdentifierFrame(V240)
        {
            OwnerIdentifier = "http://musicbrainz.org",
            IdentifierData = identifierData
        };
        tag.SetFrame(ufid);

        var parsed = RoundTrip(tag);

        var parsedUfid = parsed.GetFrame<Id3v2UniqueFileIdentifierFrame>();
        Assert.NotNull(parsedUfid);
        Assert.Equal("http://musicbrainz.org", parsedUfid!.OwnerIdentifier);
        Assert.Equal(identifierData, parsedUfid.IdentifierData);
        Assert.Equal("UFID", parsedUfid.Identifier);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 7. USER - terms of use
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void User_TermsOfUseFrame_RoundTripsAllFields()
    {
        var tag = new Id3v2Tag(V240);
        var user = new Id3v2TermsOfUseFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            Language = "eng",
            Text = "All rights reserved."
        };
        tag.SetFrame(user);

        var parsed = RoundTrip(tag);

        var parsedUser = parsed.GetFrame<Id3v2TermsOfUseFrame>();
        Assert.NotNull(parsedUser);
        Assert.Equal("USER", parsedUser!.Identifier);
        Assert.Equal(Id3v2FrameEncodingType.UTF8, parsedUser.TextEncoding);
        Assert.Equal("eng", parsedUser.Language);
        Assert.Equal("All rights reserved.", parsedUser.Text);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 8. GRID - group identification
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Grid_GroupIdentificationRegistrationFrame_RoundTrips()
    {
        byte[] dependentData = [0xAA, 0xBB, 0xCC, 0xDD];
        var tag = new Id3v2Tag(V240);
        var grid = new Id3v2GroupIdentificationRegistrationFrame(V240)
        {
            OwnerIdentifier = "http://example.com/group",
            GroupSymbol = 0x80,
            GroupDependentData = dependentData
        };
        tag.SetFrame(grid);

        var parsed = RoundTrip(tag);

        var parsedGrid = parsed.GetFrame<Id3v2GroupIdentificationRegistrationFrame>();
        Assert.NotNull(parsedGrid);
        Assert.Equal("GRID", parsedGrid!.Identifier);
        Assert.Equal("http://example.com/group", parsedGrid.OwnerIdentifier);
        Assert.Equal((byte)0x80, parsedGrid.GroupSymbol);
        Assert.Equal(dependentData, parsedGrid.GroupDependentData);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 9. OWNE - ownership
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Owne_OwnershipFrame_RoundTripsAllFields()
    {
        var tag = new Id3v2Tag(V240);
        var owne = new Id3v2OwnershipFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            PricePaid = "USD9.99",
            DateOfPurchase = "20260411",
            Seller = "ACME Records"
        };
        tag.SetFrame(owne);

        var parsed = RoundTrip(tag);

        var parsedOwne = parsed.GetFrame<Id3v2OwnershipFrame>();
        Assert.NotNull(parsedOwne);
        Assert.Equal("OWNE", parsedOwne!.Identifier);
        Assert.Equal("USD9.99", parsedOwne.PricePaid);
        Assert.Equal("20260411", parsedOwne.DateOfPurchase);
        Assert.Equal("ACME Records", parsedOwne.Seller);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 10. PRIV - private frame
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Priv_PrivateFrame_RoundTripsArbitraryBinaryPayload()
    {
        byte[] payload = [0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD, 0x7F, 0x80];
        var tag = new Id3v2Tag(V240);
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/vendor",
            PrivateData = payload
        };
        tag.SetFrame(priv);

        var parsed = RoundTrip(tag);

        var parsedPriv = parsed.GetFrame<Id3v2PrivateFrame>();
        Assert.NotNull(parsedPriv);
        Assert.Equal("PRIV", parsedPriv!.Identifier);
        Assert.Equal("http://example.com/vendor", parsedPriv.OwnerIdentifier);
        Assert.Equal(payload, parsedPriv.PrivateData);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 11. TXXX - user-defined text
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Txxx_UserDefinedTextFrame_MultipleInstancesDistinguishedByDescription()
    {
        var tag = new Id3v2Tag(V240);
        tag.SetFrame(new Id3v2UserDefinedTextInformationFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            Description = "replaygain_track_gain",
            Value = "-6.5 dB"
        });
        tag.SetFrame(new Id3v2UserDefinedTextInformationFrame(V240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            Description = "replaygain_track_peak",
            Value = "0.987654"
        });

        var parsed = RoundTrip(tag);

        var txxxFrames = parsed.GetFrames<Id3v2UserDefinedTextInformationFrame>().ToList();
        Assert.Equal(2, txxxFrames.Count);

        var gain = txxxFrames.Single(f => f.Description == "replaygain_track_gain");
        var peak = txxxFrames.Single(f => f.Description == "replaygain_track_peak");
        Assert.Equal("-6.5 dB", gain.Value);
        Assert.Equal("0.987654", peak.Value);
        Assert.All(txxxFrames, f => Assert.Equal("TXXX", f.Identifier));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 12. Frame flags v2.4
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FrameFlags_V240_UnsynchronizationFlag_IsAppliedBySerializer()
    {
        // Build payload guaranteed to contain a 0xFF 0x?? pattern that will
        // trigger unsynchronization insertion (spec section 6.1 / 4.1.2).
        var tag = new Id3v2Tag(V240);
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/unsync",
            PrivateData = [0xFF, 0xE0, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0xF0],
            UseUnsynchronization = true
        };
        tag.SetFrame(priv);

        var bytes = tag.ToByteArray();

        // Verify: no 0xFF byte in the serialized output is followed directly by
        // a byte >= 0xE0 (the exception is if the next byte is 0x00, which is
        // the unsynchronization marker itself).
        for (var i = 0; i < bytes.Length - 1; i++)
        {
            if (bytes[i] != 0xFF)
            {
                continue;
            }

            var next = bytes[i + 1];
            Assert.False(next is >= 0xE0 and not 0x00, $"byte at offset {i} is not unsynchronized (0xFF followed by 0x{next:X2})");
        }

        var parsed = RoundTrip(tag);
        var parsedPriv = parsed.GetFrame<Id3v2PrivateFrame>();
        Assert.NotNull(parsedPriv);
        Assert.Equal(priv.PrivateData, parsedPriv!.PrivateData);
        Assert.True(parsedPriv.UseUnsynchronization, "unsync flag should be preserved through round-trip");
    }

    [Fact]
    public void FrameFlags_V240_DataLengthIndicator_IsAppliedBySerializer()
    {
        var tag = new Id3v2Tag(V240);
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/dli",
            PrivateData = [0x11, 0x22, 0x33, 0x44],
            UseDataLengthIndicator = true
        };
        tag.SetFrame(priv);

        var parsed = RoundTrip(tag);
        var parsedPriv = parsed.GetFrame<Id3v2PrivateFrame>();
        Assert.NotNull(parsedPriv);
        Assert.True(parsedPriv!.UseDataLengthIndicator);
        Assert.Equal(priv.PrivateData, parsedPriv.PrivateData);
    }

    [Fact]
    public void FrameFlags_V240_GroupingIdentity_IsPreserved()
    {
        var tag = new Id3v2Tag(V240);
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/grp",
            PrivateData = [0xAA, 0xBB],
            UseGroupingIdentity = true,
            GroupIdentifier = 0x42
        };
        tag.SetFrame(priv);

        var parsed = RoundTrip(tag);
        var parsedPriv = parsed.GetFrame<Id3v2PrivateFrame>();
        Assert.NotNull(parsedPriv);
        Assert.True(parsedPriv!.UseGroupingIdentity);
        Assert.Equal((byte)0x42, parsedPriv.GroupIdentifier);
    }

    [Fact]
    public void FrameFlags_V240_EncryptionFlag_IsPreserved()
    {
        var tag = new Id3v2Tag(V240);
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/enc",
            PrivateData = [0x01, 0x02, 0x03],
            UseEncryption = true,
            EncryptionType = 0x01
        };
        tag.SetFrame(priv);

        // Without a Cryptor configured the serializer cannot actually encrypt,
        // so the data round-trips as-is but the flag and encryption type are preserved.
        var parsed = RoundTrip(tag);
        var parsedPriv = parsed.GetFrame<Id3v2PrivateFrame>();
        Assert.NotNull(parsedPriv);
        Assert.True(parsedPriv!.UseEncryption);
        Assert.Equal((byte)0x01, parsedPriv.EncryptionType);
    }

    [Fact]
    public void FrameFlags_V240_CompressionFlag_ForcesDataLengthIndicator()
    {
        var priv = new Id3v2PrivateFrame(V240)
        {
            OwnerIdentifier = "http://example.com/comp",
            PrivateData = [0x01],
            UseCompression = true
        };

        // Setting UseCompression on a v2.4 frame must auto-enable UseDataLengthIndicator.
        Assert.True(priv.UseCompression);
        Assert.True(priv.UseDataLengthIndicator);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 13. Unknown frame identifier
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnknownFrameIdentifier_DocumentsCurrentBehavior()
    {
        // Build a raw v2.4 tag containing a single frame with identifier "XXXX".
        // The identifier starts with 'X' so it is not a text or URL frame and
        // should fall through to the base Id3v2Frame factory branch.
        var payload = Encoding.ASCII.GetBytes("arbitrary xxxx data");

        using var body = new StreamBuffer();
        body.WriteString("XXXX", Encoding.ASCII);
        body.WriteBigEndianInt32(Id3v2Tag.GetSynchsafeValue(payload.Length));
        body.WriteBigEndianInt16(0);
        body.Write(payload);
        var bodyBytes = body.ToByteArray();

        using var full = new StreamBuffer();
        full.WriteString("ID3", Encoding.ASCII);
        full.WriteByte(0x04);
        full.WriteByte(0x00);
        full.WriteByte(0x00);
        full.WriteBigEndianInt32(Id3v2Tag.GetSynchsafeValue(bodyBytes.Length));
        full.Write(bodyBytes);

        var reader = new Id3v2TagReader();
        using var stream = new MemoryStream(full.ToByteArray());
        var offset = reader.ReadFromStream(stream, TagOrigin.Start);
        Assert.NotNull(offset);

        var tag = Assert.IsType<Id3v2Tag>(offset!.AudioTag);

        // Observed behavior: the unknown frame is preserved as a base Id3v2Frame
        // instance carrying the unchanged identifier. If that behavior ever
        // changes to silent drop, this assertion will flag it.
        var unknown = tag.Frames.FirstOrDefault(f => f.Identifier == "XXXX");
        Assert.NotNull(unknown);
        Assert.Equal("XXXX", unknown!.Identifier);
        Assert.Equal(V240, unknown.Version);
        Assert.Equal(payload, unknown.Data);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// 14. Property accessors
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void PropertyAccessor_TrackTitle_MapsToTit2Frame()
    {
        var tag = new Id3v2Tag(V240)
        {
            TrackTitle = MakeTextFrame("TIT2", Id3v2FrameEncodingType.UTF8, "Song via Property")
        };

        Assert.Contains(tag.Frames, f => f.Identifier == "TIT2");
        Assert.Equal("Song via Property", tag.GetTextFrame(Id3v2TextFrameIdentifier.TrackTitle)!.Values.Single());

        var parsed = RoundTrip(tag);
        Assert.Equal("Song via Property", parsed.GetTextFrame(Id3v2TextFrameIdentifier.TrackTitle)!.Values.Single());
    }

    [Fact]
    public void PropertyAccessor_CopyrightInformation_MapsToWcopFrame()
    {
        var tag = new Id3v2Tag(V240)
        {
            CopyrightInformation = new Id3v2UrlLinkFrame(V240, "WCOP") { Url = "http://example.com/copyright" }
        };

        Assert.Contains(tag.Frames, f => f.Identifier == "WCOP");
        var parsed = RoundTrip(tag);
        var wcop = parsed.GetUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.CopyrightInformation);
        Assert.NotNull(wcop);
        Assert.Equal("http://example.com/copyright", wcop!.Url);
    }
}
