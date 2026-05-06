namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2TermsOfUseFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Terms of use (USER)",
    Order = 10,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class UserEditor : ITagItemEditor<Id3v2TermsOfUseFrame>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Language { get => field; set => Set(ref field, value); } = "eng";
    public string Text { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2TermsOfUseFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2TermsOfUseFrame frame)
    {
        Load(frame);
        var dialog = new UserEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2TermsOfUseFrame f)
    {
        Encoding = f.TextEncoding;
        Language = f.Language;
        Text = f.Text;
    }

    public void Save(Id3v2TermsOfUseFrame f)
    {
        f.TextEncoding = Encoding;
        f.Language = Language;
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
