namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2ReplayGainAdjustmentFrame),
    Category = Id3v2FrameCategory.Experimental,
    MenuLabel = "Replay gain (RGAD)",
    Order = 39,
    SupportedVersions = Id3v2VersionMask.V230,
    IsUniqueInstance = true,
    // Library quirk: Id3v2ReplayGainAdjustmentFrame.IsVersionSupported is `version < Id3v230`
    // (contradicts its own "v2.3 and later" docstring), so the (Id3v2Version) ctor throws on
    // v2.3 even though the parameterless ctor uses v2.3 internally. KnownIdentifier bypasses
    // the reflection-based identifier resolution that would otherwise trigger that throw.
    KnownIdentifier = "RGAD")]
public sealed class RgadEditor : ITagItemEditor<Id3v2ReplayGainAdjustmentFrame>, INotifyPropertyChanged
{
    public int PeakAmplitude { get => field; set => Set(ref field, value); }

    public Id3v2NameCode RadioNameCode { get => field; set => Set(ref field, value); }
    public Id3v2OriginatorCode RadioOriginatorCode { get => field; set => Set(ref field, value); }
    public Id3v2ReplayGainSign RadioSign { get => field; set => Set(ref field, value); }
    public int RadioAdjustment { get => field; set => Set(ref field, value); }

    public Id3v2NameCode AudiophileNameCode { get => field; set => Set(ref field, value); }
    public Id3v2OriginatorCode AudiophileOriginatorCode { get => field; set => Set(ref field, value); }
    public Id3v2ReplayGainSign AudiophileSign { get => field; set => Set(ref field, value); }
    public int AudiophileAdjustment { get => field; set => Set(ref field, value); }

    /// <summary>
    /// Uses the parameterless constructor; the lib's `IsVersionSupported`
    /// check on the (Id3v2Version) overload disagrees with the docstring,
    /// but the parameterless ctor pins v2.3.0 which matches `SupportedVersions`.
    /// </summary>
    public Id3v2ReplayGainAdjustmentFrame CreateNew(object tag) => new();

    public bool Edit(Window owner, Id3v2ReplayGainAdjustmentFrame frame)
    {
        Load(frame);
        var dialog = new RgadEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2ReplayGainAdjustmentFrame f)
    {
        PeakAmplitude = f.PeakAmplitude;
        var radio = f.RadioAdjustment;
        RadioNameCode = radio.NameCode;
        RadioOriginatorCode = radio.OriginatorCode;
        RadioSign = radio.Sign;
        RadioAdjustment = radio.Adjustment;
        var audiophile = f.AudiophileAdjustment;
        AudiophileNameCode = audiophile.NameCode;
        AudiophileOriginatorCode = audiophile.OriginatorCode;
        AudiophileSign = audiophile.Sign;
        AudiophileAdjustment = audiophile.Adjustment;
    }

    public void Save(Id3v2ReplayGainAdjustmentFrame f)
    {
        f.PeakAmplitude = PeakAmplitude;
        // Adjustment must be set before Sign when Sign would be Negative,
        // because Id3v2ReplayGain enforces "no negative zero" in both setters.
        f.RadioAdjustment = new Id3v2ReplayGain
        {
            NameCode = RadioNameCode,
            OriginatorCode = RadioOriginatorCode,
            Adjustment = (short)RadioAdjustment,
            Sign = RadioSign,
        };
        f.AudiophileAdjustment = new Id3v2ReplayGain
        {
            NameCode = AudiophileNameCode,
            OriginatorCode = AudiophileOriginatorCode,
            Adjustment = (short)AudiophileAdjustment,
            Sign = AudiophileSign,
        };
    }

    public bool Validate(out string? error)
    {
        if (PeakAmplitude < 0)
        {
            error = "Peak amplitude must be non-negative.";
            return false;
        }
        if (!ValidateGain(RadioAdjustment, RadioSign, "Radio", out error))
        {
            return false;
        }
        if (!ValidateGain(AudiophileAdjustment, AudiophileSign, "Audiophile", out error))
        {
            return false;
        }
        error = null;
        return true;
    }

    private static bool ValidateGain(int adjustment, Id3v2ReplayGainSign sign, string label, out string? error)
    {
        if (adjustment is < 0 or > 0x1FF)
        {
            error = $"{label} adjustment must be between 0 and 511.";
            return false;
        }
        if (sign == Id3v2ReplayGainSign.Negative && adjustment == 0)
        {
            error = $"{label} adjustment cannot be a negative zero (sign Negative requires non-zero value).";
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
