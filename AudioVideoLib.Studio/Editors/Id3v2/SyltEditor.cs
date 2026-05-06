namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class SyltRowVm : INotifyPropertyChanged
{
    public string Syllable { get => field; set => Set(ref field, value); } = string.Empty;
    public int TimeStamp { get => field; set => Set(ref field, value); }

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

[Id3v2FrameEditor(typeof(Id3v2SynchronizedLyricsFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Synchronized lyrics (SYLT)",
    Order = 12,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class SyltEditor
    : CollectionEditorBase<Id3v2SynchronizedLyricsFrame, SyltRowVm>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }
    public string Language { get => field; set => Set(ref field, value); } = string.Empty;
    public Id3v2TimeStampFormat TimeStampFormat { get => field; set => Set(ref field, value); }
    public Id3v2ContentType ContentType { get => field; set => Set(ref field, value); }
    public string ContentDescriptor { get => field; set => Set(ref field, value); } = string.Empty;

    public override Id3v2SynchronizedLyricsFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2SynchronizedLyricsFrame frame)
    {
        Load(frame);
        var dialog = new SyltEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2SynchronizedLyricsFrame f)
    {
        Encoding = f.TextEncoding;
        Language = f.Language ?? string.Empty;
        TimeStampFormat = f.TimeStampFormat;
        ContentType = f.ContentType;
        ContentDescriptor = f.ContentDescriptor ?? string.Empty;
        LoadRows(f);
    }

    public void Save(Id3v2SynchronizedLyricsFrame f)
    {
        // Clear lyric syncs first so the encoding setter doesn't reject existing entries.
        f.LyricSyncs.Clear();
        f.ContentDescriptor = string.Empty;
        f.TextEncoding = Encoding;
        f.Language = Language;
        f.TimeStampFormat = TimeStampFormat;
        f.ContentType = ContentType;
        f.ContentDescriptor = ContentDescriptor;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2SynchronizedLyricsFrame f)
    {
        Entries.Clear();
        foreach (var s in f.LyricSyncs)
        {
            Entries.Add(new SyltRowVm { Syllable = s.Syllable, TimeStamp = s.TimeStamp });
        }
    }

    public override void SaveRows(Id3v2SynchronizedLyricsFrame f)
    {
        f.LyricSyncs.Clear();
        foreach (var r in Entries)
        {
            f.LyricSyncs.Add(new Id3v2LyricSync(r.Syllable ?? string.Empty, r.TimeStamp));
        }
    }

    public override bool Validate(out string? error)
    {
        if (string.IsNullOrEmpty(Language) || Language.Length != 3)
        {
            error = "Language must be a 3-character ISO-639-2 code (e.g. \"eng\").";
            return false;
        }
        var prev = int.MinValue;
        foreach (var r in Entries)
        {
            if (r.TimeStamp < prev)
            {
                error = "Lyric syncs must be in monotonic non-decreasing timestamp order.";
                return false;
            }
            prev = r.TimeStamp;
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
