namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2OwnershipFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Ownership (OWNE)",
    Order = 29,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
public sealed class OwneEditor : ITagItemEditor<Id3v2OwnershipFrame>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string PricePaid { get => field; set => Set(ref field, value); } = string.Empty;
    public string DateOfPurchase { get => field; set => Set(ref field, value); } = string.Empty;
    public string Seller { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2OwnershipFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2OwnershipFrame frame)
    {
        Load(frame);
        var dialog = new OwneEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

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
        if (!string.IsNullOrEmpty(DateOfPurchase) && !IsEightDigits(DateOfPurchase))
        {
            error = "Date of purchase must be 8 digits in YYYYMMDD format.";
            return false;
        }
        error = null;
        return true;
    }

    private static bool IsEightDigits(string value)
    {
        if (value.Length != 8)
        {
            return false;
        }
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }
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
