namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class OwneEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2OwnershipFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            PricePaid = "USD10.99",
            DateOfPurchase = "20260506",
            Seller = "ACME Records",
        };
        var e = new OwneEditor();
        e.Load(frame);
        var copy = new Id3v2OwnershipFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.PricePaid, copy.PricePaid);
        Assert.Equal(frame.DateOfPurchase, copy.DateOfPurchase);
        Assert.Equal(frame.Seller, copy.Seller);
    }

    [Theory]
    [InlineData("20260506", true)]
    [InlineData("2026-05-06", false)]
    [InlineData("ABCDEFGH", false)]
    [InlineData("", true)]
    public void Validate_DateOfPurchase(string date, bool expectedValid)
    {
        var e = new OwneEditor
        {
            PricePaid = "USD10.99",
            DateOfPurchase = date,
            Seller = "ACME",
        };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_PricePaidMustBeNonEmpty()
    {
        var e = new OwneEditor { PricePaid = string.Empty, DateOfPurchase = "20260506" };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new OwneEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
