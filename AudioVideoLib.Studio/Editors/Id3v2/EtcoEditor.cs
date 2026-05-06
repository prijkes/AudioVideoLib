namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class EtcoRowVm : INotifyPropertyChanged
{
    public Id3v2KeyEventType EventType { get => field; set => Set(ref field, value); }
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

[Id3v2FrameEditor(typeof(Id3v2EventTimingCodesFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Event timing codes (ETCO)",
    Order = 13,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class EtcoEditor
    : CollectionEditorBase<Id3v2EventTimingCodesFrame, EtcoRowVm>, INotifyPropertyChanged
{
    public Id3v2TimeStampFormat TimeStampFormat { get => field; set => Set(ref field, value); }

    public override Id3v2EventTimingCodesFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2EventTimingCodesFrame frame)
    {
        Load(frame);
        var dialog = new EtcoEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2EventTimingCodesFrame frame)
    {
        TimeStampFormat = frame.TimeStampFormat;
        LoadRows(frame);
    }

    public void Save(Id3v2EventTimingCodesFrame frame)
    {
        frame.TimeStampFormat = TimeStampFormat;
        SaveRows(frame);
    }

    public override void LoadRows(Id3v2EventTimingCodesFrame frame)
    {
        Entries.Clear();
        foreach (var ev in frame.KeyEvents)
        {
            Entries.Add(new EtcoRowVm { EventType = ev.EventType, TimeStamp = ev.TimeStamp });
        }
    }

    public override void SaveRows(Id3v2EventTimingCodesFrame frame)
    {
        frame.KeyEvents.Clear();
        foreach (var r in Entries)
        {
            frame.KeyEvents.Add(new Id3v2KeyEvent(r.EventType, r.TimeStamp));
        }
    }

    public override bool Validate(out string? error)
    {
        var prev = int.MinValue;
        foreach (var r in Entries)
        {
            if (r.TimeStamp < prev)
            {
                error = "Events must be in monotonic non-decreasing timestamp order " +
                        "(per ID3v2 spec \u00A74.6).";
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
