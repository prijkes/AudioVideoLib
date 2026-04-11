namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed class AudioFileEntry : INotifyPropertyChanged
{
    private bool _suspendModified;
    private Snapshot _snapshot;

    private AudioFileEntry(string path)
    {
        FilePath = path;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string? Title
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Artist
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Album
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Year
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Track
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Genre
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Comment
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsModified
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsModified)));
        }
    }

    public BitmapImage? CoverArt { get; private set; }

    public string TechnicalInfo { get; private set; } = string.Empty;

    internal Id3v2Tag? OriginalId3v2 { get; private set; }

    internal Id3v1Tag? OriginalId3v1 { get; private set; }

    public static AudioFileEntry Load(string path)
    {
        var entry = new AudioFileEntry(path)
        {
            _suspendModified = true,
        };
        try
        {
            using var fs = File.OpenRead(path);

            var tags = AudioTags.ReadStream(fs).ToList();
            entry.OriginalId3v2 = tags
                .Where(t => t.AudioTag is Id3v2Tag)
                .Select(t => (Id3v2Tag)t.AudioTag)
                .FirstOrDefault();
            entry.OriginalId3v1 = tags
                .Where(t => t.AudioTag is Id3v1Tag)
                .Select(t => (Id3v1Tag)t.AudioTag)
                .FirstOrDefault();

            if (entry.OriginalId3v2 is { } v2)
            {
                entry.PopulateFromId3v2(v2);
            }
            else if (entry.OriginalId3v1 is { } v1)
            {
                entry.PopulateFromId3v1(v1);
            }

            entry.LoadTechnicalInfo(fs);
            entry._snapshot = entry.CaptureSnapshot();
        }
        finally
        {
            entry._suspendModified = false;
        }

        return entry;
    }

    public void Save()
    {
        if (!IsModified)
        {
            return;
        }

        var fileBytes = File.ReadAllBytes(FilePath);
        long startOffset = 0;
        long endOffset = fileBytes.Length;

        using (var ms = new MemoryStream(fileBytes))
        {
            var tags = AudioTags.ReadStream(ms).ToList();
            var id3v2End = tags
                .Where(t => t.AudioTag is Id3v2Tag && t.TagOrigin == TagOrigin.Start)
                .OrderByDescending(t => t.EndOffset)
                .FirstOrDefault();
            var id3v1Start = tags
                .Where(t => t.AudioTag is Id3v1Tag && t.TagOrigin == TagOrigin.End)
                .OrderBy(t => t.StartOffset)
                .FirstOrDefault();

            if (id3v2End != null)
            {
                startOffset = id3v2End.EndOffset;
            }

            if (id3v1Start != null)
            {
                endOffset = id3v1Start.StartOffset;
            }
        }

        if (startOffset < 0 || endOffset < startOffset || endOffset > fileBytes.Length)
        {
            throw new InvalidDataException("Unable to determine audio region for save.");
        }

        var newId3v2 = BuildId3v2().ToByteArray();
        var newId3v1 = BuildId3v1().ToByteArray();

        var tmp = FilePath + ".avs-tmp";
        using (var outFile = File.Create(tmp))
        {
            outFile.Write(newId3v2, 0, newId3v2.Length);
            outFile.Write(fileBytes, (int)startOffset, (int)(endOffset - startOffset));
            outFile.Write(newId3v1, 0, newId3v1.Length);
        }

        File.Move(tmp, FilePath, overwrite: true);

        _snapshot = CaptureSnapshot();
        IsModified = false;
    }

    public void Revert()
    {
        if (!IsModified)
        {
            return;
        }

        _suspendModified = true;
        try
        {
            Title = _snapshot.Title;
            Artist = _snapshot.Artist;
            Album = _snapshot.Album;
            Year = _snapshot.Year;
            Track = _snapshot.Track;
            Genre = _snapshot.Genre;
            Comment = _snapshot.Comment;
        }
        finally
        {
            _suspendModified = false;
        }

        IsModified = false;
    }

    private void PopulateFromId3v2(Id3v2Tag tag)
    {
        Title = GetText(tag, "TIT2") ?? GetText(tag, "TT2");
        Artist = GetText(tag, "TPE1") ?? GetText(tag, "TP1");
        Album = GetText(tag, "TALB") ?? GetText(tag, "TAL");
        Year = GetText(tag, "TDRC") ?? GetText(tag, "TYER") ?? GetText(tag, "TYE");
        Track = GetText(tag, "TRCK") ?? GetText(tag, "TRK");
        Genre = GetText(tag, "TCON") ?? GetText(tag, "TCO");
        Comment = tag.GetFrame<Id3v2CommentFrame>()?.Text;

        var apic = tag.GetFrame<Id3v2AttachedPictureFrame>();
        if (apic?.PictureData is { Length: > 0 } data)
        {
            CoverArt = TryLoadImage(data);
        }
    }

    private void PopulateFromId3v1(Id3v1Tag tag)
    {
        Title = tag.TrackTitle;
        Artist = tag.Artist;
        Album = tag.AlbumTitle;
        Year = tag.AlbumYear;
        Track = tag.TrackNumber > 0 ? tag.TrackNumber.ToString() : null;
        Genre = tag.Genre.ToString();
        Comment = tag.TrackComment;
    }

    private void LoadTechnicalInfo(Stream stream)
    {
        try
        {
            stream.Position = 0;
            var audio = AudioStreams.ReadStream(stream).FirstOrDefault();
            switch (audio)
            {
                case MpaStream mpa when mpa.Frames.Any():
                    {
                        var first = mpa.Frames.First();
                        var ts = TimeSpan.FromMilliseconds(mpa.TotalAudioLength);
                        var vbr = mpa.VbrHeader != null ? " VBR" : string.Empty;
                        TechnicalInfo =
                            $"{first.AudioVersion} {first.LayerVersion}, {first.SamplingRate} Hz, " +
                            $"{first.Bitrate} kbps{vbr}, {first.ChannelMode}\n" +
                            $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                        break;
                    }

                case FlacStream flac:
                    {
                        var info = flac.MetadataBlocks.OfType<FlacStreamInfoMetadataBlock>().FirstOrDefault();
                        TechnicalInfo = info != null
                            ? $"FLAC, {info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit"
                            : "FLAC";
                        break;
                    }

                default:
                    TechnicalInfo = string.Empty;
                    break;
            }
        }
        catch
        {
            TechnicalInfo = string.Empty;
        }
    }

    private Id3v2Tag BuildId3v2()
    {
        var tag = OriginalId3v2 is { Version: >= Id3v2Version.Id3v230 } preserved
            ? preserved
            : new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 512 };

        var yearId = tag.Version >= Id3v2Version.Id3v240 ? "TDRC" : "TYER";

        SetOrRemoveText(tag, "TIT2", Title);
        SetOrRemoveText(tag, "TPE1", Artist);
        SetOrRemoveText(tag, "TALB", Album);
        SetOrRemoveText(tag, yearId, Year);
        SetOrRemoveText(tag, "TRCK", Track);
        SetOrRemoveText(tag, "TCON", Genre);

        if (!string.IsNullOrEmpty(Comment))
        {
            var existing = tag.GetFrame<Id3v2CommentFrame>();
            if (existing != null)
            {
                existing.TextEncoding = Id3v2FrameEncodingType.UTF8;
                if (string.IsNullOrEmpty(existing.Language))
                {
                    existing.Language = "eng";
                }

                existing.Text = Comment;
            }
            else
            {
                var comm = new Id3v2CommentFrame(tag.Version)
                {
                    TextEncoding = Id3v2FrameEncodingType.UTF8,
                    Language = "eng",
                    ShortContentDescription = string.Empty,
                    Text = Comment,
                };
                tag.SetFrame(comm);
            }
        }
        else
        {
            var existing = tag.GetFrame<Id3v2CommentFrame>();
            if (existing != null)
            {
                tag.RemoveFrame(existing);
            }
        }

        return tag;
    }

    private Id3v1Tag BuildId3v1()
    {
        var tag = OriginalId3v1 ?? new Id3v1Tag(Id3v1Version.Id3v11);
        tag.TrackTitle = Title ?? string.Empty;
        tag.Artist = Artist ?? string.Empty;
        tag.AlbumTitle = Album ?? string.Empty;
        tag.AlbumYear = Year ?? string.Empty;
        tag.TrackComment = Comment ?? string.Empty;

        if (byte.TryParse(Track?.Split('/').FirstOrDefault(), out var tn))
        {
            tag.TrackNumber = tn;
        }

        if (!string.IsNullOrEmpty(Genre) && Enum.TryParse<Id3v1Genre>(Genre, true, out var g))
        {
            tag.Genre = g;
        }

        return tag;
    }

    private static void SetOrRemoveText(Id3v2Tag tag, string identifier, string? value)
    {
        var existing = tag.GetFrame<Id3v2TextFrame>(identifier);
        if (string.IsNullOrEmpty(value))
        {
            if (existing != null)
            {
                tag.RemoveFrame(existing);
            }

            return;
        }

        if (existing != null)
        {
            existing.Values.Clear();
            existing.TextEncoding = Id3v2FrameEncodingType.UTF8;
            existing.Values.Add(value);
        }
        else
        {
            var frame = new Id3v2TextFrame(tag.Version, identifier)
            {
                TextEncoding = Id3v2FrameEncodingType.UTF8,
            };
            frame.Values.Add(value);
            tag.SetFrame(frame);
        }
    }

    private static string? GetText(Id3v2Tag tag, string identifier)
    {
        var frame = tag.GetFrame<Id3v2TextFrame>(identifier);
        return frame?.Values.FirstOrDefault();
    }

    private static BitmapImage? TryLoadImage(byte[] data)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = new MemoryStream(data);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch
        {
            return null;
        }
    }

    private Snapshot CaptureSnapshot()
    {
        return new Snapshot(Title, Artist, Album, Year, Track, Genre, Comment);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (!_suspendModified)
        {
            IsModified = true;
        }
    }

    private readonly record struct Snapshot(
        string? Title,
        string? Artist,
        string? Album,
        string? Year,
        string? Track,
        string? Genre,
        string? Comment);
}
