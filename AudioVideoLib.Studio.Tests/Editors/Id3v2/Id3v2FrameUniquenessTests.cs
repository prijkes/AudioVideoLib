namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using Xunit;

public class Id3v2FrameUniquenessTests
{
    [Theory]
    [InlineData("TIT2")]
    [InlineData("TPE1")]
    [InlineData("TPE2")]
    [InlineData("TALB")]
    [InlineData("TRCK")]
    [InlineData("TT2")]    // v2.2
    [InlineData("TP1")]    // v2.2
    public void TextFrameIdentifiers_AreUnique(string identifier)
    {
        Assert.True(Id3v2FrameUniqueness.IsUniqueTextOrUrlIdentifier(identifier));
    }

    [Theory]
    [InlineData("WCOM")]
    [InlineData("WCOP")]
    [InlineData("WOAF")]
    [InlineData("WOAS")]
    [InlineData("WORS")]
    [InlineData("WPAY")]
    [InlineData("WPUB")]
    [InlineData("WCM")]    // v2.2
    public void NonWoarUrlFrameIdentifiers_AreUnique(string identifier)
    {
        Assert.True(Id3v2FrameUniqueness.IsUniqueTextOrUrlIdentifier(identifier));
    }

    [Theory]
    [InlineData("WOAR")]   // v2.3+
    [InlineData("WAR")]    // v2.2
    public void Woar_AllowsMultiple(string identifier)
    {
        // ID3v2 §4.3.1: one WOAR per performer when the audio has multiple artists.
        Assert.False(Id3v2FrameUniqueness.IsUniqueTextOrUrlIdentifier(identifier));
    }

    [Theory]
    [InlineData("APIC")]   // attachments — separate class, multi-allowed by attr
    [InlineData("COMM")]   // comments — separate class
    [InlineData("PRIV")]   // private — separate class
    [InlineData("UFID")]   // unique-file-id — separate class
    [InlineData("TXXX")]   // user-defined text — separate class, multi-allowed
    [InlineData("WXXX")]   // user-defined URL — separate class, multi-allowed
    [InlineData("ASPI")]   // single-instance — uniqueness comes from attribute
    [InlineData("XYZQ")]   // unknown identifier
    public void NonFamilyIdentifiers_FallThroughToFalse(string identifier)
    {
        // Callers fall back to the editor attribute's IsUniqueInstance for these.
        Assert.False(Id3v2FrameUniqueness.IsUniqueTextOrUrlIdentifier(identifier));
    }
}
