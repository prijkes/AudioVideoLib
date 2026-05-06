namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class PopmEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2PopularimeterFrame(Id3v2Version.Id3v240)
        {
            EmailToUser = "user@example.com",
            Rating = 200,
            Counter = 42L,
        };
        var e = new PopmEditor();
        e.Load(frame);
        var copy = new Id3v2PopularimeterFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.EmailToUser, copy.EmailToUser);
        Assert.Equal(frame.Rating, copy.Rating);
        Assert.Equal(frame.Counter, copy.Counter);
    }

    [Fact]
    public void Validate_EmailToUserMustBeNonEmpty()
    {
        var e = new PopmEditor { EmailToUser = string.Empty, Rating = 100, Counter = 0 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Theory]
    [InlineData(0,   true)]
    [InlineData(255, true)]
    [InlineData(-1,  false)]
    [InlineData(256, false)]
    public void Validate_RatingRange(int rating, bool expectedValid)
    {
        var e = new PopmEditor { EmailToUser = "user@example.com", Rating = rating, Counter = 0 };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_CounterMustBeNonNegative()
    {
        var e = new PopmEditor { EmailToUser = "user@example.com", Rating = 100, Counter = -1L };
        Assert.False(e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new PopmEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
