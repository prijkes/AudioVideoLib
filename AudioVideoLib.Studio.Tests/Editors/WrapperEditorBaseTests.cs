namespace AudioVideoLib.Studio.Tests.Editors;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Xunit;

public class WrapperEditorBaseTests
{
    private sealed class Dialog : WrapperEditorBase<Id3v2CompressedDataMetaFrame>
    {
        public override Id3v2CompressedDataMetaFrame CreateNew(object tag) => new();
        public override bool Edit(Window owner, Id3v2CompressedDataMetaFrame frame) => false;
        public override bool Validate(out string? error)
        {
            error = null;
            return true;
        }
    }

    [Fact]
    public void TakeSnapshot_ExcludesSelf()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();
        var other = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(self);
        tag.SetFrame(other);

        var d = new Dialog();
        d.TakeSnapshot(tag, self);
        Assert.Single(d.WrappableSnapshot);
        Assert.Same(other, d.WrappableSnapshot[0]);
    }

    [Fact]
    public void TakeSnapshot_ExcludesOtherCdmWrappers_AtV221()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();
        var siblingCdm = new Id3v2CompressedDataMetaFrame();
        var plain = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(self);
        tag.SetFrame(siblingCdm);
        tag.SetFrame(plain);

        var d = new Dialog();
        d.TakeSnapshot(tag, self);
        Assert.Single(d.WrappableSnapshot);
        Assert.Same(plain, d.WrappableSnapshot[0]);
    }

    [Fact]
    public void OnAfterEdit_RemovesSelectedChildFromTag()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();
        var child = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(child);

        var d = new Dialog { SelectedChild = child };
        d.OnAfterEdit(tag, self);

        Assert.DoesNotContain(child, tag.Frames);
    }

    [Fact]
    public void OnAfterEdit_NoChild_NoOp()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();
        var sibling = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(sibling);

        var d = new Dialog();   // SelectedChild = null
        d.OnAfterEdit(tag, self);

        Assert.Contains(sibling, tag.Frames);
    }
}
