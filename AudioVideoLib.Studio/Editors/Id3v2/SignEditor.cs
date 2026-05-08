namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2SignatureFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Signature (SIGN)",
    Order = 37,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class SignEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2SignatureFrame>
{
    public int GroupSymbol { get => field; set => Set(ref field, value); } = 0x80;

    protected override string FileDialogTitle => "Select signature data";
    protected override string FileDialogFilter => "All files|*.*";

    public Id3v2SignatureFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2SignatureFrame frame)
        => EditorDialog.Run<SignEditorDialog, Id3v2SignatureFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2SignatureFrame f)
    {
        GroupSymbol = f.GroupSymbol;
        Data = f.SignatureData ?? [];
    }

    public void Save(Id3v2SignatureFrame f)
    {
        f.GroupSymbol = (byte)GroupSymbol;
        f.SignatureData = Data;
    }

    public bool Validate(out string? error)
    {
        if (GroupSymbol is < 0x80 or > 0xF0)
        {
            error = "Group symbol must be between 0x80 and 0xF0.";
            return false;
        }
        error = null;
        return true;
    }
}
