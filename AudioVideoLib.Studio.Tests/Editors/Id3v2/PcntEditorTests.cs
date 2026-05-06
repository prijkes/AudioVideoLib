namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class PcntEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2PlayCounterFrame(Id3v2Version.Id3v240) { Counter = 12345L };
        var e = new PcntEditor();
        e.Load(frame);
        var copy = new Id3v2PlayCounterFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.Counter, copy.Counter);
    }

    [Theory]
    [InlineData(0L,  true)]
    [InlineData(1L,  true)]
    [InlineData(-1L, false)]
    public void Validate_NonNegative(long counter, bool expectedValid)
    {
        var e = new PcntEditor { Counter = counter };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var f = new PcntEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v220, f.Version);
    }
}
