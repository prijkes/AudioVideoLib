namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class CrmEditorTests
{
    [Fact]
    public void Validate_NoChildAndNoBlob_Fails()
    {
        var editor = new CrmEditor { OwnerIdentifier = "http://example.com", ContentExplanation = "x" };
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_EmptyOwnerFails()
    {
        var editor = new CrmEditor
        {
            OwnerIdentifier = string.Empty,
            SelectedChild = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2"),
        };
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_InvalidUrlFails()
    {
        var editor = new CrmEditor
        {
            OwnerIdentifier = "not a url",
            SelectedChild = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2"),
        };
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_ValidUrlAndChild_Passes()
    {
        var editor = new CrmEditor
        {
            OwnerIdentifier = "http://example.com",
            SelectedChild = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2"),
        };
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void Save_WritesChildBytesIntoEncryptedDataBlock()
    {
        var child = new Id3v2TextFrame(Id3v2Version.Id3v220, "TT2");
        child.Values.Add("title");
        var editor = new CrmEditor
        {
            OwnerIdentifier = "http://example.com",
            ContentExplanation = "test",
            SelectedChild = child,
        };
        var frame = new Id3v2EncryptedMetaFrame(Id3v2Version.Id3v220);
        editor.Save(frame);
        Assert.Equal("http://example.com", frame.OwnerIdentifier);
        Assert.Equal("test", frame.ContentExplanation);
        Assert.NotNull(frame.EncryptedDataBlock);
        Assert.NotEmpty(frame.EncryptedDataBlock);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var f = new CrmEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v221, f.Version);
    }
}
