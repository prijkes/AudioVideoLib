namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UrlFrameEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WCOM")
        {
            Url = "http://example.com/",
        };

        var e = new UrlFrameEditor();
        e.Load(frame);
        var copy = new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WCOM");
        e.Save(copy);
        Assert.Equal(frame.Url, copy.Url);
    }

    [Fact]
    public void CreateNew_NoIdentifier_Throws()
        => Assert.Throws<System.InvalidOperationException>(() =>
                new UrlFrameEditor().CreateNew(new Id3v2Tag(Id3v2Version.Id3v240)));

    [Fact]
    public void CreateNew_WithIdentifier_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new UrlFrameEditor().CreateNew(tag, "WCOM");
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Theory]
    [InlineData("",                       false)]
    [InlineData("not a url",              false)]
    [InlineData("http://example.com/",    true)]
    [InlineData("https://example.com/",   true)]
    public void Validate_RequiresAbsoluteUri(string url, bool expectedValid)
    {
        var e = new UrlFrameEditor { Url = url };
        Assert.Equal(expectedValid, e.Validate(out _));
    }
}
