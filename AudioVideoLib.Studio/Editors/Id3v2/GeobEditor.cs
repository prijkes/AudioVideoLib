namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Microsoft.Win32;

[Id3v2FrameEditor(typeof(Id3v2GeneralEncapsulatedObjectFrame),
    Category = Id3v2FrameCategory.Attachments,
    MenuLabel = "General encapsulated object (GEOB)",
    Order = 28,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class GeobEditor : ITagItemEditor<Id3v2GeneralEncapsulatedObjectFrame>, INotifyPropertyChanged
{
    private byte[] _encapsulatedObject = [];

    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string MimeType { get => field; set => Set(ref field, value); } = string.Empty;
    public string Filename { get => field; set => Set(ref field, value); } = string.Empty;
    public string ContentDescription { get => field; set => Set(ref field, value); } = string.Empty;

    public string DataInfo => _encapsulatedObject.Length == 0
        ? "(no data)"
        : $"{_encapsulatedObject.Length:N0} bytes";

    public Id3v2GeneralEncapsulatedObjectFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2GeneralEncapsulatedObjectFrame frame)
    {
        Load(frame);
        var dialog = new GeobEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2GeneralEncapsulatedObjectFrame f)
    {
        Encoding = f.TextEncoding;
        MimeType = f.MimeType ?? string.Empty;
        Filename = f.Filename ?? string.Empty;
        ContentDescription = f.ContentDescription ?? string.Empty;
        _encapsulatedObject = f.EncapsulatedObject ?? [];
        OnPropertyChanged(nameof(DataInfo));
    }

    public void Save(Id3v2GeneralEncapsulatedObjectFrame f)
    {
        f.TextEncoding = Encoding;
        f.MimeType = MimeType;
        f.Filename = Filename;
        f.ContentDescription = ContentDescription;
        f.EncapsulatedObject = _encapsulatedObject;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(ContentDescription))
        {
            error = "Content description must not be empty.";
            return false;
        }
        if (!string.IsNullOrEmpty(MimeType) && !MimeType.Contains('/'))
        {
            error = "MIME type must be of the form <type>/<subtype>.";
            return false;
        }
        error = null;
        return true;
    }

    public void LoadDataFromFile(string path)
    {
        _encapsulatedObject = File.ReadAllBytes(path);
        OnPropertyChanged(nameof(DataInfo));
    }

    public void ClearData()
    {
        _encapsulatedObject = [];
        OnPropertyChanged(nameof(DataInfo));
    }

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select encapsulated object",
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
