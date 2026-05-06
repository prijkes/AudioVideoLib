namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class EquaRowVm : INotifyPropertyChanged
{
    public bool Increment { get => field; set => Set(ref field, value); }
    public short Frequency { get => field; set => Set(ref field, value); }
    public int Adjustment { get => field; set => Set(ref field, value); }

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

[Id3v2FrameEditor(typeof(Id3v2EqualisationFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Equalisation (EQUA)",
    Order = 22,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
public sealed class EquaEditor
    : CollectionEditorBase<Id3v2EqualisationFrame, EquaRowVm>, INotifyPropertyChanged
{
    public byte AdjustmentBits { get => field; set => Set(ref field, value); } = 16;

    public override Id3v2EqualisationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2EqualisationFrame frame)
    {
        Load(frame);
        var dialog = new EquaEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2EqualisationFrame f)
    {
        AdjustmentBits = f.AdjustmentBits == 0 ? (byte)16 : f.AdjustmentBits;
        LoadRows(f);
    }

    public void Save(Id3v2EqualisationFrame f)
    {
        f.AdjustmentBits = AdjustmentBits;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2EqualisationFrame f)
    {
        Entries.Clear();
        foreach (var b in f.EqualisationBands)
        {
            Entries.Add(new EquaRowVm { Increment = b.Increment, Frequency = b.Frequency, Adjustment = b.Adjustment });
        }
    }

    public override void SaveRows(Id3v2EqualisationFrame f)
    {
        f.EqualisationBands.Clear();
        foreach (var r in Entries.OrderBy(x => x.Frequency))
        {
            f.EqualisationBands.Add(new Id3v2EqualisationBand(r.Increment, r.Frequency, r.Adjustment));
        }
    }

    public override bool Validate(out string? error)
    {
        if (AdjustmentBits is < 1 or > 16)
        {
            error = "AdjustmentBits must be in the range 1..16 (per ID3v2 spec).";
            return false;
        }
        var seen = new HashSet<short>();
        foreach (var r in Entries)
        {
            if (r.Frequency < 0)
            {
                error = "Frequency must be non-negative (0..32767 Hz).";
                return false;
            }
            if (r.Adjustment == 0)
            {
                error = "Adjustment values of 0 are reserved (the ID3v2 spec requires omitting them); change or remove the row.";
                return false;
            }
            if (!seen.Add(r.Frequency))
            {
                error = "Each frequency may appear at most once in the band list.";
                return false;
            }
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
