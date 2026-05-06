namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PositionSynchronizationFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Position synchronization (POSS)",
    Order = 16,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
public sealed class PossEditor : ITagItemEditor<Id3v2PositionSynchronizationFrame>, INotifyPropertyChanged
{
    public Id3v2TimeStampFormat TimeStampFormat { get => field; set => Set(ref field, value); }
        = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds;
    public long Position { get => field; set => Set(ref field, value); }

    public Id3v2PositionSynchronizationFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2PositionSynchronizationFrame frame)
    {
        Load(frame);
        var dialog = new PossEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2PositionSynchronizationFrame f)
    {
        TimeStampFormat = f.TimeStampFormat;
        Position = f.Position;
    }

    public void Save(Id3v2PositionSynchronizationFrame f)
    {
        f.TimeStampFormat = TimeStampFormat;
        f.Position = Position;
    }

    public bool Validate(out string? error)
    {
        if (Position < 0)
        {
            error = "Position must be non-negative.";
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
