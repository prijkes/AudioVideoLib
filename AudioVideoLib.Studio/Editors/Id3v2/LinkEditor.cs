namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2LinkedInformationFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Linked information",
    Order = 18,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class LinkEditor : ObservableObject, ITagItemEditor<Id3v2LinkedInformationFrame>, IValidatedEditor
{
    private Id3v2Version _version = Id3v2Version.Id3v240;

    public string FrameIdentifier { get => field; set => Set(ref field, value); } = string.Empty;
    public string Url { get => field; set => Set(ref field, value); } = string.Empty;
    public string AdditionalIdData { get => field; set => Set(ref field, value); } = string.Empty;

    public Id3v2LinkedInformationFrame CreateNew(object tag)
    {
        var version = ((Id3v2Tag)tag).Version;
        // The frameIdentifier ctor arg is the LINKED frame's identifier (the frame this
        // LINK refers to). Default to a recognisable text-frame identifier so the user
        // sees something sensible in the dialog and overwrites it. The user then edits
        // the FrameIdentifier field to point at the actually-linked frame's id.
        var defaultLinkedId = version < Id3v2Version.Id3v230 ? "TT2" : "TIT2";
        return new Id3v2LinkedInformationFrame(version, defaultLinkedId);
    }

    public bool Edit(Window owner, Id3v2LinkedInformationFrame frame)
        => EditorDialog.Run<LinkEditorDialog, Id3v2LinkedInformationFrame>(
            owner, frame, this, Load, Save,
            d => d.IdentifierBox.MaxLength = Id3v2Frame.GetIdentifierFieldLength(_version));

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
        var expectedLength = Id3v2Frame.GetIdentifierFieldLength(_version);
        if (FrameIdentifier?.Length != expectedLength)
        {
            error = $"Frame identifier must be {expectedLength} characters for ID3v2.{(int)_version / 10}.";
            return false;
        }
        if (string.IsNullOrEmpty(Url) || !Id3v2Frame.IsValidUrl(Url))
        {
            error = "URL must be a valid RFC 1738 URL (e.g. \"http://example.com/\").";
            return false;
        }
        error = null;
        return true;
    }

}
