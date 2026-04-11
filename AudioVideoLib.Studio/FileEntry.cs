namespace AudioVideoLib.Studio;

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed class FileEntry : INotifyPropertyChanged
{
    private FileEntry(string filePath)
    {
        FilePath = filePath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string DisplayTitle { get; private set; } = string.Empty;

    public string DisplayArtist { get; private set; } = string.Empty;

    public string DurationText { get; private set; } = string.Empty;

    public int BitrateKbps { get; private set; }

    public string BitrateMode { get; private set; } = string.Empty;

    public bool IsModified
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Notify();
        }
    }

    public static FileEntry Load(string path)
    {
        var entry = new FileEntry(path);
        entry.Refresh();
        return entry;
    }

    public void Refresh()
    {
        try
        {
            using var fs = File.OpenRead(FilePath);

            var id3v2 = AudioTags.ReadStream(fs).Select(t => t.AudioTag).OfType<Id3v2Tag>().FirstOrDefault();
            if (id3v2 != null)
            {
                DisplayTitle = id3v2.GetFrame<Id3v2TextFrame>("TIT2")?.Values.FirstOrDefault()
                            ?? id3v2.GetFrame<Id3v2TextFrame>("TT2")?.Values.FirstOrDefault()
                            ?? Path.GetFileNameWithoutExtension(FilePath);
                DisplayArtist = id3v2.GetFrame<Id3v2TextFrame>("TPE1")?.Values.FirstOrDefault()
                             ?? id3v2.GetFrame<Id3v2TextFrame>("TP1")?.Values.FirstOrDefault()
                             ?? string.Empty;
            }
            else
            {
                fs.Position = 0;
                var id3v1 = AudioTags.ReadStream(fs).Select(t => t.AudioTag).OfType<Id3v1Tag>().FirstOrDefault();
                DisplayTitle = id3v1?.TrackTitle ?? Path.GetFileNameWithoutExtension(FilePath);
                DisplayArtist = id3v1?.Artist ?? string.Empty;
            }

            fs.Position = 0;
            var audio = AudioStreams.ReadStream(fs).FirstOrDefault();
            switch (audio)
            {
                case MpaStream mpa when mpa.Frames.Any():
                    {
                        var first = mpa.Frames.First();
                        BitrateKbps = first.Bitrate;
                        BitrateMode = mpa.VbrHeader != null ? "VBR" : "CBR";
                        DurationText = FormatDuration(mpa.TotalAudioLength);
                        break;
                    }

                case FlacStream:
                    {
                        BitrateKbps = 0;
                        BitrateMode = "FLAC";
                        DurationText = string.Empty;
                        break;
                    }

                default:
                    BitrateKbps = 0;
                    BitrateMode = string.Empty;
                    DurationText = string.Empty;
                    break;
            }
        }
        catch
        {
            DisplayTitle = Path.GetFileNameWithoutExtension(FilePath);
            DisplayArtist = string.Empty;
        }

        Notify(nameof(DisplayTitle));
        Notify(nameof(DisplayArtist));
        Notify(nameof(DurationText));
        Notify(nameof(BitrateKbps));
        Notify(nameof(BitrateMode));
    }

    private static string FormatDuration(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }

    private void Notify([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
