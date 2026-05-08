namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2GroupIdentificationRegistrationFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Group identification (GRID)",
    Order = 7,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class GridEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2GroupIdentificationRegistrationFrame>
{
    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public int GroupSymbol { get => field; set => Set(ref field, value); } = 0x80;

    public override string DataInfo => Data.Length == 0
        ? "No data."
        : $"{Data.Length:N0} bytes";

    protected override string FileDialogTitle => "Select group dependent data";
    protected override string FileDialogFilter => "All files|*.*";

    public Id3v2GroupIdentificationRegistrationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2GroupIdentificationRegistrationFrame frame)
    {
        Load(frame);
        var dialog = new GridEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2GroupIdentificationRegistrationFrame f)
    {
        OwnerIdentifier = f.OwnerIdentifier ?? string.Empty;
        GroupSymbol = f.GroupSymbol;
        Data = f.GroupDependentData ?? [];
    }

    public void Save(Id3v2GroupIdentificationRegistrationFrame f)
    {
        f.OwnerIdentifier = OwnerIdentifier;
        f.GroupSymbol = (byte)GroupSymbol;
        f.GroupDependentData = Data;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(OwnerIdentifier))
        {
            error = "Owner identifier must not be empty.";
            return false;
        }
        if (GroupSymbol is < 0x80 or > 0xF0)
        {
            error = "Group symbol must be between 0x80 and 0xF0.";
            return false;
        }
        error = null;
        return true;
    }
}
