namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UserDefinedUrlLinkFrame),
    Category = Id3v2FrameCategory.UrlFrames,
    MenuLabel = "User-defined URL (WXXX)",
    Order = 2,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UserDefinedUrlEditor
    : ITagItemEditor<Id3v2UserDefinedUrlLinkFrame>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Description { get => field; set => Set(ref field, value); } = string.Empty;
    public string Url { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2UserDefinedUrlLinkFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2UserDefinedUrlLinkFrame frame)
    {
        Load(frame);
        var dialog = new UserDefinedUrlEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2UserDefinedUrlLinkFrame frame)
    {
        Encoding = frame.TextEncoding;
        Description = frame.Description ?? string.Empty;
        Url = frame.Url ?? string.Empty;
    }

    public void Save(Id3v2UserDefinedUrlLinkFrame frame)
    {
        frame.TextEncoding = Encoding;
        frame.Description = Description;
        frame.Url = Url;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Description))
        {
            error = "Description is required for a user-defined URL frame (WXXX).";
            return false;
        }
        if (string.IsNullOrEmpty(Url) || !Uri.TryCreate(Url, UriKind.Absolute, out _))
        {
            error = "URL must be an absolute URI (e.g. \"http://example.com/\").";
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
