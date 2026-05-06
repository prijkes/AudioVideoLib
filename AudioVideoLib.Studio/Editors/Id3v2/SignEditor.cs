namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2SignatureFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Signature (SIGN)",
    Order = 37,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class SignEditor : ITagItemEditor<Id3v2SignatureFrame>, INotifyPropertyChanged
{
    private byte[] _signatureData = [];

    public int GroupSymbol { get => field; set => Set(ref field, value); } = 0x80;

    public string DataInfo => _signatureData.Length == 0
        ? "(no data)"
        : $"{_signatureData.Length:N0} bytes";

    public Id3v2SignatureFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2SignatureFrame frame)
    {
        Load(frame);
        var dialog = new SignEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2SignatureFrame f)
    {
        GroupSymbol = f.GroupSymbol;
        _signatureData = f.SignatureData ?? [];
        OnPropertyChanged(nameof(DataInfo));
    }

    public void Save(Id3v2SignatureFrame f)
    {
        f.GroupSymbol = (byte)GroupSymbol;
        f.SignatureData = _signatureData;
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

    public void LoadDataFromFile(string path)
    {
        _signatureData = File.ReadAllBytes(path);
        OnPropertyChanged(nameof(DataInfo));
    }

    public void ClearData()
    {
        _signatureData = [];
        OnPropertyChanged(nameof(DataInfo));
    }

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select signature data",
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
