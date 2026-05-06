namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class Equ2RowVm : INotifyPropertyChanged
{
    public short Frequency { get => field; set => Set(ref field, value); }
    public short VolumeAdjustment { get => field; set => Set(ref field, value); }

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

[Id3v2FrameEditor(typeof(Id3v2Equalisation2Frame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Equalisation 2 (EQU2)",
    Order = 23,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class Equ2Editor
    : CollectionEditorBase<Id3v2Equalisation2Frame, Equ2RowVm>, INotifyPropertyChanged
{
    public Id3v2InterpolationMethod InterpolationMethod { get => field; set => Set(ref field, value); }
    public string Identification { get => field; set => Set(ref field, value); } = string.Empty;

    public override Id3v2Equalisation2Frame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2Equalisation2Frame frame)
    {
        Load(frame);
        var dialog = new Equ2EditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2Equalisation2Frame f)
    {
        InterpolationMethod = f.InterpolationMethod;
        Identification = f.Identification ?? string.Empty;
        LoadRows(f);
    }

    public void Save(Id3v2Equalisation2Frame f)
    {
        f.InterpolationMethod = InterpolationMethod;
        f.Identification = Identification;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2Equalisation2Frame f)
    {
        Entries.Clear();
        foreach (var p in f.AdjustmentPoints)
        {
            Entries.Add(new Equ2RowVm { Frequency = p.Frequency, VolumeAdjustment = p.VolumeAdjustment });
        }
    }

    public override void SaveRows(Id3v2Equalisation2Frame f)
    {
        f.AdjustmentPoints.Clear();
        foreach (var r in Entries.OrderBy(x => x.Frequency))
        {
            f.AdjustmentPoints.Add(new Id3v2AdjustmentPoint(r.Frequency, r.VolumeAdjustment));
        }
    }

    public override bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Identification))
        {
            error = "Identification must not be empty (the uniqueness key for EQU2).";
            return false;
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
