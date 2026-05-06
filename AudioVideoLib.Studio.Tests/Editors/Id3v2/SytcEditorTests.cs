namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class SytcEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2SyncedTempoCodesFrame(Id3v2Version.Id3v240)
        {
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
        };
        frame.TempoData.Add(new Id3v2TempoCode(120, 1000));
        frame.TempoData.Add(new Id3v2TempoCode(140, 2000));

        var editor = new SytcEditor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);

        var copy = new Id3v2SyncedTempoCodesFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.TimeStampFormat, copy.TimeStampFormat);
        Assert.Equal(2, copy.TempoData.Count);
        var saved = copy.TempoData.ToList();
        Assert.Equal(120, saved[0].BeatsPerMinute);
        Assert.Equal(1000, saved[0].TimeStamp);
        Assert.Equal(140, saved[1].BeatsPerMinute);
    }

    [Fact]
    public void Validate_NonDecreasingTimestampsRequired()
    {
        var editor = new SytcEditor { TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds };
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 120, TimeStamp = 1000 });
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 140, TimeStamp = 500 });
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_BpmReservedOrInRange()
    {
        // 0 and 1 are reserved beat-free / single-beat values; allow them.
        var editor = new SytcEditor { TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds };
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 0, TimeStamp = 0 });
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 1, TimeStamp = 100 });
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 510, TimeStamp = 200 });
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void Validate_BpmOutOfRangeFails()
    {
        var editor = new SytcEditor { TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds };
        editor.AddRow(new SytcRowVm { BeatsPerMinute = 600, TimeStamp = 0 });
        Assert.False(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new SytcEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
