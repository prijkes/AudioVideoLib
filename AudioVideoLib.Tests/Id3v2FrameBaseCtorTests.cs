namespace AudioVideoLib.Tests;

using AudioVideoLib;
using AudioVideoLib.Tags;

using Xunit;

public class Id3v2FrameBaseCtorTests
{
    [Fact]
    public void BaseCtor_DispatchesToOverride_PostV230()
    {
        Assert.Throws<InvalidVersionException>(
            () => new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v230));
    }

    [Fact]
    public void BaseCtor_DispatchesToOverride_PreV230()
    {
        Assert.Throws<InvalidVersionException>(
            () => new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v240));
    }

    [Fact]
    public void BaseCtor_AcceptsValidVersion()
    {
        var f = new Id3v2CommentFrame(Id3v2Version.Id3v230);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void BaseCtor_TextFrame_NullIdentifierAtBaseCtorIsBenign()
    {
        var f = new Id3v2TextFrame(Id3v2Version.Id3v230, "TIT2");
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
        Assert.Equal("TIT2", f.Identifier);
    }

    [Fact]
    public void BaseCtor_UrlLinkFrame_NullIdentifierAtBaseCtorIsBenign()
    {
        var f = new Id3v2UrlLinkFrame(Id3v2Version.Id3v230, "WCOM");
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
        Assert.Equal("WCOM", f.Identifier);
    }

    [Fact]
    public void BaseCtor_EncryptionMethodRegistrationFrame_ParameterlessUsesV230()
    {
        var f = new Id3v2EncryptionMethodRegistrationFrame();
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Theory]
    [InlineData(Id3v2Version.Id3v230)]
    [InlineData(Id3v2Version.Id3v240)]
    public void BaseCtor_Xrva_AcceptsV230AndLater(Id3v2Version version)
    {
        var f = new Id3v2ExperimentalRelativeVolumeAdjustment2Frame(version);
        Assert.Equal(version, f.Version);
    }
}
