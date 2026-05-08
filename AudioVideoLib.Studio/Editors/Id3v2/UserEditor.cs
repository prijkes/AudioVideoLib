namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2TermsOfUseFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Terms of use (USER)",
    Order = 10,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class UserEditor : ObservableObject, ITagItemEditor<Id3v2TermsOfUseFrame>, IValidatedEditor
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Language { get => field; set => Set(ref field, value); } = "eng";
    public string Text { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2TermsOfUseFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2TermsOfUseFrame frame)
        => EditorDialog.Run<UserEditorDialog, Id3v2TermsOfUseFrame>(
            owner, frame, this, Load, Save);

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

}
