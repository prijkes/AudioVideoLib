namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class XrvaEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2ExperimentalRelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240)
        {
            Identification = "experimental-track",
        };
        frame.ChannelInformation.Add(new Id3v2ChannelInformation(Id3v2ChannelType.MasterVolume, 1.5f, 0, 0));
        frame.ChannelInformation.Add(new Id3v2ChannelInformation(Id3v2ChannelType.FrontRight, -2.0f, 8, 50));

        var editor = new XrvaEditor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);

        var copy = new Id3v2ExperimentalRelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal("experimental-track", copy.Identification);
        Assert.Equal(2, copy.ChannelInformation.Count);
        Assert.Equal(Id3v2ChannelType.MasterVolume, copy.ChannelInformation[0].ChannelType);
        Assert.Equal(Id3v2ChannelType.FrontRight, copy.ChannelInformation[1].ChannelType);
    }

    [Fact]
    public void Validate_EmptyIdentificationFails()
    {
        var editor = new XrvaEditor { Identification = string.Empty };
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_NonEmptyIdentification_Passes()
    {
        var editor = new XrvaEditor { Identification = "x" };
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesV240()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new XrvaEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }
}
