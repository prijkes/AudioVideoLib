namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2RelativeVolumeAdjustmentFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Relative volume adjustment (RVAD)",
    Order = 20,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
public sealed class RvadEditor : ITagItemEditor<Id3v2RelativeVolumeAdjustmentFrame>, INotifyPropertyChanged
{
    public int IncrementDecrement { get => field; set => Set(ref field, value); }
    public int VolumeDescriptionBits { get => field; set => Set(ref field, value); } = 16;

    public int RelativeVolumeChangeRightChannel { get => field; set => Set(ref field, value); }
    public int RelativeVolumeChangeLeftChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeRightChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeLeftChannel { get => field; set => Set(ref field, value); }

    public int RelativeVolumeChangeRightBackChannel { get => field; set => Set(ref field, value); }
    public int RelativeVolumeChangeLeftBackChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeRightBackChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeLeftBackChannel { get => field; set => Set(ref field, value); }

    public int RelativeVolumeChangeCenterChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeCenterChannel { get => field; set => Set(ref field, value); }

    public int RelativeVolumeChangeBassChannel { get => field; set => Set(ref field, value); }
    public int PeakVolumeBassChannel { get => field; set => Set(ref field, value); }

    public Id3v2RelativeVolumeAdjustmentFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2RelativeVolumeAdjustmentFrame frame)
    {
        Load(frame);
        var dialog = new RvadEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2RelativeVolumeAdjustmentFrame f)
    {
        IncrementDecrement = f.IncrementDecrement;
        VolumeDescriptionBits = f.VolumeDescriptionBits;
        RelativeVolumeChangeRightChannel = f.RelativeVolumeChangeRightChannel;
        RelativeVolumeChangeLeftChannel = f.RelativeVolumeChangeLeftChannel;
        PeakVolumeRightChannel = f.PeakVolumeRightChannel;
        PeakVolumeLeftChannel = f.PeakVolumeLeftChannel;
        RelativeVolumeChangeRightBackChannel = f.RelativeVolumeChangeRightBackChannel;
        RelativeVolumeChangeLeftBackChannel = f.RelativeVolumeChangeLeftBackChannel;
        PeakVolumeRightBackChannel = f.PeakVolumeRightBackChannel;
        PeakVolumeLeftBackChannel = f.PeakVolumeLeftBackChannel;
        RelativeVolumeChangeCenterChannel = f.RelativeVolumeChangeCenterChannel;
        PeakVolumeCenterChannel = f.PeakVolumeCenterChannel;
        RelativeVolumeChangeBassChannel = f.RelativeVolumeChangeBassChannel;
        PeakVolumeBassChannel = f.PeakVolumeBassChannel;
    }

    public void Save(Id3v2RelativeVolumeAdjustmentFrame f)
    {
        f.IncrementDecrement = (byte)IncrementDecrement;
        f.VolumeDescriptionBits = (byte)VolumeDescriptionBits;
        f.RelativeVolumeChangeRightChannel = RelativeVolumeChangeRightChannel;
        f.RelativeVolumeChangeLeftChannel = RelativeVolumeChangeLeftChannel;
        f.PeakVolumeRightChannel = PeakVolumeRightChannel;
        f.PeakVolumeLeftChannel = PeakVolumeLeftChannel;
        f.RelativeVolumeChangeRightBackChannel = RelativeVolumeChangeRightBackChannel;
        f.RelativeVolumeChangeLeftBackChannel = RelativeVolumeChangeLeftBackChannel;
        f.PeakVolumeRightBackChannel = PeakVolumeRightBackChannel;
        f.PeakVolumeLeftBackChannel = PeakVolumeLeftBackChannel;
        f.RelativeVolumeChangeCenterChannel = RelativeVolumeChangeCenterChannel;
        f.PeakVolumeCenterChannel = PeakVolumeCenterChannel;
        f.RelativeVolumeChangeBassChannel = RelativeVolumeChangeBassChannel;
        f.PeakVolumeBassChannel = PeakVolumeBassChannel;
    }

    public bool Validate(out string? error)
    {
        if (VolumeDescriptionBits is < 1 or > 64)
        {
            error = "Volume description bits must be between 1 and 64.";
            return false;
        }
        if (IncrementDecrement is < 0 or > 255)
        {
            error = "Increment/decrement must be between 0 and 255.";
            return false;
        }
        if (AnyNegativeAdjustment())
        {
            error = "Adjustment values must be non-negative (sign is encoded by Increment/decrement bits).";
            return false;
        }
        if (AnyNegativePeak())
        {
            error = "Peak values must be non-negative.";
            return false;
        }
        error = null;
        return true;
    }

    private bool AnyNegativeAdjustment()
        => RelativeVolumeChangeRightChannel < 0
        || RelativeVolumeChangeLeftChannel < 0
        || RelativeVolumeChangeRightBackChannel < 0
        || RelativeVolumeChangeLeftBackChannel < 0
        || RelativeVolumeChangeCenterChannel < 0
        || RelativeVolumeChangeBassChannel < 0;

    private bool AnyNegativePeak()
        => PeakVolumeRightChannel < 0
        || PeakVolumeLeftChannel < 0
        || PeakVolumeRightBackChannel < 0
        || PeakVolumeLeftBackChannel < 0
        || PeakVolumeCenterChannel < 0
        || PeakVolumeBassChannel < 0;

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
