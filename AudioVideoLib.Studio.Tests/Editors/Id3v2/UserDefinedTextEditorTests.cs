namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UserDefinedTextEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2UserDefinedTextInformationFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            Description = "MyDesc",
            Value = "MyValue",
        };

        var e = new UserDefinedTextEditor();
        e.Load(frame);
        var copy = new Id3v2UserDefinedTextInformationFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Description, copy.Description);
        Assert.Equal(frame.Value, copy.Value);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new UserDefinedTextEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Validate_EmptyDescription_Fails()
    {
        var e = new UserDefinedTextEditor { Description = string.Empty };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_NonEmptyDescription_Passes()
    {
        var e = new UserDefinedTextEditor { Description = "x" };
        Assert.True(e.Validate(out _));
    }
}
