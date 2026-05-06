namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UserDefinedUrlEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2UserDefinedUrlLinkFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            Description = "MyDesc",
            Url = "http://example.com/",
        };

        var e = new UserDefinedUrlEditor();
        e.Load(frame);
        var copy = new Id3v2UserDefinedUrlLinkFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Description, copy.Description);
        Assert.Equal(frame.Url, copy.Url);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new UserDefinedUrlEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Theory]
    [InlineData("",     "http://example.com/", false)] // empty description
    [InlineData("desc", "",                    false)] // empty URL
    [InlineData("desc", "not a url",           false)] // bad URL
    [InlineData("desc", "http://example.com/", true)]
    public void Validate(string description, string url, bool expectedValid)
    {
        var e = new UserDefinedUrlEditor { Description = description, Url = url };
        Assert.Equal(expectedValid, e.Validate(out _));
    }
}
