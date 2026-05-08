namespace AudioVideoLib.Tests;

using System.Linq;

using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// D4 regression: <see cref="Id3v2TextFrame.EnumerateIdentifierMappings"/> and
/// <see cref="Id3v2UrlLinkFrame.EnumerateIdentifierMappings"/> are the canonical
/// view over the lib's identifier tables. Pin a few representative entries so
/// future edits to the dictionary don't silently change the public iterator.
/// </summary>
public class Id3v2EnumerateIdentifierMappingsTests
{
    [Theory]
    [InlineData("TALB", "TAL", 4)]   // all 4 versions
    [InlineData("TBPM", "TBP", 4)]   // all 4 versions
    [InlineData("TMOO", null, 1)]    // v2.4-only
    [InlineData("TYER", "TYE", 3)]   // legacy: v2.2 / v2.2.1 / v2.3
    [InlineData("TSIZ", "TSI", 3)]   // v2.2 / v2.2.1 / v2.3 (no v2.4)
    public void TextFrame_EnumeratorYieldsExpectedMapping(string canonical, string? alternate, int versionCount)
    {
        var mapping = Id3v2TextFrame.EnumerateIdentifierMappings()
            .Single(m => m.Identifier == canonical);

        Assert.Equal(canonical, mapping.Identifier);
        Assert.Equal(alternate, mapping.V220Identifier);
        Assert.Equal(versionCount, mapping.SupportedVersions.Count);
    }

    [Theory]
    [InlineData("WCOM", "WCM", 4)]   // all 4 versions
    [InlineData("WOAR", "WAR", 4)]
    [InlineData("WORS", null, 2)]    // v2.3 / v2.4 only
    [InlineData("WPAY", null, 2)]    // v2.3 / v2.4 only
    public void UrlLinkFrame_EnumeratorYieldsExpectedMapping(string canonical, string? alternate, int versionCount)
    {
        var mapping = Id3v2UrlLinkFrame.EnumerateIdentifierMappings()
            .Single(m => m.Identifier == canonical);

        Assert.Equal(canonical, mapping.Identifier);
        Assert.Equal(alternate, mapping.V220Identifier);
        Assert.Equal(versionCount, mapping.SupportedVersions.Count);
    }

    [Fact]
    public void TextFrame_EnumeratorYieldsAllEntries()
        => Assert.Equal(51, Id3v2TextFrame.EnumerateIdentifierMappings().Count());

    [Fact]
    public void UrlLinkFrame_EnumeratorYieldsAllEntries()
        => Assert.Equal(8, Id3v2UrlLinkFrame.EnumerateIdentifierMappings().Count());
}
