namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UrlLinkFrame),
    Category = Id3v2FrameCategory.UrlFrames,
    MenuLabel = "URL frame",
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UrlFrameEditor : ObservableObject, ITagItemEditor<Id3v2UrlLinkFrame>
{
    public string Identifier { get => field; set => Set(ref field, value); } = "WCOM";
    public string Url { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2UrlLinkFrame CreateNew(object tag)
        => throw new InvalidOperationException(
            "UrlFrameEditor needs an identifier. Use CreateNew(tag, identifier) instead.");

    public Id3v2UrlLinkFrame CreateNew(object tag, string identifier)
        => new(((Id3v2Tag)tag).Version, identifier);

    public bool Edit(Window owner, Id3v2UrlLinkFrame frame)
        => EditorDialog.Run<UrlFrameEditorDialog, Id3v2UrlLinkFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2UrlLinkFrame frame)
    {
        Identifier = frame.Identifier ?? string.Empty;
        Url = frame.Url ?? string.Empty;
    }

    public void Save(Id3v2UrlLinkFrame frame)
    {
        frame.Url = Url;
    }

    public bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Url) || !Id3v2Frame.IsValidUrl(Url))
        {
            error = "URL must be a valid RFC 1738 URL (e.g. \"http://example.com/\").";
            return false;
        }
        error = null;
        return true;
    }

}
