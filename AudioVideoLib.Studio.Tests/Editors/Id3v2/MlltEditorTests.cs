namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class MlltEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2MpegLocationLookupTableFrame(Id3v2Version.Id3v240)
        {
            MpegFramesBetweenReference = 100,
            BytesBetweenReference = 4096,
            MillisecondsBetweenReference = 500,
        };
        frame.References.Add(new Id3v2MpegLookupTableItem(0x10, 0x05));
        frame.References.Add(new Id3v2MpegLookupTableItem(0x20, 0x0A));

        var editor = new MlltEditor();
        editor.Load(frame);
        Assert.Equal(2, editor.Entries.Count);
        Assert.Equal((byte)0x10, editor.Entries[0].DeviationInBytes);

        var copy = new Id3v2MpegLocationLookupTableFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.MpegFramesBetweenReference, copy.MpegFramesBetweenReference);
        Assert.Equal(frame.BytesBetweenReference, copy.BytesBetweenReference);
        Assert.Equal(frame.MillisecondsBetweenReference, copy.MillisecondsBetweenReference);
        Assert.Equal(2, copy.References.Count);
        Assert.Equal((byte)0x10, copy.References[0].DeviationInBytes);
        Assert.Equal((byte)0x05, copy.References[0].DeviationInMilliseconds);
        Assert.Equal((byte)0x20, copy.References[1].DeviationInBytes);
    }

    [Fact]
    public void Validate_NegativeHeader_Fails()
    {
        var editor = new MlltEditor { MpegFramesBetweenReference = -1 };
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_DefaultsValid()
    {
        var editor = new MlltEditor
        {
            MpegFramesBetweenReference = 0,
            BytesBetweenReference = 0,
            MillisecondsBetweenReference = 0,
        };
        Assert.True(editor.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new MlltEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void AddRow_AppendsToEntries()
    {
        var e = new MlltEditor();
        e.AddRow(new MlltRowVm { DeviationInBytes = 1, DeviationInMilliseconds = 2 });
        Assert.Single(e.Entries);
    }
}
