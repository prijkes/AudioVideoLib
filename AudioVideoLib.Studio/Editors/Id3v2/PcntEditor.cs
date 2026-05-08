namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PlayCounterFrame),
    Category = Id3v2FrameCategory.CountersAndRatings,
    MenuLabel = "Play counter",
    Order = 25,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class PcntEditor : ObservableObject, ITagItemEditor<Id3v2PlayCounterFrame>, IValidatedEditor
{
    public long Counter { get => field; set => Set(ref field, value); }

    public Id3v2PlayCounterFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2PlayCounterFrame frame)
        => EditorDialog.Run<PcntEditorDialog, Id3v2PlayCounterFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2PlayCounterFrame f) => Counter = f.Counter;

    public void Save(Id3v2PlayCounterFrame f) => f.Counter = Counter;

    public bool Validate(out string? error)
    {
        if (Counter < 0)
        {
            error = "Counter must be non-negative.";
            return false;
        }
        error = null;
        return true;
    }

}
