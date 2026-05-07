namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2EncryptionMethodRegistrationFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Encryption method (ENCR)",
    Order = 8,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class EncrEditor : ITagItemEditor<Id3v2EncryptionMethodRegistrationFrame>, INotifyPropertyChanged
{
    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public int MethodSymbol { get => field; set => Set(ref field, value); } = 0x80;
    public byte[] Data { get => field; set => Set(ref field, value ?? []); } = [];

    public string DataInfo => Data.Length == 0
        ? "No data."
        : $"{Data.Length:N0} bytes";

    public Id3v2EncryptionMethodRegistrationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2EncryptionMethodRegistrationFrame frame)
    {
        Load(frame);
        var dialog = new EncrEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

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

    public void LoadDataFromFile(string path) => Data = File.ReadAllBytes(path);

    public void ClearData() => Data = [];

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select encryption data",
            Filter = "All files|*.*",
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        if (prop == nameof(Data))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataInfo)));
        }
    }
}
