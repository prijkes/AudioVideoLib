namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UserDefinedTextInformationFrame),
    Category = Id3v2FrameCategory.TextFrames,
    MenuLabel = "User-defined text (TXXX)",
    Order = 2,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UserDefinedTextEditor
    : ITagItemEditor<Id3v2UserDefinedTextInformationFrame>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Description { get => field; set => Set(ref field, value); } = string.Empty;
    public string Value { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2UserDefinedTextInformationFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2UserDefinedTextInformationFrame frame)
    {
        Load(frame);
        var dialog = new UserDefinedTextEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2UserDefinedTextInformationFrame frame)
    {
        Encoding = frame.TextEncoding;
        Description = frame.Description ?? string.Empty;
        Value = frame.Value ?? string.Empty;
    }

    public void Save(Id3v2UserDefinedTextInformationFrame frame)
    {
        frame.TextEncoding = Encoding;
        frame.Description = Description;
        frame.Value = Value;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Description))
        {
            error = "Description is required for a user-defined text frame (TXXX).";
            return false;
        }
        error = null;
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
