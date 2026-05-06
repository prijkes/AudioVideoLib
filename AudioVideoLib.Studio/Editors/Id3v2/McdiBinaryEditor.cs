namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2MusicCdIdentifierFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Music CD identifier (MCDI)",
    Order = 6,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class McdiBinaryEditor : ITagItemEditor<Id3v2MusicCdIdentifierFrame>
{
    public Id3v2MusicCdIdentifierFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2MusicCdIdentifierFrame frame) => BinaryDataDialog.EditMcdi(owner, frame);
}
