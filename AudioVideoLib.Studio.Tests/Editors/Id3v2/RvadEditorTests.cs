namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class RvadEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version.Id3v230)
        {
            IncrementDecrement = 0x3F,
            VolumeDescriptionBits = 16,
            RelativeVolumeChangeRightChannel = 100,
            RelativeVolumeChangeLeftChannel = 110,
            PeakVolumeRightChannel = 200,
            PeakVolumeLeftChannel = 210,
            RelativeVolumeChangeRightBackChannel = 120,
            RelativeVolumeChangeLeftBackChannel = 130,
            PeakVolumeRightBackChannel = 220,
            PeakVolumeLeftBackChannel = 230,
            RelativeVolumeChangeCenterChannel = 140,
            PeakVolumeCenterChannel = 240,
            RelativeVolumeChangeBassChannel = 150,
            PeakVolumeBassChannel = 250,
        };
        var e = new RvadEditor();
        e.Load(frame);
        var copy = new Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version.Id3v230);
        e.Save(copy);
        Assert.Equal(frame.IncrementDecrement, copy.IncrementDecrement);
        Assert.Equal(frame.VolumeDescriptionBits, copy.VolumeDescriptionBits);
        Assert.Equal(frame.RelativeVolumeChangeRightChannel, copy.RelativeVolumeChangeRightChannel);
        Assert.Equal(frame.RelativeVolumeChangeLeftChannel, copy.RelativeVolumeChangeLeftChannel);
        Assert.Equal(frame.PeakVolumeRightChannel, copy.PeakVolumeRightChannel);
        Assert.Equal(frame.PeakVolumeLeftChannel, copy.PeakVolumeLeftChannel);
        Assert.Equal(frame.RelativeVolumeChangeRightBackChannel, copy.RelativeVolumeChangeRightBackChannel);
        Assert.Equal(frame.RelativeVolumeChangeLeftBackChannel, copy.RelativeVolumeChangeLeftBackChannel);
        Assert.Equal(frame.PeakVolumeRightBackChannel, copy.PeakVolumeRightBackChannel);
        Assert.Equal(frame.PeakVolumeLeftBackChannel, copy.PeakVolumeLeftBackChannel);
        Assert.Equal(frame.RelativeVolumeChangeCenterChannel, copy.RelativeVolumeChangeCenterChannel);
        Assert.Equal(frame.PeakVolumeCenterChannel, copy.PeakVolumeCenterChannel);
        Assert.Equal(frame.RelativeVolumeChangeBassChannel, copy.RelativeVolumeChangeBassChannel);
        Assert.Equal(frame.PeakVolumeBassChannel, copy.PeakVolumeBassChannel);
    }

    [Theory]
    [InlineData(0, false)]   // 0 disallowed by lib
    [InlineData(1, true)]
    [InlineData(16, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void Validate_VolumeDescriptionBitsRange(int bits, bool expectedValid)
    {
        var e = new RvadEditor { VolumeDescriptionBits = bits };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_NegativeAdjustmentRejected()
    {
        var e = new RvadEditor
        {
            VolumeDescriptionBits = 16,
            RelativeVolumeChangeRightChannel = -1,
        };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_NegativePeakRejected()
    {
        var e = new RvadEditor
        {
            VolumeDescriptionBits = 16,
            PeakVolumeRightChannel = -1,
        };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new RvadEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
