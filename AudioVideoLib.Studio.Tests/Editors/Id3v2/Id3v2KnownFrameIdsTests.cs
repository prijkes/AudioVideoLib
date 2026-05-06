namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class Id3v2KnownFrameIdsTests
{
    [Fact]
    public void TextIds_NoDuplicateIdentifiers()
        => Assert.Equal(Id3v2KnownTextFrameIds.All.Length,
                        Id3v2KnownTextFrameIds.All.Select(i => i.Identifier).Distinct().Count());

    [Fact]
    public void TextIds_NoDuplicateV220Identifiers()
    {
        var v220 = Id3v2KnownTextFrameIds.All.Where(i => i.V220Identifier is not null)
                                             .Select(i => i.V220Identifier!).ToArray();
        Assert.Equal(v220.Length, v220.Distinct().Count());
    }

    [Fact]
    public void TextIds_AllStartWithT()
        => Assert.All(Id3v2KnownTextFrameIds.All, i => Assert.StartsWith("T", i.Identifier));

    [Theory]
    [InlineData("TIT2")] [InlineData("TPE1")] [InlineData("TALB")] [InlineData("TCON")]
    public void TextId_ConstructsAtV240(string identifier)
    {
        var entry = Id3v2KnownTextFrameIds.All.Single(i => i.Identifier == identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v240))
        {
            return;
        }
        var f = new Id3v2TextFrame(Id3v2Version.Id3v240, identifier);
        Assert.Equal(identifier, f.Identifier);
    }

    [Theory]
    [InlineData("TYER", "TYE")] [InlineData("TIT2", "TT2")] [InlineData("TALB", "TAL")]
    public void TextId_V220_ConstructsAndIdentifierMatches(string v240Id, string v220Id)
    {
        var entry = Id3v2KnownTextFrameIds.All.Single(i => i.Identifier == v240Id);
        Assert.Equal(v220Id, entry.V220Identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v220))
        {
            return;
        }
        var f = new Id3v2TextFrame(Id3v2Version.Id3v220, v220Id);
        Assert.Equal(v220Id, f.Identifier);
    }

    [Fact]
    public void UrlIds_NoDuplicates()
        => Assert.Equal(Id3v2KnownUrlFrameIds.All.Length,
                        Id3v2KnownUrlFrameIds.All.Select(i => i.Identifier).Distinct().Count());

    [Fact]
    public void UrlIds_AllStartWithW()
        => Assert.All(Id3v2KnownUrlFrameIds.All, i => Assert.StartsWith("W", i.Identifier));

    [Theory]
    [InlineData("WCOM")] [InlineData("WOAR")] [InlineData("WPUB")]
    public void UrlId_ConstructsAtV240(string identifier)
    {
        var entry = Id3v2KnownUrlFrameIds.All.Single(i => i.Identifier == identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v240))
        {
            return;
        }
        var f = new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, identifier);
        Assert.Equal(identifier, f.Identifier);
    }
}
