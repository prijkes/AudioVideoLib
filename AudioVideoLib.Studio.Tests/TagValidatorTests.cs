namespace AudioVideoLib.Studio.Tests;

using AudioVideoLib.Studio;
using AudioVideoLib.Tags;

using Xunit;

[Collection("Studio")]
public class TagValidatorTests
{
    [Fact]
    public void Validate_MultipleWoar_DoesNotWarn()
    {
        // ID3v2 §4.3.1: WOAR may appear once per performer when the audio has
        // multiple artists. The validator must not flag this as a duplicate.
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WOAR") { Url = "http://artist1.example" });
        tag.SetFrame(new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WOAR") { Url = "http://artist2.example" });
        var issues = TagValidator.Validate(tag);
        Assert.DoesNotContain(issues, i => i.Message.Contains("WOAR") && i.Message.Contains("more than once"));
    }

    [Fact]
    public void Validate_DuplicateWcom_Warns()
    {
        // WCOM is unique per identifier (§4.3, no carve-out). Two WCOMs with
        // different URLs are non-equal under Id3v2UrlLinkFrame.Equals so SetFrame
        // appends rather than replaces — the validator must warn on the duplicate.
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WCOM") { Url = "http://store1.example" });
        tag.SetFrame(new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WCOM") { Url = "http://store2.example" });
        var issues = TagValidator.Validate(tag);
        Assert.Contains(issues, i => i.Message.Contains("WCOM") && i.Message.Contains("more than once"));
    }

    [Fact]
    public void Validate_MultipleApic_DoesNotWarn()
    {
        // APIC may repeat (different picture types per spec). Editor attribute
        // declares IsUniqueInstance=false; validator must respect that.
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2AttachedPictureFrame(Id3v2Version.Id3v240) { ImageFormat = "image/jpeg", PictureData = [0xFF] });
        tag.SetFrame(new Id3v2AttachedPictureFrame(Id3v2Version.Id3v240) { ImageFormat = "image/jpeg", PictureData = [0xFE] });
        var issues = TagValidator.Validate(tag);
        Assert.DoesNotContain(issues, i => i.Message.Contains("APIC") && i.Message.Contains("more than once"));
    }
}
