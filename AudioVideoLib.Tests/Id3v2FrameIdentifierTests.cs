namespace AudioVideoLib.Tests;

using System;

using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// D2 regression — every frame whose Identifier delegates to the factory must
/// return the canonical string at every supported version. The matrix below
/// covers all 21 migrated frames × all of their supported versions.
/// </summary>
public class Id3v2FrameIdentifierTests
{
    [Theory]
    // Id3v2AttachedPictureFrame — PIC / APIC
    [InlineData(typeof(Id3v2AttachedPictureFrame), Id3v2Version.Id3v220, "PIC")]
    [InlineData(typeof(Id3v2AttachedPictureFrame), Id3v2Version.Id3v221, "PIC")]
    [InlineData(typeof(Id3v2AttachedPictureFrame), Id3v2Version.Id3v230, "APIC")]
    [InlineData(typeof(Id3v2AttachedPictureFrame), Id3v2Version.Id3v240, "APIC")]
    // Id3v2AudioEncryptionFrame — CRA / AENC
    [InlineData(typeof(Id3v2AudioEncryptionFrame), Id3v2Version.Id3v220, "CRA")]
    [InlineData(typeof(Id3v2AudioEncryptionFrame), Id3v2Version.Id3v221, "CRA")]
    [InlineData(typeof(Id3v2AudioEncryptionFrame), Id3v2Version.Id3v230, "AENC")]
    [InlineData(typeof(Id3v2AudioEncryptionFrame), Id3v2Version.Id3v240, "AENC")]
    // Id3v2CommentFrame — COM / COMM
    [InlineData(typeof(Id3v2CommentFrame), Id3v2Version.Id3v220, "COM")]
    [InlineData(typeof(Id3v2CommentFrame), Id3v2Version.Id3v221, "COM")]
    [InlineData(typeof(Id3v2CommentFrame), Id3v2Version.Id3v230, "COMM")]
    [InlineData(typeof(Id3v2CommentFrame), Id3v2Version.Id3v240, "COMM")]
    // Id3v2EqualisationFrame — EQU / EQUA (v2.4 not supported)
    [InlineData(typeof(Id3v2EqualisationFrame), Id3v2Version.Id3v220, "EQU")]
    [InlineData(typeof(Id3v2EqualisationFrame), Id3v2Version.Id3v221, "EQU")]
    [InlineData(typeof(Id3v2EqualisationFrame), Id3v2Version.Id3v230, "EQUA")]
    // Id3v2EventTimingCodesFrame — ETC / ETCO
    [InlineData(typeof(Id3v2EventTimingCodesFrame), Id3v2Version.Id3v220, "ETC")]
    [InlineData(typeof(Id3v2EventTimingCodesFrame), Id3v2Version.Id3v221, "ETC")]
    [InlineData(typeof(Id3v2EventTimingCodesFrame), Id3v2Version.Id3v230, "ETCO")]
    [InlineData(typeof(Id3v2EventTimingCodesFrame), Id3v2Version.Id3v240, "ETCO")]
    // Id3v2GeneralEncapsulatedObjectFrame — GEO / GEOB
    [InlineData(typeof(Id3v2GeneralEncapsulatedObjectFrame), Id3v2Version.Id3v220, "GEO")]
    [InlineData(typeof(Id3v2GeneralEncapsulatedObjectFrame), Id3v2Version.Id3v221, "GEO")]
    [InlineData(typeof(Id3v2GeneralEncapsulatedObjectFrame), Id3v2Version.Id3v230, "GEOB")]
    [InlineData(typeof(Id3v2GeneralEncapsulatedObjectFrame), Id3v2Version.Id3v240, "GEOB")]
    // Id3v2InvolvedPeopleListFrame — IPL / IPLS (v2.4 not supported)
    [InlineData(typeof(Id3v2InvolvedPeopleListFrame), Id3v2Version.Id3v220, "IPL")]
    [InlineData(typeof(Id3v2InvolvedPeopleListFrame), Id3v2Version.Id3v221, "IPL")]
    [InlineData(typeof(Id3v2InvolvedPeopleListFrame), Id3v2Version.Id3v230, "IPLS")]
    // Id3v2MpegLocationLookupTableFrame — MLL / MLLT
    [InlineData(typeof(Id3v2MpegLocationLookupTableFrame), Id3v2Version.Id3v220, "MLL")]
    [InlineData(typeof(Id3v2MpegLocationLookupTableFrame), Id3v2Version.Id3v221, "MLL")]
    [InlineData(typeof(Id3v2MpegLocationLookupTableFrame), Id3v2Version.Id3v230, "MLLT")]
    [InlineData(typeof(Id3v2MpegLocationLookupTableFrame), Id3v2Version.Id3v240, "MLLT")]
    // Id3v2MusicCdIdentifierFrame — MCI / MCDI
    [InlineData(typeof(Id3v2MusicCdIdentifierFrame), Id3v2Version.Id3v220, "MCI")]
    [InlineData(typeof(Id3v2MusicCdIdentifierFrame), Id3v2Version.Id3v221, "MCI")]
    [InlineData(typeof(Id3v2MusicCdIdentifierFrame), Id3v2Version.Id3v230, "MCDI")]
    [InlineData(typeof(Id3v2MusicCdIdentifierFrame), Id3v2Version.Id3v240, "MCDI")]
    // Id3v2PlayCounterFrame — CNT / PCNT
    [InlineData(typeof(Id3v2PlayCounterFrame), Id3v2Version.Id3v220, "CNT")]
    [InlineData(typeof(Id3v2PlayCounterFrame), Id3v2Version.Id3v221, "CNT")]
    [InlineData(typeof(Id3v2PlayCounterFrame), Id3v2Version.Id3v230, "PCNT")]
    [InlineData(typeof(Id3v2PlayCounterFrame), Id3v2Version.Id3v240, "PCNT")]
    // Id3v2PopularimeterFrame — POP / POPM
    [InlineData(typeof(Id3v2PopularimeterFrame), Id3v2Version.Id3v220, "POP")]
    [InlineData(typeof(Id3v2PopularimeterFrame), Id3v2Version.Id3v221, "POP")]
    [InlineData(typeof(Id3v2PopularimeterFrame), Id3v2Version.Id3v230, "POPM")]
    [InlineData(typeof(Id3v2PopularimeterFrame), Id3v2Version.Id3v240, "POPM")]
    // Id3v2RecommendedBufferSizeFrame — BUF / RBUF
    [InlineData(typeof(Id3v2RecommendedBufferSizeFrame), Id3v2Version.Id3v220, "BUF")]
    [InlineData(typeof(Id3v2RecommendedBufferSizeFrame), Id3v2Version.Id3v221, "BUF")]
    [InlineData(typeof(Id3v2RecommendedBufferSizeFrame), Id3v2Version.Id3v230, "RBUF")]
    [InlineData(typeof(Id3v2RecommendedBufferSizeFrame), Id3v2Version.Id3v240, "RBUF")]
    // Id3v2RelativeVolumeAdjustmentFrame — RVA / RVAD (v2.4 not supported)
    [InlineData(typeof(Id3v2RelativeVolumeAdjustmentFrame), Id3v2Version.Id3v220, "RVA")]
    [InlineData(typeof(Id3v2RelativeVolumeAdjustmentFrame), Id3v2Version.Id3v221, "RVA")]
    [InlineData(typeof(Id3v2RelativeVolumeAdjustmentFrame), Id3v2Version.Id3v230, "RVAD")]
    // Id3v2ReverbFrame — REV / REVB
    [InlineData(typeof(Id3v2ReverbFrame), Id3v2Version.Id3v220, "REV")]
    [InlineData(typeof(Id3v2ReverbFrame), Id3v2Version.Id3v221, "REV")]
    [InlineData(typeof(Id3v2ReverbFrame), Id3v2Version.Id3v230, "REVB")]
    [InlineData(typeof(Id3v2ReverbFrame), Id3v2Version.Id3v240, "REVB")]
    // Id3v2SyncedTempoCodesFrame — STC / SYTC
    [InlineData(typeof(Id3v2SyncedTempoCodesFrame), Id3v2Version.Id3v220, "STC")]
    [InlineData(typeof(Id3v2SyncedTempoCodesFrame), Id3v2Version.Id3v221, "STC")]
    [InlineData(typeof(Id3v2SyncedTempoCodesFrame), Id3v2Version.Id3v230, "SYTC")]
    [InlineData(typeof(Id3v2SyncedTempoCodesFrame), Id3v2Version.Id3v240, "SYTC")]
    // Id3v2SynchronizedLyricsFrame — SLT / SYLT
    [InlineData(typeof(Id3v2SynchronizedLyricsFrame), Id3v2Version.Id3v220, "SLT")]
    [InlineData(typeof(Id3v2SynchronizedLyricsFrame), Id3v2Version.Id3v221, "SLT")]
    [InlineData(typeof(Id3v2SynchronizedLyricsFrame), Id3v2Version.Id3v230, "SYLT")]
    [InlineData(typeof(Id3v2SynchronizedLyricsFrame), Id3v2Version.Id3v240, "SYLT")]
    // Id3v2UniqueFileIdentifierFrame — UFI / UFID
    [InlineData(typeof(Id3v2UniqueFileIdentifierFrame), Id3v2Version.Id3v220, "UFI")]
    [InlineData(typeof(Id3v2UniqueFileIdentifierFrame), Id3v2Version.Id3v221, "UFI")]
    [InlineData(typeof(Id3v2UniqueFileIdentifierFrame), Id3v2Version.Id3v230, "UFID")]
    [InlineData(typeof(Id3v2UniqueFileIdentifierFrame), Id3v2Version.Id3v240, "UFID")]
    // Id3v2UnsynchronizedLyricsFrame — ULT / USLT
    [InlineData(typeof(Id3v2UnsynchronizedLyricsFrame), Id3v2Version.Id3v220, "ULT")]
    [InlineData(typeof(Id3v2UnsynchronizedLyricsFrame), Id3v2Version.Id3v221, "ULT")]
    [InlineData(typeof(Id3v2UnsynchronizedLyricsFrame), Id3v2Version.Id3v230, "USLT")]
    [InlineData(typeof(Id3v2UnsynchronizedLyricsFrame), Id3v2Version.Id3v240, "USLT")]
    // Id3v2UserDefinedTextInformationFrame — TXX / TXXX
    [InlineData(typeof(Id3v2UserDefinedTextInformationFrame), Id3v2Version.Id3v220, "TXX")]
    [InlineData(typeof(Id3v2UserDefinedTextInformationFrame), Id3v2Version.Id3v221, "TXX")]
    [InlineData(typeof(Id3v2UserDefinedTextInformationFrame), Id3v2Version.Id3v230, "TXXX")]
    [InlineData(typeof(Id3v2UserDefinedTextInformationFrame), Id3v2Version.Id3v240, "TXXX")]
    // Id3v2UserDefinedUrlLinkFrame — WXX / WXXX
    [InlineData(typeof(Id3v2UserDefinedUrlLinkFrame), Id3v2Version.Id3v220, "WXX")]
    [InlineData(typeof(Id3v2UserDefinedUrlLinkFrame), Id3v2Version.Id3v221, "WXX")]
    [InlineData(typeof(Id3v2UserDefinedUrlLinkFrame), Id3v2Version.Id3v230, "WXXX")]
    [InlineData(typeof(Id3v2UserDefinedUrlLinkFrame), Id3v2Version.Id3v240, "WXXX")]
    // Id3v2LinkedInformationFrame — LNK / LINK (ctor takes a frame-identifier arg)
    [InlineData(typeof(Id3v2LinkedInformationFrame), Id3v2Version.Id3v220, "LNK")]
    [InlineData(typeof(Id3v2LinkedInformationFrame), Id3v2Version.Id3v221, "LNK")]
    [InlineData(typeof(Id3v2LinkedInformationFrame), Id3v2Version.Id3v230, "LINK")]
    [InlineData(typeof(Id3v2LinkedInformationFrame), Id3v2Version.Id3v240, "LINK")]
    public void Identifier_MatchesFactoryAtVersion(Type type, Id3v2Version version, string expected)
    {
        // LinkedInformationFrame's only ctor takes (Id3v2Version, string).
        var frame = type == typeof(Id3v2LinkedInformationFrame)
            ? (Id3v2Frame)Activator.CreateInstance(type, version, "TIT2")!
            : (Id3v2Frame)Activator.CreateInstance(type, version)!;

        Assert.Equal(expected, frame.Identifier);
    }
}
