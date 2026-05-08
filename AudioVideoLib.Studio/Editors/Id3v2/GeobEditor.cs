namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2GeneralEncapsulatedObjectFrame),
    Category = Id3v2FrameCategory.Attachments,
    MenuLabel = "General encapsulated object (GEOB)",
    Order = 28,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class GeobEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2GeneralEncapsulatedObjectFrame>
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string MimeType { get => field; set => Set(ref field, value); } = string.Empty;
    public string Filename { get => field; set => Set(ref field, value); } = string.Empty;
    public string ContentDescription { get => field; set => Set(ref field, value); } = string.Empty;

    protected override string FileDialogTitle => "Select encapsulated object";
    protected override string FileDialogFilter => "All files|*.*";

    public Id3v2GeneralEncapsulatedObjectFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2GeneralEncapsulatedObjectFrame frame)
    {
        Load(frame);
        var dialog = new GeobEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2GeneralEncapsulatedObjectFrame f)
    {
        Encoding = f.TextEncoding;
        MimeType = f.MimeType ?? string.Empty;
        Filename = f.Filename ?? string.Empty;
        ContentDescription = f.ContentDescription ?? string.Empty;
        Data = f.EncapsulatedObject ?? [];
    }

    public void Save(Id3v2GeneralEncapsulatedObjectFrame f)
    {
        f.TextEncoding = Encoding;
        f.MimeType = MimeType;
        f.Filename = Filename;
        f.ContentDescription = ContentDescription;
        f.EncapsulatedObject = Data;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(ContentDescription))
        {
            error = "Content description must not be empty.";
            return false;
        }
        if (!string.IsNullOrEmpty(MimeType) && !MimeType.Contains('/'))
        {
            error = "MIME type must be of the form <type>/<subtype>.";
            return false;
        }
        error = null;
        return true;
    }
}
