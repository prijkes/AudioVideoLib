namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2RecommendedBufferSizeFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Recommended buffer size (RBUF)",
    Order = 35,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class RbufEditor : ITagItemEditor<Id3v2RecommendedBufferSizeFrame>, INotifyPropertyChanged
{
    public int BufferSize { get => field; set => Set(ref field, value); }
    public bool UseEmbeddedInfo { get => field; set => Set(ref field, value); }
    public int OffsetToNextTag { get => field; set => Set(ref field, value); }

    public Id3v2RecommendedBufferSizeFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2RecommendedBufferSizeFrame frame)
    {
        Load(frame);
        var dialog = new RbufEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

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
