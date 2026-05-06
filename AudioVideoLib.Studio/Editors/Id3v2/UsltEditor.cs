namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2UnsynchronizedLyricsFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Unsynchronized lyrics (USLT)",
    Order = 11,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class UsltEditor : ITagItemEditor<Id3v2UnsynchronizedLyricsFrame>
{
    public Id3v2UnsynchronizedLyricsFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2UnsynchronizedLyricsFrame frame) => UsltEditorDialog.EditCore(owner, frame);
}
