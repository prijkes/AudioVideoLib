namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2CommercialFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Commercial (COMR)",
    Order = 30,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class ComrEditor : ITagItemEditor<Id3v2CommercialFrame>, INotifyPropertyChanged
{
    private byte[] _sellerLogo = [];

    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string PriceString { get => field; set => Set(ref field, value); } = string.Empty;
    public string ValidUntil { get => field; set => Set(ref field, value); } = string.Empty;
    public string ContactUrl { get => field; set => Set(ref field, value); } = string.Empty;
    public Id3v2AudioDeliveryType ReceivedAs { get => field; set => Set(ref field, value); }
    public string NameOfSeller { get => field; set => Set(ref field, value); } = string.Empty;
    public string ShortDescription { get => field; set => Set(ref field, value); } = string.Empty;
    public string PictureMimeType { get => field; set => Set(ref field, value); } = string.Empty;

    public string DataInfo => _sellerLogo.Length == 0 ? "(no data)" : $"{_sellerLogo.Length:N0} bytes";

    public Id3v2CommercialFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2CommercialFrame frame)
    {
        Load(frame);
        var dialog = new ComrEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

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
        _sellerLogo = f.SellerLogo ?? [];
        OnPropertyChanged(nameof(DataInfo));
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
        f.SellerLogo = _sellerLogo;
    }

    public bool Validate(out string? error)
    {
        if (!string.IsNullOrEmpty(ValidUntil) && !IsEightDigits(ValidUntil))
        {
            error = "Valid until must be 8 digits in YYYYMMDD format.";
            return false;
        }
        if (!string.IsNullOrEmpty(ContactUrl) && !Uri.IsWellFormedUriString(ContactUrl, UriKind.Absolute))
        {
            error = "Contact URL must be a valid absolute URL.";
            return false;
        }
        error = null;
        return true;
    }

    public void LoadDataFromFile(string path)
    {
        _sellerLogo = File.ReadAllBytes(path);
        OnPropertyChanged(nameof(DataInfo));
    }

    public void ClearData()
    {
        _sellerLogo = [];
        OnPropertyChanged(nameof(DataInfo));
    }

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select seller logo",
            Filter = "Image files|*.png;*.jpg;*.jpeg|All files|*.*",
        };
        if (dlg.ShowDialog(owner) != true)
        {
            return;
        }
        try
        {
            LoadDataFromFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Could not read file:\n\n{ex.Message}", "Load",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

    private void OnPropertyChanged(string prop)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

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
