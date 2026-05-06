namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class RevbEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2ReverbFrame(Id3v2Version.Id3v240)
        {
            ReverbLeftMilliseconds = 1000,
            ReverbRightMilliseconds = 1100,
            ReverbBouncesLeft = 5,
            ReverbBouncesRight = 6,
            ReverbFeedbackLeftToLeft = 0x80,
            ReverbFeedbackLeftToRight = 0x40,
            ReverbFeedbackRightToRight = 0x70,
            ReverbFeedbackRightToLeft = 0x30,
            PremixLeftToRight = 0x20,
            PremixRightToLeft = 0x10,
        };
        var e = new RevbEditor();
        e.Load(frame);
        var copy = new Id3v2ReverbFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.ReverbLeftMilliseconds, copy.ReverbLeftMilliseconds);
        Assert.Equal(frame.ReverbRightMilliseconds, copy.ReverbRightMilliseconds);
        Assert.Equal(frame.ReverbBouncesLeft, copy.ReverbBouncesLeft);
        Assert.Equal(frame.ReverbBouncesRight, copy.ReverbBouncesRight);
        Assert.Equal(frame.ReverbFeedbackLeftToLeft, copy.ReverbFeedbackLeftToLeft);
        Assert.Equal(frame.ReverbFeedbackLeftToRight, copy.ReverbFeedbackLeftToRight);
        Assert.Equal(frame.ReverbFeedbackRightToRight, copy.ReverbFeedbackRightToRight);
        Assert.Equal(frame.ReverbFeedbackRightToLeft, copy.ReverbFeedbackRightToLeft);
        Assert.Equal(frame.PremixLeftToRight, copy.PremixLeftToRight);
        Assert.Equal(frame.PremixRightToLeft, copy.PremixRightToLeft);
    }

    [Fact]
    public void Validate_NegativeReverbTimeRejected()
    {
        var e = new RevbEditor { ReverbLeftMilliseconds = -1 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public void Validate_BouncesRange(int bounces, bool expectedValid)
    {
        var e = new RevbEditor { ReverbBouncesLeft = bounces };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new RevbEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
