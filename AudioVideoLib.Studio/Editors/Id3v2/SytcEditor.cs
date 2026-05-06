namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class SytcRowVm : INotifyPropertyChanged
{
    public int BeatsPerMinute { get => field; set => Set(ref field, value); }
    public int TimeStamp { get => field; set => Set(ref field, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

[Id3v2FrameEditor(typeof(Id3v2SyncedTempoCodesFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Synced tempo codes (SYTC)",
    Order = 15,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class SytcEditor
    : CollectionEditorBase<Id3v2SyncedTempoCodesFrame, SytcRowVm>, INotifyPropertyChanged
{
    public Id3v2TimeStampFormat TimeStampFormat { get => field; set => Set(ref field, value); }

    public override Id3v2SyncedTempoCodesFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2SyncedTempoCodesFrame frame)
    {
        Load(frame);
        var dialog = new SytcEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2SyncedTempoCodesFrame f)
    {
        TimeStampFormat = f.TimeStampFormat;
        LoadRows(f);
    }

    public void Save(Id3v2SyncedTempoCodesFrame f)
    {
        f.TimeStampFormat = TimeStampFormat;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2SyncedTempoCodesFrame f)
    {
        Entries.Clear();
        foreach (var t in f.TempoData)
        {
            Entries.Add(new SytcRowVm { BeatsPerMinute = t.BeatsPerMinute, TimeStamp = t.TimeStamp });
        }
    }

    public override void SaveRows(Id3v2SyncedTempoCodesFrame f)
    {
        f.TempoData.Clear();
        foreach (var r in Entries)
        {
            f.TempoData.Add(new Id3v2TempoCode(r.BeatsPerMinute, r.TimeStamp));
        }
    }

    public override bool Validate(out string? error)
    {
        var prev = int.MinValue;
        foreach (var r in Entries)
        {
            var bpm = r.BeatsPerMinute;
            if (bpm is < 0 or > 510)
            {
                error = "Beats per minute must be 0, 1, or in the range 2..510 (per ID3v2 spec).";
                return false;
            }
            if (r.TimeStamp < prev)
            {
                error = "Tempo codes must be in monotonic non-decreasing timestamp order.";
                return false;
            }
            prev = r.TimeStamp;
        }
        error = null;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
