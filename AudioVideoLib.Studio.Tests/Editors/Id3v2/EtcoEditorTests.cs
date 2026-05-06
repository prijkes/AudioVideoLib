namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class EtcoEditorTests
{
    [Fact]
    public void LoadSaveRoundTrip_PopulatedFrame()
    {
        var frame = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240)
        {
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
        };
        frame.KeyEvents.Add(new Id3v2KeyEvent(Id3v2KeyEventType.MainPartStart, 1000));

        var editor = new EtcoEditor();
        editor.Load(frame);
        Assert.Single(editor.Entries);

        var copy = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.TimeStampFormat, copy.TimeStampFormat);
        Assert.Single(copy.KeyEvents);
    }

    [Fact]
    public void EmptyEventList_RoundTrips()
    {
        var frame = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        var editor = new EtcoEditor();
        editor.Load(frame);
        Assert.Empty(editor.Entries);
        var copy = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Empty(copy.KeyEvents);
    }

    [Fact]
    public void AddRow_AppendsToEntries()
    {
        var editor = new EtcoEditor();
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.Padding, TimeStamp = 500 });
        Assert.Single(editor.Entries);
    }

    [Fact]
    public void Validate_NonMonotonicTimestamps_Fails()
    {
        var editor = new EtcoEditor { TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds };
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.MainPartStart, TimeStamp = 1000 });
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.MainPartEnd, TimeStamp = 500 });
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
        Assert.Contains("monotonic", err, System.StringComparison.OrdinalIgnoreCase);
    }
}
