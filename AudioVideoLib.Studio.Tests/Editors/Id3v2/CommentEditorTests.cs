namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class CommentEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2CommentFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            Language = "eng",
            ShortContentDescription = "desc",
            Text = "comment text",
        };
        var e = new CommentEditor();
        e.Load(frame);
        var copy = new Id3v2CommentFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Language, copy.Language);
        Assert.Equal(frame.ShortContentDescription, copy.ShortContentDescription);
        Assert.Equal(frame.Text, copy.Text);
    }

    [Theory]
    [InlineData("en",   false)]
    [InlineData("eng",  true)]
    [InlineData("ENGL", false)]
    [InlineData("",     false)]
    public void Validate_LanguageMustBe3Chars(string lang, bool expectedValid)
    {
        var e = new CommentEditor { Language = lang };
        Assert.Equal(expectedValid, e.Validate(out _));
    }
}
