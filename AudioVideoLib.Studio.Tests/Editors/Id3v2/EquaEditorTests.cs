namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class EquaEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2EqualisationFrame(Id3v2Version.Id3v230)
        {
            AdjustmentBits = 16,
        };
        frame.EqualisationBands.Add(new Id3v2EqualisationBand(true, 100, 5));
        frame.EqualisationBands.Add(new Id3v2EqualisationBand(false, 200, -3));

        var editor = new EquaEditor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);

        var copy = new Id3v2EqualisationFrame(Id3v2Version.Id3v230);
        editor.Save(copy);
        Assert.Equal(frame.AdjustmentBits, copy.AdjustmentBits);
        Assert.Equal(2, copy.EqualisationBands.Count);
        var saved = copy.EqualisationBands.OrderBy(b => b.Frequency).ToList();
        Assert.Equal((short)100, saved[0].Frequency);
        Assert.Equal((short)200, saved[1].Frequency);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(16, true)]
    [InlineData(17, false)]
    public void Validate_AdjustmentBitsRange(byte bits, bool expectedValid)
    {
        // Use a non-zero adjustment so only AdjustmentBits is being checked.
        var editor = new EquaEditor { AdjustmentBits = bits };
        editor.AddRow(new EquaRowVm { Increment = true, Frequency = 100, Adjustment = 1 });
        Assert.Equal(expectedValid, editor.Validate(out _));
    }

    [Fact]
    public void Validate_ZeroAdjustmentFails()
    {
        var editor = new EquaEditor { AdjustmentBits = 16 };
        editor.AddRow(new EquaRowVm { Increment = true, Frequency = 100, Adjustment = 0 });
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_DuplicateFrequencyFails()
    {
        var editor = new EquaEditor { AdjustmentBits = 16 };
        editor.AddRow(new EquaRowVm { Increment = true,  Frequency = 100, Adjustment = 1 });
        editor.AddRow(new EquaRowVm { Increment = false, Frequency = 100, Adjustment = -1 });
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_NegativeFrequencyFails()
    {
        var editor = new EquaEditor { AdjustmentBits = 16 };
        editor.AddRow(new EquaRowVm { Increment = true, Frequency = -1, Adjustment = 1 });
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new EquaEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
