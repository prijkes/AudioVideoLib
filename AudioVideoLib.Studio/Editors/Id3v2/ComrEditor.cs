namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2CommercialFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Commercial",
    Order = 30,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class ComrEditor : BinaryDataEditorBase, ITagItemEditor<Id3v2CommercialFrame>, IValidatedEditor
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string PriceString { get => field; set => Set(ref field, value); } = string.Empty;
    public string ValidUntil { get => field; set => Set(ref field, value); } = string.Empty;
    public string ContactUrl { get => field; set => Set(ref field, value); } = string.Empty;
    public Id3v2AudioDeliveryType ReceivedAs { get => field; set => Set(ref field, value); }
    public string NameOfSeller { get => field; set => Set(ref field, value); } = string.Empty;
    public string ShortDescription { get => field; set => Set(ref field, value); } = string.Empty;
    public string PictureMimeType { get => field; set => Set(ref field, value); } = string.Empty;

    protected override string FileDialogTitle => "Select seller logo";
    protected override string FileDialogFilter => "Image files|*.png;*.jpg;*.jpeg|All files|*.*";

    public Id3v2CommercialFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2CommercialFrame frame)
        => EditorDialog.Run<ComrEditorDialog, Id3v2CommercialFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2CommercialFrame f)
    {
        Encoding = f.TextEncoding;
        PriceString = f.PriceString ?? string.Empty;
        ValidUntil = f.ValidUntil ?? string.Empty;
        ContactUrl = f.ContactUrl ?? string.Empty;
        ReceivedAs = f.ReceivedAs;
        NameOfSeller = f.NameOfSeller ?? string.Empty;
        ShortDescription = f.ShortDescription ?? string.Empty;
        PictureMimeType = f.PictureMimeType ?? string.Empty;
        Data = f.SellerLogo ?? [];
    }

    public void Save(Id3v2CommercialFrame f)
    {
        f.TextEncoding = Encoding;
        f.PriceString = PriceString;
        f.ValidUntil = ValidUntil;
        f.ContactUrl = ContactUrl;
        f.ReceivedAs = ReceivedAs;
        f.NameOfSeller = NameOfSeller;
        f.ShortDescription = ShortDescription;
        f.PictureMimeType = PictureMimeType;
        f.SellerLogo = Data;
    }

    public bool Validate(out string? error)
    {
        if (!string.IsNullOrEmpty(ValidUntil) && !Id3v2DateValidation.IsYyyyMmDd(ValidUntil))
        {
            error = "Valid until must be 8 digits in YYYYMMDD format.";
            return false;
        }
        if (!string.IsNullOrEmpty(ContactUrl) && !Id3v2Frame.IsValidUrl(ContactUrl))
        {
            error = "Contact URL must be a valid RFC 1738 URL.";
            return false;
        }
        error = null;
        return true;
    }

}
