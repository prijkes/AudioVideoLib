namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class Id3v2AddMenuBuilderTests
{
    [Fact]
    public void BuildEntryLabel_NotInTag_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2MusicCdIdentifierFrame))
        {
            MenuLabel = "Music CD identifier (MCDI)",
            IsUniqueInstance = true,
            SupportedVersions = Id3v2VersionMask.All,
        };
        Assert.StartsWith("Add", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }

    [Fact]
    public void BuildEntryLabel_InTagAndUnique_ReturnsEdit()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        // MCDI requires a TRCK frame to be present first per the lib's invariants.
        var trck = new Id3v2TextFrame(Id3v2Version.Id3v240, "TRCK");
        trck.Values.Add("1");
        tag.SetFrame(trck);
        var mcdi = new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] };
        tag.SetFrame(mcdi);
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2MusicCdIdentifierFrame))
        {
            MenuLabel = "Music CD identifier (MCDI)",
            IsUniqueInstance = true,
            SupportedVersions = Id3v2VersionMask.All,
        };
        Assert.StartsWith("Edit", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }

    [Fact]
    public void BuildEntryLabel_InTagButNotUnique_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        // PRIV.OwnerIdentifier must be a valid RFC 1738 URL.
        var priv = new Id3v2PrivateFrame(Id3v2Version.Id3v240)
        {
            OwnerIdentifier = "http://example.com",
            PrivateData = [1],
        };
        tag.SetFrame(priv);
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2PrivateFrame))
        {
            MenuLabel = "Private (PRIV)",
            IsUniqueInstance = false,
            SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
        };
        Assert.StartsWith("Add", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }

    [Fact]
    public void BuildModel_V240_TextFrameEntries_UseV240Identifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
        var text = model.Categories.Single(c => c.Category == Id3v2FrameCategory.TextFrames);
        Assert.Contains(text.Entries, e => e.FrameIdentifier == "TIT2");
    }

    [Fact]
    public void BuildModel_V220_TextFrameEntries_UseV220Identifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
        var text = model.Categories.Single(c => c.Category == Id3v2FrameCategory.TextFrames);
        Assert.Contains(text.Entries, e => e.FrameIdentifier == "TT2");
    }

    [Fact]
    public void BuildModel_V240_HidesContainersCategory()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
        Assert.DoesNotContain(model.Categories, c => c.Category == Id3v2FrameCategory.Containers);
    }
}
