namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2RecommendedBufferSizeFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Recommended buffer size",
    Order = 35,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class RbufEditor : ObservableObject, ITagItemEditor<Id3v2RecommendedBufferSizeFrame>, IValidatedEditor
{
    public int BufferSize { get => field; set => Set(ref field, value); }
    public bool UseEmbeddedInfo { get => field; set => Set(ref field, value); }
    public int OffsetToNextTag { get => field; set => Set(ref field, value); }

    public Id3v2RecommendedBufferSizeFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2RecommendedBufferSizeFrame frame)
        => EditorDialog.Run<RbufEditorDialog, Id3v2RecommendedBufferSizeFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2RecommendedBufferSizeFrame f)
    {
        BufferSize = f.BufferSize;
        UseEmbeddedInfo = f.UseEmbeddedInfo;
        OffsetToNextTag = f.OffsetToNextTag;
    }

    public void Save(Id3v2RecommendedBufferSizeFrame f)
    {
        f.BufferSize = BufferSize;
        f.UseEmbeddedInfo = UseEmbeddedInfo;
        f.OffsetToNextTag = OffsetToNextTag;
    }

    public bool Validate(out string? error)
    {
        if (BufferSize < 0)
        {
            error = "Buffer size must be non-negative.";
            return false;
        }
        if (OffsetToNextTag < 0)
        {
            error = "Offset to next tag must be non-negative.";
            return false;
        }
        error = null;
        return true;
    }

}
