namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class RgadEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var radio = new Id3v2ReplayGain
        {
            NameCode = Id3v2NameCode.RadioGainAdjustment,
            OriginatorCode = Id3v2OriginatorCode.SetByUser,
            Adjustment = 50,
            Sign = Id3v2ReplayGainSign.Positive,
        };
        var audiophile = new Id3v2ReplayGain
        {
            NameCode = Id3v2NameCode.AudiophileGainAdjustment,
            OriginatorCode = Id3v2OriginatorCode.PreSetByProducer,
            Adjustment = 25,
            Sign = Id3v2ReplayGainSign.Negative,
        };
        var frame = new Id3v2ReplayGainAdjustmentFrame
        {
            PeakAmplitude = 32768,
            RadioAdjustment = radio,
            AudiophileAdjustment = audiophile,
        };
        var e = new RgadEditor();
        e.Load(frame);
        var copy = new Id3v2ReplayGainAdjustmentFrame();
        e.Save(copy);
        Assert.Equal(frame.PeakAmplitude, copy.PeakAmplitude);
        Assert.Equal(frame.RadioAdjustment.NameCode, copy.RadioAdjustment.NameCode);
        Assert.Equal(frame.RadioAdjustment.OriginatorCode, copy.RadioAdjustment.OriginatorCode);
        Assert.Equal(frame.RadioAdjustment.Sign, copy.RadioAdjustment.Sign);
        Assert.Equal(frame.RadioAdjustment.Adjustment, copy.RadioAdjustment.Adjustment);
        Assert.Equal(frame.AudiophileAdjustment.NameCode, copy.AudiophileAdjustment.NameCode);
        Assert.Equal(frame.AudiophileAdjustment.OriginatorCode, copy.AudiophileAdjustment.OriginatorCode);
        Assert.Equal(frame.AudiophileAdjustment.Sign, copy.AudiophileAdjustment.Sign);
        Assert.Equal(frame.AudiophileAdjustment.Adjustment, copy.AudiophileAdjustment.Adjustment);
    }

    [Fact]
    public void Validate_NegativePeakRejected()
    {
        var e = new RgadEditor { PeakAmplitude = -1 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_NegativeAdjustmentValueRejected()
    {
        var e = new RgadEditor { RadioAdjustment = -1 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_NegativeSignWithZeroAdjustmentRejected()
    {
        var e = new RgadEditor
        {
            RadioSign = Id3v2ReplayGainSign.Negative,
            RadioAdjustment = 0,
        };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void CreateNew_ProducesFrame()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new RgadEditor().CreateNew(tag);
        Assert.NotNull(f);
    }
}
