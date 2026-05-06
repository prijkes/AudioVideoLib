namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class SeekEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2SeekFrame(Id3v2Version.Id3v240) { MinimumOffsetToNextTag = 4096 };
        var e = new SeekEditor();
        e.Load(frame);
        var copy = new Id3v2SeekFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.MinimumOffsetToNextTag, copy.MinimumOffsetToNextTag);
    }

    [Theory]
    [InlineData(0,  true)]
    [InlineData(1,  true)]
    [InlineData(-1, false)]
    public void Validate_NonNegative(int offset, bool expectedValid)
    {
        var e = new SeekEditor { MinimumOffsetToNextTag = offset };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new SeekEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }
}
