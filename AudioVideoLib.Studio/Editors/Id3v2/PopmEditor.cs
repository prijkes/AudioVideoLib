namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PopularimeterFrame),
    Category = Id3v2FrameCategory.CountersAndRatings,
    MenuLabel = "Popularimeter (POPM)",
    Order = 26,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class PopmEditor : ITagItemEditor<Id3v2PopularimeterFrame>, INotifyPropertyChanged
{
    public string EmailToUser { get => field; set => Set(ref field, value); } = string.Empty;
    public int Rating { get => field; set => Set(ref field, value); }
    public long Counter { get => field; set => Set(ref field, value); }

    public Id3v2PopularimeterFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2PopularimeterFrame frame)
    {
        Load(frame);
        var dialog = new PopmEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2PopularimeterFrame f)
    {
        EmailToUser = f.EmailToUser ?? string.Empty;
        Rating = f.Rating;
        Counter = f.Counter;
    }

    public void Save(Id3v2PopularimeterFrame f)
    {
        f.EmailToUser = EmailToUser;
        f.Rating = (byte)Rating;
        f.Counter = Counter;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(EmailToUser))
        {
            error = "Email to user must not be empty.";
            return false;
        }
        if (Rating is < 0 or > 255)
        {
            error = "Rating must be between 0 and 255.";
            return false;
        }
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
