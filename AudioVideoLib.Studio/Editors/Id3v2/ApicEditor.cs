namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2AttachedPictureFrame),
    Category = Id3v2FrameCategory.Attachments,
    MenuLabel = "Attached picture (APIC)",
    Order = 27,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class ApicEditor : ITagItemEditor<Id3v2AttachedPictureFrame>
{
    public Id3v2AttachedPictureFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2AttachedPictureFrame frame) => ApicEditorDialog.EditCore(owner, frame);
}
