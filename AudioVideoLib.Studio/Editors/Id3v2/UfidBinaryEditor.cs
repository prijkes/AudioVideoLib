namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UniqueFileIdentifierFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Unique file identifier (UFID)",
    Order = 5,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UfidBinaryEditor : ITagItemEditor<Id3v2UniqueFileIdentifierFrame>
{
    public Id3v2UniqueFileIdentifierFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2UniqueFileIdentifierFrame frame) => BinaryDataDialog.EditUfid(owner, frame);
}
