namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PlayCounterFrame),
    Category = Id3v2FrameCategory.CountersAndRatings,
    MenuLabel = "Play counter (PCNT)",
    Order = 25,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class PcntEditor : ITagItemEditor<Id3v2PlayCounterFrame>, INotifyPropertyChanged
{
    public long Counter { get => field; set => Set(ref field, value); }

    public Id3v2PlayCounterFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2PlayCounterFrame frame)
    {
        Load(frame);
        var dialog = new PcntEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2PlayCounterFrame f) => Counter = f.Counter;

    public void Save(Id3v2PlayCounterFrame f) => f.Counter = Counter;

    public bool Validate(out string? error)
    {
        if (Counter < 0)
        {
            error = "Counter must be non-negative.";
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
