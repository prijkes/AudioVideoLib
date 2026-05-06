namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class LinkEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip_V240()
    {
        var frame = new Id3v2LinkedInformationFrame(Id3v2Version.Id3v240, "WCOM")
        {
            Url = "http://example.com/",
            AdditionalIdData = "extra",
        };
        var e = new LinkEditor();
        e.Load(frame);
        var copy = new Id3v2LinkedInformationFrame(Id3v2Version.Id3v240, "TIT2");
        e.Save(copy);
        Assert.Equal(frame.FrameIdentifier, copy.FrameIdentifier);
        Assert.Equal(frame.Url, copy.Url);
        Assert.Equal(frame.AdditionalIdData, copy.AdditionalIdData);
    }

    [Theory]
    [InlineData("WCOM", true)]
    [InlineData("WCO",  false)]
    [InlineData("WCOMM", false)]
    [InlineData("",     false)]
    public void Validate_FrameIdentifierLength_V240(string id, bool expectedValid)
    {
        var frame = new Id3v2LinkedInformationFrame(Id3v2Version.Id3v240, "WCOM");
        var e = new LinkEditor();
        e.Load(frame);
        e.FrameIdentifier = id;
        e.Url = "http://example.com/";
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Theory]
    [InlineData("WCM", true)]
    [InlineData("WC",  false)]
    [InlineData("WCMM", false)]
    public void Validate_FrameIdentifierLength_V220(string id, bool expectedValid)
    {
        var frame = new Id3v2LinkedInformationFrame(Id3v2Version.Id3v220, "WCM");
        var e = new LinkEditor();
        e.Load(frame);
        e.FrameIdentifier = id;
        e.Url = "http://example.com/";
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_UrlMustBeAbsolute()
    {
        var frame = new Id3v2LinkedInformationFrame(Id3v2Version.Id3v240, "WCOM");
        var e = new LinkEditor();
        e.Load(frame);
        e.FrameIdentifier = "WCOM";
        e.Url = "not-a-url";
        Assert.False(e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new LinkEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
