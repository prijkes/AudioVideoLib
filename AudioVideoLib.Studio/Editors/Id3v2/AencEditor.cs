namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2AudioEncryptionFrame),
    Category = Id3v2FrameCategory.EncryptionAndCompression,
    MenuLabel = "Audio encryption",
    Order = 31,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class AencEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2AudioEncryptionFrame>, IValidatedEditor
{
    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public short PreviewStart { get => field; set => Set(ref field, value); }
    public short PreviewLength { get => field; set => Set(ref field, value); }

    protected override string FileDialogTitle => "Select encryption info";
    protected override string FileDialogFilter => "All files|*.*";

    public Id3v2AudioEncryptionFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2AudioEncryptionFrame frame)
        => EditorDialog.Run<AencEditorDialog, Id3v2AudioEncryptionFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2AudioEncryptionFrame f)
    {
        OwnerIdentifier = f.OwnerIdentifier ?? string.Empty;
        PreviewStart = f.PreviewStart;
        PreviewLength = f.PreviewLength;
        Data = f.EncryptionInfo ?? [];
    }

    public void Save(Id3v2AudioEncryptionFrame f)
    {
        f.OwnerIdentifier = OwnerIdentifier;
        f.PreviewStart = PreviewStart;
        f.PreviewLength = PreviewLength;
        f.EncryptionInfo = Data;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(OwnerIdentifier))
        {
            error = "Owner identifier must not be empty.";
            return false;
        }
        if (PreviewStart < 0)
        {
            error = "Preview start must be non-negative.";
            return false;
        }
        if (PreviewLength < 0)
        {
            error = "Preview length must be non-negative.";
            return false;
        }
        error = null;
        return true;
    }

}
