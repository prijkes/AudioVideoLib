namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class PossEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2PositionSynchronizationFrame(Id3v2Version.Id3v240)
        {
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
            Position = 12345L,
        };
        var e = new PossEditor();
        e.Load(frame);
        var copy = new Id3v2PositionSynchronizationFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TimeStampFormat, copy.TimeStampFormat);
        Assert.Equal(frame.Position, copy.Position);
    }

    [Theory]
    [InlineData(0L,  true)]
    [InlineData(1L,  true)]
    [InlineData(-1L, false)]
    public void Validate_PositionMustBeNonNegative(long pos, bool expectedValid)
    {
        var e = new PossEditor { Position = pos };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new PossEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
