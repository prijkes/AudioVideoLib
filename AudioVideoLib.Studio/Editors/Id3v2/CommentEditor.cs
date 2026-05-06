namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2CommentFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Comment (COMM)",
    Order = 9,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class CommentEditor : ITagItemEditor<Id3v2CommentFrame>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Language { get => field; set => Set(ref field, value); } = "eng";
    public string ShortContentDescription { get => field; set => Set(ref field, value); } = string.Empty;
    public string Text { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2CommentFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2CommentFrame frame)
    {
        Load(frame);
        var dialog = new CommentEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2CommentFrame f)
    {
        Encoding = f.TextEncoding;
        Language = f.Language;
        ShortContentDescription = f.ShortContentDescription;
        Text = f.Text;
    }

    public void Save(Id3v2CommentFrame f)
    {
        f.TextEncoding = Encoding;
        f.Language = Language;
        f.ShortContentDescription = ShortContentDescription;
        f.Text = Text;
    }

    public bool Validate(out string? error)
    {
        if (Language?.Length != 3)
        {
            error = "Language must be a 3-character ISO-639-2 code (e.g. \"eng\").";
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
