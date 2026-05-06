namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UserEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2TermsOfUseFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            Language = "eng",
            Text = "Sample terms text.",
        };
        var e = new UserEditor();
        e.Load(frame);
        var copy = new Id3v2TermsOfUseFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Language, copy.Language);
        Assert.Equal(frame.Text, copy.Text);
    }

    [Theory]
    [InlineData("en",   false)]
    [InlineData("eng",  true)]
    [InlineData("ENGL", false)]
    [InlineData("",     false)]
    public void Validate_LanguageMustBe3Chars(string lang, bool expectedValid)
    {
        var e = new UserEditor { Language = lang };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new UserEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
