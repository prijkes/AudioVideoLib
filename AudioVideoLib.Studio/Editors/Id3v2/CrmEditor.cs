namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2EncryptedMetaFrame),
    Category = Id3v2FrameCategory.Containers,
    MenuLabel = "Encrypted meta (CRM)",
    Order = 33,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221,
    IsUniqueInstance = false,
    KnownIdentifier = "CRM")]
public sealed class CrmEditor : WrapperEditorBase<Id3v2EncryptedMetaFrame>, INotifyPropertyChanged
{
    private byte[] _encryptedDataBlock = [];

    public string OwnerIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public string ContentExplanation { get => field; set => Set(ref field, value); } = string.Empty;

    public string DataInfo => _encryptedDataBlock.Length == 0
        ? "(no data)"
        : $"{_encryptedDataBlock.Length:N0} bytes";

    public override Id3v2EncryptedMetaFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2EncryptedMetaFrame frame)
    {
        // Snapshot is populated by the dispatch caller via IWrapperEditor.OnBeforeEdit.
        Load(frame);
        var dialog = new CrmEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2EncryptedMetaFrame f)
    {
        OwnerIdentifier = f.OwnerIdentifier ?? string.Empty;
        ContentExplanation = f.ContentExplanation ?? string.Empty;
        _encryptedDataBlock = f.EncryptedDataBlock ?? [];
        OnPropertyChanged(nameof(DataInfo));
    }

    public void Save(Id3v2EncryptedMetaFrame f)
    {
        f.OwnerIdentifier = OwnerIdentifier;
        f.ContentExplanation = ContentExplanation;
        if (SelectedChild is not null)
        {
            // Wrap the child's data block; coordinator (MainWindow.OnAddOrEditFrame)
            // is responsible for removing the original child from the tag.
            f.EncryptedDataBlock = SelectedChild.Data ?? [];
        }
        else
        {
            f.EncryptedDataBlock = _encryptedDataBlock;
        }
    }

    public override bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(OwnerIdentifier))
        {
            error = "Owner identifier must not be empty (the uniqueness key for CRM).";
            return false;
        }
        if (!Id3v2Frame.IsValidUrl(OwnerIdentifier))
        {
            error = "Owner identifier must be a valid RFC 1738 URL (e.g. http://example.com).";
            return false;
        }
        if (SelectedChild is null && _encryptedDataBlock.Length == 0)
        {
            error = "Pick a frame to wrap or load an encrypted data blob from disk.";
            return false;
        }
        error = null;
        return true;
    }

    public void LoadDataFromFile(string path)
    {
        _encryptedDataBlock = File.ReadAllBytes(path);
        OnPropertyChanged(nameof(DataInfo));
    }

    public void ClearData()
    {
        _encryptedDataBlock = [];
        OnPropertyChanged(nameof(DataInfo));
    }

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select encrypted data block",
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
        catch (System.Exception ex)
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
