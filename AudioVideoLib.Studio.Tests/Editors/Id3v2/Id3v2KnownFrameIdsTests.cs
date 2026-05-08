namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System;
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

    // ----- Resolve / ResolveYear -----

    [Fact]
    public void Resolve_TIT2_V220_ReturnsTT2WriteAndTT2TIT2Reads()
    {
        // The bug: writer must use TT2 on v2.2, not TIT2.
        var r = Id3v2KnownTextFrameIds.Resolve("TIT2", Id3v2Version.Id3v220);
        Assert.Equal("TT2", r.Write);
        Assert.Equal(["TT2", "TIT2"], r.Read);
    }

    [Fact]
    public void Resolve_TIT2_V221_ReturnsTT2Write()
    {
        var r = Id3v2KnownTextFrameIds.Resolve("TIT2", Id3v2Version.Id3v221);
        Assert.Equal("TT2", r.Write);
    }

    [Fact]
    public void Resolve_TIT2_V230_ReturnsTIT2WriteAndTT2Legacy()
    {
        var r = Id3v2KnownTextFrameIds.Resolve("TIT2", Id3v2Version.Id3v230);
        Assert.Equal("TIT2", r.Write);
        Assert.Equal(["TIT2", "TT2"], r.Read);
    }

    [Fact]
    public void Resolve_TIT2_V240_SameAsV230()
    {
        var r = Id3v2KnownTextFrameIds.Resolve("TIT2", Id3v2Version.Id3v240);
        Assert.Equal("TIT2", r.Write);
        Assert.Equal(["TIT2", "TT2"], r.Read);
    }

    [Fact]
    public void Resolve_TT2_V230_AcceptsCanonicalAsV220Identifier()
    {
        // Caller passes the V220 form; should resolve identically.
        var r = Id3v2KnownTextFrameIds.Resolve("TT2", Id3v2Version.Id3v230);
        Assert.Equal("TIT2", r.Write);
    }

    [Fact]
    public void Resolve_LowerCase_AcceptsCaseInsensitive()
    {
        var r = Id3v2KnownTextFrameIds.Resolve("tit2", Id3v2Version.Id3v240);
        Assert.Equal("TIT2", r.Write);
    }

    [Fact]
    public void Resolve_UnknownIdentifier_Throws()
    {
        // Loud failure on programmer error, not silent passthrough.
        Assert.Throws<ArgumentException>(
            () => Id3v2KnownTextFrameIds.Resolve("XXXX", Id3v2Version.Id3v240));
    }

    [Fact]
    public void Resolve_TDRC_V230_Throws()
    {
        // TDRC is V240-only; asking on v2.3 must throw rather than silently write TDRC.
        Assert.Throws<ArgumentException>(
            () => Id3v2KnownTextFrameIds.Resolve("TDRC", Id3v2Version.Id3v230));
    }

    [Fact]
    public void Resolve_TYER_V240_Throws()
    {
        // TYER's mask is V220 | V221 | V230 — caller must use ResolveYear for v2.4.
        Assert.Throws<ArgumentException>(
            () => Id3v2KnownTextFrameIds.Resolve("TYER", Id3v2Version.Id3v240));
    }

    [Fact]
    public void Resolve_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Id3v2KnownTextFrameIds.Resolve(null!, Id3v2Version.Id3v240));
    }

    [Fact]
    public void ResolveYear_V240_ReturnsTDRCWithLegacyChain()
    {
        var r = Id3v2KnownTextFrameIds.ResolveYear(Id3v2Version.Id3v240);
        Assert.Equal("TDRC", r.Write);
        Assert.Equal(["TDRC", "TYER", "TYE"], r.Read);
    }

    [Fact]
    public void ResolveYear_V230_ReturnsTYERWithTYELegacy()
    {
        var r = Id3v2KnownTextFrameIds.ResolveYear(Id3v2Version.Id3v230);
        Assert.Equal("TYER", r.Write);
        Assert.Equal(["TYER", "TYE"], r.Read);
    }

    [Fact]
    public void ResolveYear_V220_ReturnsTYEWithTYERLegacy()
    {
        var r = Id3v2KnownTextFrameIds.ResolveYear(Id3v2Version.Id3v220);
        Assert.Equal("TYE", r.Write);
        Assert.Equal(["TYE", "TYER"], r.Read);
    }
}
