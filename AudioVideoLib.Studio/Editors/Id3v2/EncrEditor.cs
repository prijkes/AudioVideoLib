namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2EncryptionMethodRegistrationFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Encryption method (ENCR)",
    Order = 8,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class EncrEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2EncryptionMethodRegistrationFrame>
{
    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public int MethodSymbol { get => field; set => Set(ref field, value); } = 0x80;

    public override string DataInfo => Data.Length == 0
        ? "No data."
        : $"{Data.Length:N0} bytes";

    protected override string FileDialogTitle => "Select encryption data";
    protected override string FileDialogFilter => "All files|*.*";

    public Id3v2EncryptionMethodRegistrationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2EncryptionMethodRegistrationFrame frame)
        => EditorDialog.Run<EncrEditorDialog, Id3v2EncryptionMethodRegistrationFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2EncryptionMethodRegistrationFrame f)
    {
        OwnerIdentifier = f.OwnerIdentifier ?? string.Empty;
        MethodSymbol = f.MethodSymbol;
        Data = f.EncryptionData ?? [];
    }

    public void Save(Id3v2EncryptionMethodRegistrationFrame f)
    {
        f.OwnerIdentifier = OwnerIdentifier;
        f.MethodSymbol = (byte)MethodSymbol;
        f.EncryptionData = Data;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(OwnerIdentifier))
        {
            error = "Owner identifier must not be empty.";
            return false;
        }
        if (MethodSymbol is < 0x80 or > 0xF0)
        {
            error = "Method symbol must be between 0x80 and 0xF0.";
            return false;
        }
        error = null;
        return true;
    }
}
