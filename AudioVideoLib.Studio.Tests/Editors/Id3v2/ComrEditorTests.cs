namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class ComrEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2CommercialFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            PriceString = "USD9.99",
            ValidUntil = "20261231",
            ContactUrl = "http://example.com",
            ReceivedAs = Id3v2AudioDeliveryType.InternetFile,
            NameOfSeller = "ACME Records",
            ShortDescription = "Special offer",
            PictureMimeType = "image/png",
            SellerLogo = [0xDE, 0xAD, 0xBE, 0xEF],
        };
        var e = new ComrEditor();
        e.Load(frame);
        var copy = new Id3v2CommercialFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.PriceString, copy.PriceString);
        Assert.Equal(frame.ValidUntil, copy.ValidUntil);
        Assert.Equal(frame.ContactUrl, copy.ContactUrl);
        Assert.Equal(frame.ReceivedAs, copy.ReceivedAs);
        Assert.Equal(frame.NameOfSeller, copy.NameOfSeller);
        Assert.Equal(frame.ShortDescription, copy.ShortDescription);
        Assert.Equal(frame.PictureMimeType, copy.PictureMimeType);
        Assert.Equal(frame.SellerLogo, copy.SellerLogo);
    }

    [Theory]
    [InlineData("20261231", true)]
    [InlineData("2026-12-31", false)]
    [InlineData("ABCDEFGH", false)]
    [InlineData("", true)]
    public void Validate_ValidUntil(string date, bool expectedValid)
    {
        var e = new ComrEditor
        {
            PriceString = "USD9.99",
            ValidUntil = date,
            ContactUrl = "http://example.com",
        };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("not a url", false)]
    [InlineData("", true)]
    public void Validate_ContactUrl(string url, bool expectedValid)
    {
        var e = new ComrEditor
        {
            PriceString = "USD9.99",
            ValidUntil = "20261231",
            ContactUrl = url,
        };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new ComrEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void LoadDataFromFile_AndClear_UpdateState()
    {
        var path = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, [1, 2, 3, 4, 5]);
            var e = new ComrEditor();
            e.LoadDataFromFile(path);
            Assert.Equal("5 bytes", e.DataInfo);
            e.ClearData();
            Assert.Equal("(no data)", e.DataInfo);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
