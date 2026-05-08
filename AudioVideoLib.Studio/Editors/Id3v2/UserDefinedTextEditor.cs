namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UserDefinedTextInformationFrame),
    Category = Id3v2FrameCategory.TextFrames,
    MenuLabel = "User-defined text (TXXX)",
    Order = 2,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UserDefinedTextEditor
    : ObservableObject, ITagItemEditor<Id3v2UserDefinedTextInformationFrame>
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Description { get => field; set => Set(ref field, value); } = string.Empty;
    public string Value { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2UserDefinedTextInformationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2UserDefinedTextInformationFrame frame)
        => EditorDialog.Run<UserDefinedTextEditorDialog, Id3v2UserDefinedTextInformationFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2UserDefinedTextInformationFrame frame)
    {
        Encoding = frame.TextEncoding;
        Description = frame.Description ?? string.Empty;
        Value = frame.Value ?? string.Empty;
    }

    public void Save(Id3v2UserDefinedTextInformationFrame frame)
    {
        frame.TextEncoding = Encoding;
        frame.Description = Description;
        frame.Value = Value;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Description))
        {
            error = "Description is required for a user-defined text frame (TXXX).";
            return false;
        }
        error = null;
        return true;
    }

}
