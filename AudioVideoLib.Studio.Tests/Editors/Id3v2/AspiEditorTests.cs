namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class AspiEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v240)
        {
            IndexedDataStart = 1024,
            IndexedDataLength = 4096,
            BitsPerIndexPoint = 16,
            NumberOfIndexPoints = 3,
        };
        frame.FractionAtIndex.Add(100);
        frame.FractionAtIndex.Add(200);
        frame.FractionAtIndex.Add(300);

        var editor = new AspiEditor();
        editor.Load(frame);
        Assert.Equal(3, editor.Entries.Count);

        var copy = new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.IndexedDataStart, copy.IndexedDataStart);
        Assert.Equal(frame.IndexedDataLength, copy.IndexedDataLength);
        Assert.Equal(frame.BitsPerIndexPoint, copy.BitsPerIndexPoint);
        Assert.Equal(3, copy.FractionAtIndex.Count);
        // Auto-synced on Save
        Assert.Equal((short)3, copy.NumberOfIndexPoints);
        Assert.Equal((short)100, copy.FractionAtIndex[0]);
        Assert.Equal((short)300, copy.FractionAtIndex[2]);
    }

    [Fact]
    public void Save_AutosyncsNumberOfIndexPoints()
    {
        var editor = new AspiEditor { BitsPerIndexPoint = 8 };
        editor.AddRow(new AspiRowVm { Fraction = 1 });
        editor.AddRow(new AspiRowVm { Fraction = 2 });
        var copy = new Id3v2AudioSeekPointIndexFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal((short)2, copy.NumberOfIndexPoints);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(16, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    [InlineData(32, false)]
    public void Validate_BitsPerIndexPoint(byte bits, bool expectedValid)
    {
        var editor = new AspiEditor { BitsPerIndexPoint = bits };
        Assert.Equal(expectedValid, editor.Validate(out _));
    }

    [Fact]
    public void Validate_FractionExceedsBitDepthFails()
    {
        var editor = new AspiEditor { BitsPerIndexPoint = 8 };
        editor.AddRow(new AspiRowVm { Fraction = 300 }); // > byte.MaxValue
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesV240()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new AspiEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }
}
