namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2AudioEncryptionFrame),
    Category = Id3v2FrameCategory.EncryptionAndCompression,
    MenuLabel = "Audio encryption (AENC)",
    Order = 31,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class AencEditor : ITagItemEditor<Id3v2AudioEncryptionFrame>, INotifyPropertyChanged
{
    private byte[] _encryptionInfo = [];

    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public short PreviewStart { get => field; set => Set(ref field, value); }
    public short PreviewLength { get => field; set => Set(ref field, value); }

    public string DataInfo => _encryptionInfo.Length == 0 ? "(no data)" : $"{_encryptionInfo.Length:N0} bytes";

    public Id3v2AudioEncryptionFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2AudioEncryptionFrame frame)
    {
        Load(frame);
        var dialog = new AencEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2AudioEncryptionFrame f)
    {
        OwnerIdentifier = f.OwnerIdentifier ?? string.Empty;
        PreviewStart = f.PreviewStart;
        PreviewLength = f.PreviewLength;
        _encryptionInfo = f.EncryptionInfo ?? [];
        OnPropertyChanged(nameof(DataInfo));
    }

    public void Save(Id3v2AudioEncryptionFrame f)
    {
        f.OwnerIdentifier = OwnerIdentifier;
        f.PreviewStart = PreviewStart;
        f.PreviewLength = PreviewLength;
        f.EncryptionInfo = _encryptionInfo;
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

    public void LoadDataFromFile(string path)
    {
        _encryptionInfo = File.ReadAllBytes(path);
        OnPropertyChanged(nameof(DataInfo));
    }

    public void ClearData()
    {
        _encryptionInfo = [];
        OnPropertyChanged(nameof(DataInfo));
    }

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select encryption info",
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
