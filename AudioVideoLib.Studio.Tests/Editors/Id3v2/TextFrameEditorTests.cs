namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class TextFrameEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2")
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
        };
        frame.Values.Add("Test title");

        var editor = new TextFrameEditor();
        editor.Load(frame);
        var copy = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        editor.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Values.ToArray(), copy.Values.ToArray());
    }

    [Fact]
    public void CreateNew_NoIdentifier_Throws()
        => Assert.Throws<System.InvalidOperationException>(() =>
                new TextFrameEditor().CreateNew(new Id3v2Tag(Id3v2Version.Id3v240)));

    [Fact]
    public void CreateNew_WithIdentifier_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new TextFrameEditor().CreateNew(tag, "TPE1");
        Assert.Equal("TPE1", f.Identifier);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Validate_AlwaysTrue_ForBaseTextFrame()
    {
        var e = new TextFrameEditor();
        Assert.True(e.Validate(out _));
    }
}
