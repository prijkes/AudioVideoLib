namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2SeekFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Seek (SEEK)",
    Order = 36,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
public sealed class SeekEditor : ITagItemEditor<Id3v2SeekFrame>, INotifyPropertyChanged
{
    public int MinimumOffsetToNextTag { get => field; set => Set(ref field, value); }

    public Id3v2SeekFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2SeekFrame frame)
    {
        Load(frame);
        var dialog = new SeekEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2SeekFrame f) => MinimumOffsetToNextTag = f.MinimumOffsetToNextTag;

    public void Save(Id3v2SeekFrame f) => f.MinimumOffsetToNextTag = MinimumOffsetToNextTag;

    public bool Validate(out string? error)
    {
        if (MinimumOffsetToNextTag < 0)
        {
            error = "Minimum offset to next tag must be non-negative.";
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
