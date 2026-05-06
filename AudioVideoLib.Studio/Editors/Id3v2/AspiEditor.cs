namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class AspiRowVm : INotifyPropertyChanged
{
    public short Fraction { get => field; set => Set(ref field, value); }

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

[Id3v2FrameEditor(typeof(Id3v2AudioSeekPointIndexFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Audio seek point index (ASPI)",
    Order = 17,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
public sealed class AspiEditor
    : CollectionEditorBase<Id3v2AudioSeekPointIndexFrame, AspiRowVm>, INotifyPropertyChanged
{
    public int IndexedDataStart { get => field; set => Set(ref field, value); }
    public int IndexedDataLength { get => field; set => Set(ref field, value); }
    public byte BitsPerIndexPoint { get => field; set => Set(ref field, value); } = 16;

    public override Id3v2AudioSeekPointIndexFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2AudioSeekPointIndexFrame frame)
    {
        Load(frame);
        var dialog = new AspiEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2AudioSeekPointIndexFrame f)
    {
        IndexedDataStart = f.IndexedDataStart;
        IndexedDataLength = f.IndexedDataLength;
        BitsPerIndexPoint = f.BitsPerIndexPoint;
        LoadRows(f);
    }

    public void Save(Id3v2AudioSeekPointIndexFrame f)
    {
        f.IndexedDataStart = IndexedDataStart;
        f.IndexedDataLength = IndexedDataLength;
        f.BitsPerIndexPoint = BitsPerIndexPoint;
        SaveRows(f);
        // Auto-sync the count to the entries actually written.
        f.NumberOfIndexPoints = (short)Entries.Count;
    }

    public override void LoadRows(Id3v2AudioSeekPointIndexFrame f)
    {
        Entries.Clear();
        foreach (var s in f.FractionAtIndex)
        {
            Entries.Add(new AspiRowVm { Fraction = s });
        }
    }

    public override void SaveRows(Id3v2AudioSeekPointIndexFrame f)
    {
        f.FractionAtIndex.Clear();
        foreach (var r in Entries)
        {
            f.FractionAtIndex.Add(r.Fraction);
        }
    }

    public override bool Validate(out string? error)
    {
        if (BitsPerIndexPoint is not (8 or 16))
        {
            error = "BitsPerIndexPoint must be 8 or 16 (per ID3v2 spec \u00A74.30).";
            return false;
        }
        var max = BitsPerIndexPoint == 8 ? byte.MaxValue : ushort.MaxValue;
        foreach (var r in Entries)
        {
            // Fraction is short; treat its bit pattern as unsigned for the upper-bound check.
            var unsigned = (ushort)r.Fraction;
            if (unsigned > max)
            {
                error = $"Each fraction must fit in {BitsPerIndexPoint} bits (max {max}).";
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
