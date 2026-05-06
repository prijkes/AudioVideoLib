namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class IplsEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2InvolvedPeopleListFrame(Id3v2Version.Id3v230)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
        };
        frame.InvolvedPeople.Add(new Id3v2InvolvedPeople("Producer", "Alice"));
        frame.InvolvedPeople.Add(new Id3v2InvolvedPeople("Engineer", "Bob"));

        var editor = new IplsEditor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);
        Assert.Equal("Producer", editor.Entries[0].Involvement);

        var copy = new Id3v2InvolvedPeopleListFrame(Id3v2Version.Id3v230);
        editor.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(2, copy.InvolvedPeople.Count);
        Assert.Equal("Producer", copy.InvolvedPeople[0].Involvement);
        Assert.Equal("Alice", copy.InvolvedPeople[0].Involvee);
        Assert.Equal("Bob", copy.InvolvedPeople[1].Involvee);
    }

    [Fact]
    public void Validate_EmptyInvolvementFails()
    {
        var editor = new IplsEditor();
        editor.AddRow(new IplsRowVm { Involvement = string.Empty, Involvee = "Alice" });
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_EmptyInvolveeFails()
    {
        var editor = new IplsEditor();
        editor.AddRow(new IplsRowVm { Involvement = "Producer", Involvee = string.Empty });
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_AllValid_Passes()
    {
        var editor = new IplsEditor();
        editor.AddRow(new IplsRowVm { Involvement = "Producer", Involvee = "Alice" });
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new IplsEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
