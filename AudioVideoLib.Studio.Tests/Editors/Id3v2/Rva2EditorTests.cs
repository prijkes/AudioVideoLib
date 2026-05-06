namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class Rva2EditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2RelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240)
        {
            Identification = "track-norm",
        };
        frame.ChannelInformation.Add(new Id3v2ChannelInformation(Id3v2ChannelType.MasterVolume, 2.0f, 0, 0));
        frame.ChannelInformation.Add(new Id3v2ChannelInformation(Id3v2ChannelType.FrontLeft, -3.0f, 8, 100));

        var editor = new Rva2Editor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);

        var copy = new Id3v2RelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.Identification, copy.Identification);
        Assert.Equal(2, copy.ChannelInformation.Count);
        Assert.Equal(Id3v2ChannelType.MasterVolume, copy.ChannelInformation[0].ChannelType);
        Assert.Equal(2.0f, copy.ChannelInformation[0].VolumeAdjustment);
        Assert.Equal(Id3v2ChannelType.FrontLeft, copy.ChannelInformation[1].ChannelType);
        Assert.Equal((byte)8, copy.ChannelInformation[1].BitsRepresentingPeak);
        Assert.Equal(100L, copy.ChannelInformation[1].PeakVolume);
    }

    [Fact]
    public void Validate_EmptyIdentificationFails()
    {
        var editor = new Rva2Editor { Identification = string.Empty };
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_NonEmptyIdentification_Passes()
    {
        var editor = new Rva2Editor { Identification = "x" };
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesV240()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new Rva2Editor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }
}
