namespace AudioVideoLib.Studio.Tests;

using System.Linq;

using AudioVideoLib.Studio;
using AudioVideoLib.Tags;

using Xunit;

[Collection("Studio")]
public class Id3v2TabViewModelTests
{
    [Fact]
    public void OpenV220Tag_CommitsWithoutChanges_DoesNotUpgradeIdentifiers()
    {
        // Regression: opening a v2.2 tag and saving used to silently rewrite
        // every text frame to its v2.3+ identifier (TT2 → TIT2 etc.). After
        // the resolver+sweep refactor, the tag must round-trip unchanged.
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        AddTextFrame(tag, "TT2", "Title");
        AddTextFrame(tag, "TP1", "Artist");
        AddTextFrame(tag, "TAL", "Album");

        var vm = new Id3v2TabViewModel(tag);
        vm.CommitToTag();

        Assert.Contains(tag.Frames, f => f.Identifier == "TT2");
        Assert.Contains(tag.Frames, f => f.Identifier == "TP1");
        Assert.Contains(tag.Frames, f => f.Identifier == "TAL");
        Assert.DoesNotContain(tag.Frames, f => f.Identifier == "TIT2");
        Assert.DoesNotContain(tag.Frames, f => f.Identifier == "TPE1");
        Assert.DoesNotContain(tag.Frames, f => f.Identifier == "TALB");
    }

    [Fact]
    public void OpenV220Tag_EditsTitle_WritesTT2_NotTIT2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        AddTextFrame(tag, "TT2", "Old");

        var vm = new Id3v2TabViewModel(tag) { Title = "New" };
        vm.CommitToTag();

        var tt2 = tag.Frames.OfType<Id3v2TextFrame>().Single(f => f.Identifier == "TT2");
        Assert.Equal("New", tt2.Values.FirstOrDefault());
        Assert.DoesNotContain(tag.Frames, f => f.Identifier == "TIT2");
    }

    [Fact]
    public void MixedTagWithBothIds_AfterCommit_ContainsOnlyWriteId()
    {
        // Pins the sweep behaviour: a v2.3 tag that somehow contains both
        // TIT2 and TT2 (e.g. produced by a buggy older save) must collapse
        // to just TIT2 on commit.
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        AddTextFrame(tag, "TIT2", "Modern");
        AddTextFrame(tag, "TT2", "Legacy");

        var vm = new Id3v2TabViewModel(tag);
        vm.CommitToTag();

        var titleFrames = tag.Frames.OfType<Id3v2TextFrame>()
            .Where(f => f.Identifier is "TIT2" or "TT2")
            .ToList();
        Assert.Single(titleFrames);
        Assert.Equal("TIT2", titleFrames[0].Identifier);
    }

    private static void AddTextFrame(Id3v2Tag tag, string identifier, string value)
    {
        var frame = new Id3v2TextFrame(tag.Version, identifier)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
        };
        frame.Values.Add(value);
        tag.SetFrame(frame);
    }
}
