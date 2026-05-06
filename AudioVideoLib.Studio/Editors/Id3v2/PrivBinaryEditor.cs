namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PrivateFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Private (PRIV)",
    Order = 34,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class PrivBinaryEditor : ITagItemEditor<Id3v2PrivateFrame>
{
    public Id3v2PrivateFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2PrivateFrame frame) => BinaryDataDialog.EditPriv(owner, frame);
}
