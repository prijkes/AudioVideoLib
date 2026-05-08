namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2OwnershipFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Ownership",
    Order = 29,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
public sealed class OwneEditor : ObservableObject, ITagItemEditor<Id3v2OwnershipFrame>, IValidatedEditor
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string PricePaid { get => field; set => Set(ref field, value); } = string.Empty;
    public string DateOfPurchase { get => field; set => Set(ref field, value); } = string.Empty;
    public string Seller { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2OwnershipFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2OwnershipFrame frame)
        => EditorDialog.Run<OwneEditorDialog, Id3v2OwnershipFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2OwnershipFrame f)
    {
        Encoding = f.TextEncoding;
        PricePaid = f.PricePaid ?? string.Empty;
        DateOfPurchase = f.DateOfPurchase ?? string.Empty;
        Seller = f.Seller ?? string.Empty;
    }

    public void Save(Id3v2OwnershipFrame f)
    {
        f.TextEncoding = Encoding;
        f.PricePaid = PricePaid;
        f.DateOfPurchase = DateOfPurchase;
        f.Seller = Seller;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(PricePaid))
        {
            error = "Price paid is required.";
            return false;
        }
        if (!string.IsNullOrEmpty(DateOfPurchase) && !Id3v2DateValidation.IsYyyyMmDd(DateOfPurchase))
        {
            error = "Date of purchase must be 8 digits in YYYYMMDD format.";
            return false;
        }
        error = null;
        return true;
    }

}
