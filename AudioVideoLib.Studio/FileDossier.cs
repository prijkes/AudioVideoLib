namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed class FileDossier : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public BitmapImage? CoverArt { get; private set; }

    public string HeaderTitle { get; private set; } = string.Empty;

    public string HeaderArtist { get; private set; } = string.Empty;

    public string HeaderAlbum { get; private set; } = string.Empty;

    public string HeaderYear { get; private set; } = string.Empty;

    public string HeaderSourceBadge { get; private set; } = string.Empty;

    public ObservableCollection<TechCard> TechCards { get; } = [];

    public string EncoderSummary { get; private set; } = string.Empty;

    public bool HasEncoder { get; private set; }

    public ObservableCollection<TagTabViewModel> TagTabs { get; } = [];

    public ObservableCollection<HexRegion> HexRegions { get; } = [];

    public HexRegion? SelectedHexRegion
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
            Notify(nameof(HexText));
        }
    }

    public string HexText => SelectedHexRegion?.Hex ?? string.Empty;

    public string FileSizeText { get; private set; } = string.Empty;

    public string FilePathDisplay => FilePath;

    public FileDossier(string filePath)
    {
        FilePath = filePath;
        Load();
    }

    public bool HasUnsavedChanges => TagTabs.OfType<TagTabViewModel>().Any(t => t.IsDirty);

    public void Save()
    {
        foreach (var tab in TagTabs)
        {
            switch (tab)
            {
                case Id3v2TabViewModel v2:
                    v2.CommitToTag();
                    break;
                case Id3v1TabViewModel v1:
                    v1.CommitToTag();
                    break;
            }
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

        var id3v2Tab = TagTabs.OfType<Id3v2TabViewModel>().FirstOrDefault();
        var id3v1Tab = TagTabs.OfType<Id3v1TabViewModel>().FirstOrDefault();

        var newId3v2 = id3v2Tab != null
            ? id3v2Tab.Tag.ToByteArray()
            : new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 512 }.ToByteArray();
        var newId3v1 = id3v1Tab != null
            ? id3v1Tab.Tag.ToByteArray()
            : [];

        var tmp = FilePath + ".avs-tmp";
        using (var outFile = File.Create(tmp))
        {
            outFile.Write(newId3v2, 0, newId3v2.Length);
            outFile.Write(fileBytes, (int)startOffset, (int)(endOffset - startOffset));
            if (newId3v1.Length > 0)
            {
                outFile.Write(newId3v1, 0, newId3v1.Length);
            }
        }

        File.Move(tmp, FilePath, overwrite: true);

        foreach (var tab in TagTabs)
        {
            ClearDirty(tab);
        }

        Notify(nameof(HasUnsavedChanges));
    }

    private void Load()
    {
        using var fs = File.OpenRead(FilePath);
        var fileLength = fs.Length;
        FileSizeText = FormatSize(fileLength);

        var tagOffsets = AudioTags.ReadStream(fs).ToList();

        var id3v2 = tagOffsets.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault();
        var id3v1 = tagOffsets.Select(o => o.AudioTag).OfType<Id3v1Tag>().FirstOrDefault();
        var ape = tagOffsets.Select(o => o.AudioTag).OfType<ApeTag>().FirstOrDefault();
        var lyrics3v2 = tagOffsets.Select(o => o.AudioTag).OfType<Lyrics3v2Tag>().FirstOrDefault();
        var musicMatch = tagOffsets.Select(o => o.AudioTag).OfType<MusicMatchTag>().FirstOrDefault();

        if (id3v2 != null)
        {
            TagTabs.Add(new Id3v2TabViewModel(id3v2));
        }

        if (ape != null)
        {
            TagTabs.Add(new ApeTabViewModel(ape));
        }

        if (lyrics3v2 != null)
        {
            TagTabs.Add(new Lyrics3v2TabViewModel(lyrics3v2));
        }

        if (musicMatch != null)
        {
            TagTabs.Add(new MusicMatchTabViewModel(musicMatch));
        }

        if (id3v1 != null)
        {
            TagTabs.Add(new Id3v1TabViewModel(id3v1));
        }

        if (id3v2 is { } v2)
        {
            HeaderTitle = TryText(v2, "TIT2") ?? TryText(v2, "TT2") ?? Path.GetFileNameWithoutExtension(FilePath);
            HeaderArtist = TryText(v2, "TPE1") ?? TryText(v2, "TP1") ?? string.Empty;
            HeaderAlbum = TryText(v2, "TALB") ?? TryText(v2, "TAL") ?? string.Empty;
            HeaderYear = TryText(v2, "TDRC") ?? TryText(v2, "TYER") ?? TryText(v2, "TYE") ?? string.Empty;
            HeaderSourceBadge = $"ID3{v2.Version.ToString().Replace("Id3v", "v")}";
            var apic = v2.GetFrame<Id3v2AttachedPictureFrame>();
            if (apic?.PictureData is { Length: > 0 } picData)
            {
                CoverArt = TryLoadImage(picData);
            }
        }
        else if (id3v1 is { } v1)
        {
            HeaderTitle = v1.TrackTitle ?? Path.GetFileNameWithoutExtension(FilePath);
            HeaderArtist = v1.Artist ?? string.Empty;
            HeaderAlbum = v1.AlbumTitle ?? string.Empty;
            HeaderYear = v1.AlbumYear ?? string.Empty;
            HeaderSourceBadge = "ID3v1";
        }
        else
        {
            HeaderTitle = Path.GetFileNameWithoutExtension(FilePath);
        }

        fs.Position = 0;
        var audio = AudioStreams.ReadStream(fs).FirstOrDefault();
        BuildTechCards(audio);
        BuildEncoder(audio);

        BuildHexRegions(fs, tagOffsets);

        Notify(nameof(HeaderTitle));
        Notify(nameof(HeaderArtist));
        Notify(nameof(HeaderAlbum));
        Notify(nameof(HeaderYear));
        Notify(nameof(HeaderSourceBadge));
        Notify(nameof(CoverArt));
        Notify(nameof(EncoderSummary));
        Notify(nameof(HasEncoder));
        Notify(nameof(FileSizeText));
    }

    private void BuildTechCards(IAudioStream? audio)
    {
        TechCards.Clear();
        switch (audio)
        {
            case MpaStream mpa when mpa.Frames.Any():
                {
                    var first = mpa.Frames.First();
                    TechCards.Add(new TechCard("Format", $"{first.AudioVersion} {first.LayerVersion}"));
                    TechCards.Add(new TechCard(
                        "Bitrate",
                        mpa.VbrHeader != null ? $"{first.Bitrate} kbps VBR" : $"{first.Bitrate} kbps CBR"));
                    TechCards.Add(new TechCard("Sample Rate", $"{first.SamplingRate / 1000.0:0.#} kHz"));
                    TechCards.Add(new TechCard("Channels", first.ChannelMode.ToString()));
                    TechCards.Add(new TechCard("Duration", FormatDuration(mpa.TotalAudioLength)));
                    TechCards.Add(new TechCard("Frames", mpa.Frames.Count().ToString("N0")));
                    break;
                }

            case FlacStream flac:
                {
                    var info = flac.MetadataBlocks.OfType<FlacStreamInfoMetadataBlock>().FirstOrDefault();
                    TechCards.Add(new TechCard("Format", "FLAC"));
                    if (info != null)
                    {
                        TechCards.Add(new TechCard("Sample Rate", $"{info.SampleRate / 1000.0:0.#} kHz"));
                        TechCards.Add(new TechCard("Channels", info.Channels.ToString()));
                        TechCards.Add(new TechCard("Bits", $"{info.BitsPerSample}-bit"));
                        if (info.TotalSamples > 0 && info.SampleRate > 0)
                        {
                            var seconds = (double)info.TotalSamples / info.SampleRate;
                            TechCards.Add(new TechCard("Duration", FormatDuration((long)(seconds * 1000))));
                        }
                    }
                    break;
                }

            default:
                TechCards.Add(new TechCard("Format", "Unknown"));
                break;
        }

        TechCards.Add(new TechCard("File Size", FileSizeText));
    }

    private void BuildEncoder(IAudioStream? audio)
    {
        if (audio is MpaStream mpa && mpa.VbrHeader is { } vbr)
        {
            var parts = new List<string>
            {
                $"{vbr.HeaderType}",
            };

            if (vbr.Quality != 0)
            {
                parts.Add($"q={vbr.Quality}");
            }

            if (vbr.LameTag is { } lame)
            {
                parts.Add($"LAME {lame.EncoderVersion}");
                parts.Add($"rev {lame.InfoTagRevision}");
                if (lame.LowpassFilterValue > 0)
                {
                    parts.Add($"lowpass {lame.LowpassFilterValue / 1000.0:0.#} kHz");
                }

                parts.Add($"music CRC 0x{lame.MusicCrc:X4}");
                parts.Add($"info CRC 0x{lame.InfoTagCrc:X4}");
            }

            EncoderSummary = string.Join("   •   ", parts);
            HasEncoder = true;
        }
        else
        {
            EncoderSummary = string.Empty;
            HasEncoder = false;
        }
    }

    private void BuildHexRegions(Stream stream, IReadOnlyList<IAudioTagOffset> offsets)
    {
        HexRegions.Clear();

        stream.Position = 0;
        var fullBytes = new byte[Math.Min(stream.Length, 4 * 1024)];
        var read = stream.Read(fullBytes, 0, fullBytes.Length);
        Array.Resize(ref fullBytes, read);
        HexRegions.Add(new HexRegion("File (first 4 KB)", 0, HexDumper.Dump(fullBytes, 0)));

        foreach (var offset in offsets)
        {
            var length = (int)Math.Min(offset.EndOffset - offset.StartOffset, 16 * 1024);
            if (length <= 0)
            {
                continue;
            }

            var buffer = new byte[length];
            stream.Position = offset.StartOffset;
            stream.ReadExactly(buffer, 0, length);
            var label = $"{offset.AudioTag.GetType().Name.Replace("Tag", string.Empty)} @ {offset.StartOffset:N0}";
            HexRegions.Add(new HexRegion(label, offset.StartOffset, HexDumper.Dump(buffer, offset.StartOffset)));
        }

        SelectedHexRegion = HexRegions.FirstOrDefault();
    }

    private static void ClearDirty(TagTabViewModel tab)
    {
        var prop = tab.GetType().GetProperty(nameof(TagTabViewModel.IsDirty));
        prop?.SetValue(tab, false);
    }

    private static string? TryText(Id3v2Tag tag, string identifier)
    {
        return tag.GetFrame<Id3v2TextFrame>(identifier)?.Values.FirstOrDefault();
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

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.##} MB",
        };
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

public sealed record TechCard(string Label, string Value);

public sealed record HexRegion(string Label, long StartOffset, string Hex)
{
    public override string ToString() => Label;
}
