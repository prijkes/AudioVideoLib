namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

public sealed class MlltRowVm : ObservableObject
{
    public byte DeviationInBytes { get => field; set => Set(ref field, value); }
    public byte DeviationInMilliseconds { get => field; set => Set(ref field, value); }

}

[Id3v2FrameEditor(typeof(Id3v2MpegLocationLookupTableFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "MPEG location lookup table",
    Order = 14,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class MlltEditor
    : CollectionEditorBase<Id3v2MpegLocationLookupTableFrame, MlltRowVm>
{
    public short MpegFramesBetweenReference { get => field; set => Set(ref field, value); }
    public int BytesBetweenReference { get => field; set => Set(ref field, value); }
    public int MillisecondsBetweenReference { get => field; set => Set(ref field, value); }

    public override Id3v2MpegLocationLookupTableFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2MpegLocationLookupTableFrame frame)
        => EditorDialog.Run<MlltEditorDialog, Id3v2MpegLocationLookupTableFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2MpegLocationLookupTableFrame f)
    {
        MpegFramesBetweenReference = f.MpegFramesBetweenReference;
        BytesBetweenReference = f.BytesBetweenReference;
        MillisecondsBetweenReference = f.MillisecondsBetweenReference;
        LoadRows(f);
    }

    public void Save(Id3v2MpegLocationLookupTableFrame f)
    {
        f.MpegFramesBetweenReference = MpegFramesBetweenReference;
        f.BytesBetweenReference = BytesBetweenReference;
        f.MillisecondsBetweenReference = MillisecondsBetweenReference;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2MpegLocationLookupTableFrame f)
    {
        Entries.Clear();
        foreach (var r in f.References)
        {
            Entries.Add(new MlltRowVm
            {
                DeviationInBytes = r.DeviationInBytes,
                DeviationInMilliseconds = r.DeviationInMilliseconds,
            });
        }
    }

    public override void SaveRows(Id3v2MpegLocationLookupTableFrame f)
    {
        f.References.Clear();
        foreach (var r in Entries)
        {
            f.References.Add(new Id3v2MpegLookupTableItem(r.DeviationInBytes, r.DeviationInMilliseconds));
        }
    }

    public override bool Validate(out string? error)
    {
        if (MpegFramesBetweenReference < 0 || BytesBetweenReference < 0 || MillisecondsBetweenReference < 0)
        {
            error = "Header values must be non-negative.";
            return false;
        }
        error = null;
        return true;
    }

}
