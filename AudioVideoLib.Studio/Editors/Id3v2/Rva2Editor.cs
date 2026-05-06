namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class Rva2RowVm : INotifyPropertyChanged
{
    public Id3v2ChannelType ChannelType { get => field; set => Set(ref field, value); }
    public float VolumeAdjustment { get => field; set => Set(ref field, value); }
    public byte BitsRepresentingPeak { get => field; set => Set(ref field, value); }
    public long PeakVolume { get => field; set => Set(ref field, value); }

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

[Id3v2FrameEditor(typeof(Id3v2RelativeVolumeAdjustment2Frame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Relative volume adjustment 2 (RVA2)",
    Order = 21,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class Rva2Editor
    : CollectionEditorBase<Id3v2RelativeVolumeAdjustment2Frame, Rva2RowVm>, INotifyPropertyChanged
{
    public string Identification { get => field; set => Set(ref field, value); } = string.Empty;

    public override Id3v2RelativeVolumeAdjustment2Frame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2RelativeVolumeAdjustment2Frame frame)
    {
        Load(frame);
        var dialog = new Rva2EditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2RelativeVolumeAdjustment2Frame f)
    {
        Identification = f.Identification ?? string.Empty;
        LoadRows(f);
    }

    public void Save(Id3v2RelativeVolumeAdjustment2Frame f)
    {
        f.Identification = Identification;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2RelativeVolumeAdjustment2Frame f)
    {
        Entries.Clear();
        foreach (var c in f.ChannelInformation)
        {
            Entries.Add(new Rva2RowVm
            {
                ChannelType = c.ChannelType,
                VolumeAdjustment = c.VolumeAdjustment,
                BitsRepresentingPeak = c.BitsRepresentingPeak,
                PeakVolume = c.PeakVolume,
            });
        }
    }

    public override void SaveRows(Id3v2RelativeVolumeAdjustment2Frame f)
    {
        f.ChannelInformation.Clear();
        foreach (var r in Entries)
        {
            f.ChannelInformation.Add(new Id3v2ChannelInformation(
                r.ChannelType, r.VolumeAdjustment, r.BitsRepresentingPeak, r.PeakVolume));
        }
    }

    public override bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Identification))
        {
            error = "Identification must not be empty (the uniqueness key for RVA2).";
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
