namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class CdmEditorTests
{
    [Fact]
    public void Validate_NoChildSelected_Fails()
    {
        var e = new CdmEditor();
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_ChildSelected_Passes()
    {
        var e = new CdmEditor
        {
            SelectedChild = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2"),
        };
        Assert.True(e.Validate(out _));
    }

    [Fact]
    public void CreateNew_ReturnsCdmFrame()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var f = new CdmEditor().CreateNew(tag);
        Assert.NotNull(f);
    }

    [Fact]
    public void Save_WritesChildBytesAndZlibMethod()
    {
        var child = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        child.Values.Add("hello");
        var e = new CdmEditor { SelectedChild = child };
        var f = new Id3v2CompressedDataMetaFrame();
        e.Save(f);
        Assert.Equal(Id3v2CompressionMethod.ZLib, f.CompressionMethod);
        Assert.NotNull(f.CompressedFrame);
        Assert.NotEmpty(f.CompressedFrame);
        // Sanity: child's serialised bytes are non-empty.
        Assert.Equal(child.Data, f.CompressedFrame);
    }

    [Fact]
    public void Save_NullChild_LeavesFrameUntouched()
    {
        var e = new CdmEditor();
        var f = new Id3v2CompressedDataMetaFrame();
        var beforeBytes = f.CompressedFrame;
        e.Save(f);
        Assert.Same(beforeBytes, f.CompressedFrame);
    }
}
