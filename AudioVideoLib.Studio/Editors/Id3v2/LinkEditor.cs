namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2LinkedInformationFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Linked information (LINK)",
    Order = 18,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class LinkEditor : ITagItemEditor<Id3v2LinkedInformationFrame>, INotifyPropertyChanged
{
    private Id3v2Version _version = Id3v2Version.Id3v240;

    public string FrameIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public string Url { get => field; set => Set(ref field, value); } = string.Empty;
    public string AdditionalIdData { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2LinkedInformationFrame CreateNew(object tag)
    {
        var version = ((Id3v2Tag)tag).Version;
        var defaultId = version < Id3v2Version.Id3v230 ? "WCM" : "WCOM";
        return new Id3v2LinkedInformationFrame(version, defaultId);
    }

    public bool Edit(Window owner, Id3v2LinkedInformationFrame frame)
    {
        Load(frame);
        var dialog = new LinkEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2LinkedInformationFrame f)
    {
        _version = f.Version;
        FrameIdentifier = f.FrameIdentifier ?? string.Empty;
        Url = f.Url ?? string.Empty;
        AdditionalIdData = f.AdditionalIdData ?? string.Empty;
    }

    public void Save(Id3v2LinkedInformationFrame f)
    {
        f.FrameIdentifier = FrameIdentifier;
        f.Url = Url;
        f.AdditionalIdData = AdditionalIdData;
    }

    public bool Validate(out string? error)
    {
        var expectedLength = _version < Id3v2Version.Id3v230 ? 3 : 4;
        if (FrameIdentifier?.Length != expectedLength)
        {
            error = $"Frame identifier must be {expectedLength} characters for ID3v2.{(int)_version / 10}.";
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
