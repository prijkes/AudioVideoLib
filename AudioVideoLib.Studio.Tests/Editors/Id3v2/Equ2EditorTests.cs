namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class Equ2EditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2Equalisation2Frame(Id3v2Version.Id3v240)
        {
            InterpolationMethod = Id3v2InterpolationMethod.Linear,
            Identification = "preset",
        };
        frame.AdjustmentPoints.Add(new Id3v2AdjustmentPoint(100, 1024));
        frame.AdjustmentPoints.Add(new Id3v2AdjustmentPoint(200, -512));

        var editor = new Equ2Editor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);

        var copy = new Id3v2Equalisation2Frame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.InterpolationMethod, copy.InterpolationMethod);
        Assert.Equal(frame.Identification, copy.Identification);
        Assert.Equal(2, copy.AdjustmentPoints.Count);
        var saved = copy.AdjustmentPoints.OrderBy(p => p.Frequency).ToList();
        Assert.Equal((short)100, saved[0].Frequency);
        Assert.Equal((short)1024, saved[0].VolumeAdjustment);
        Assert.Equal((short)200, saved[1].Frequency);
    }

    [Fact]
    public void Validate_EmptyIdentificationFails()
    {
        var editor = new Equ2Editor { Identification = string.Empty };
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void Validate_NonEmptyIdentification_Passes()
    {
        var editor = new Equ2Editor { Identification = "preset" };
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesV240()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new Equ2Editor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }
}
