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

public enum TagKind
{
    Id3v2,
    Id3v1,
    Ape,
    Lyrics3v2,
    MusicMatch,
}

public sealed class FileDossier : INotifyPropertyChanged
{
    private List<IAudioTagOffset> _offsets = [];
    private readonly HashSet<IAudioTag> _newTags = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IAudioTag> _removedTags = new(ReferenceEqualityComparer.Instance);
    private FlacStream? _flacStream;
    private bool _isFlac;

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

    public bool HasUnsavedChanges =>
        TagTabs.Any(t => t.IsDirty) ||
        _newTags.Count > 0 ||
        _removedTags.Count > 0;

    public IReadOnlyList<TagKind> AddableTagKinds
    {
        get
        {
            var present = new HashSet<TagKind>();
            foreach (var tab in TagTabs)
            {
                switch (tab)
                {
                    case Id3v2TabViewModel: present.Add(TagKind.Id3v2); break;
                    case Id3v1TabViewModel: present.Add(TagKind.Id3v1); break;
                    case ApeTabViewModel: present.Add(TagKind.Ape); break;
                    case Lyrics3v2TabViewModel: present.Add(TagKind.Lyrics3v2); break;
                    case MusicMatchTabViewModel: present.Add(TagKind.MusicMatch); break;
                }
            }

            return [.. Enum.GetValues<TagKind>().Where(k => !present.Contains(k))];
        }
    }

    public TagTabViewModel AddNewTag(TagKind kind)
    {
        TagTabViewModel vm;
        IAudioTag tag;
        switch (kind)
        {
            case TagKind.Id3v2:
                var id3v2 = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 512 };
                vm = new Id3v2TabViewModel(id3v2);
                tag = id3v2;
                break;
            case TagKind.Id3v1:
                var id3v1 = new Id3v1Tag(Id3v1Version.Id3v11);
                vm = new Id3v1TabViewModel(id3v1);
                tag = id3v1;
                break;
            case TagKind.Ape:
                var ape = new ApeTag(ApeVersion.Version2)
                {
                    UseHeader = true,
                    UseFooter = true,
                };
                vm = new ApeTabViewModel(ape);
                tag = ape;
                break;
            case TagKind.Lyrics3v2:
                var lyrics = new Lyrics3v2Tag();
                vm = new Lyrics3v2TabViewModel(lyrics);
                tag = lyrics;
                break;
            case TagKind.MusicMatch:
                var mm = new MusicMatchTag(3, 100);
                vm = new MusicMatchTabViewModel(mm);
                tag = mm;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        _newTags.Add(tag);
        vm.MarkDirty();
        TagTabs.Add(vm);
        Notify(nameof(AddableTagKinds));
        Notify(nameof(HasUnsavedChanges));
        return vm;
    }

    public bool RemoveTag(TagTabViewModel tab)
    {
        if (tab == null || !TagTabs.Contains(tab))
        {
            return false;
        }

        IAudioTag? underlying = tab switch
        {
            Id3v2TabViewModel v2 => v2.Tag,
            Id3v1TabViewModel v1 => v1.Tag,
            ApeTabViewModel ape => ape.Tag,
            Lyrics3v2TabViewModel l3 => l3.Tag,
            MusicMatchTabViewModel mm => mm.Tag,
            _ => null,
        };

        if (underlying != null)
        {
            if (_newTags.Contains(underlying))
            {
                // Brand new, never saved — just forget about it.
                _newTags.Remove(underlying);
            }
            else
            {
                // Previously persisted — track it so the save path skips it.
                _removedTags.Add(underlying);
            }
        }

        TagTabs.Remove(tab);
        Notify(nameof(AddableTagKinds));
        Notify(nameof(HasUnsavedChanges));
        return true;
    }

    public static string GetKindLabel(TagKind kind) => kind switch
    {
        TagKind.Id3v2     => "ID3v2.4 (file start)",
        TagKind.Id3v1     => "ID3v1.1 (file end, 128 bytes)",
        TagKind.Ape       => "APEv2 (file end)",
        TagKind.Lyrics3v2 => "Lyrics3v2 (file end)",
        TagKind.MusicMatch => "MusicMatch (file end)",
        _                 => kind.ToString(),
    };

    public void Save()
    {
        if (_isFlac)
        {
            SaveFlac();
        }
        else
        {
            SaveMp3Style();
        }
    }

    private void SaveMp3Style()
    {
        // Commit editable tab VMs back to their library tag instances, and snapshot
        // which tag instances have unsaved edits so we know which regions to
        // re-serialize vs preserve byte-for-byte.
        var dirtyTags = new HashSet<IAudioTag>(ReferenceEqualityComparer.Instance);
        foreach (var tab in TagTabs)
        {
            if (!tab.IsDirty)
            {
                continue;
            }

            switch (tab)
            {
                case Id3v2TabViewModel v2:
                    v2.CommitToTag();
                    dirtyTags.Add(v2.Tag);
                    break;
                case Id3v1TabViewModel v1:
                    v1.CommitToTag();
                    dirtyTags.Add(v1.Tag);
                    break;
                case ApeTabViewModel ape:
                    dirtyTags.Add(ape.Tag);
                    break;
                case Lyrics3v2TabViewModel l3:
                    dirtyTags.Add(l3.Tag);
                    break;
            }
        }

        var fileBytes = File.ReadAllBytes(FilePath);

        var existingStart = _offsets
            .Where(o => o.TagOrigin == TagOrigin.Start && !_removedTags.Contains(o.AudioTag))
            .OrderBy(o => o.StartOffset)
            .ToList();
        var existingEnd = _offsets
            .Where(o => o.TagOrigin == TagOrigin.End && !_removedTags.Contains(o.AudioTag))
            .OrderBy(o => o.StartOffset)
            .ToList();

        // For audio-region boundaries we still need the ORIGINAL start/end
        // positions — even a removed tag contributed bytes to the original file,
        // so the audio middle starts where the (possibly removed) start tags
        // ended and ends where the (possibly removed) end tags began.
        var allStartOffsets = _offsets
            .Where(o => o.TagOrigin == TagOrigin.Start)
            .ToList();
        var allEndOffsets = _offsets
            .Where(o => o.TagOrigin == TagOrigin.End)
            .ToList();

        var audioStart = allStartOffsets.Count > 0 ? allStartOffsets.Max(o => o.EndOffset) : 0L;
        var audioEnd = allEndOffsets.Count > 0 ? allEndOffsets.Min(o => o.StartOffset) : fileBytes.Length;

        if (audioStart < 0 || audioEnd < audioStart || audioEnd > fileBytes.Length)
        {
            throw new InvalidDataException("Unable to determine audio region for save.");
        }

        // Build write lists with a priority so new tags interleave with existing
        // tags in the right order — e.g. a new APE added to a file that already
        // ends in Id3v1 slots in *before* the Id3v1, not after it.
        var startWrites = BuildWriteList(existingStart, _newTags.Where(IsStartOrigin));
        var endWrites = BuildWriteList(existingEnd, _newTags.Where(IsEndOrigin));

        var tmp = FilePath + ".avs-tmp";
        using (var outFile = File.Create(tmp))
        {
            foreach (var item in startWrites)
            {
                WriteWriteItem(outFile, item, fileBytes, dirtyTags);
            }

            outFile.Write(fileBytes, (int)audioStart, (int)(audioEnd - audioStart));

            foreach (var item in endWrites)
            {
                WriteWriteItem(outFile, item, fileBytes, dirtyTags);
            }
        }

        File.Move(tmp, FilePath, overwrite: true);

        _newTags.Clear();
        _removedTags.Clear();

        foreach (var tab in TagTabs)
        {
            tab.ResetDirty();
        }

        using (var fs = File.OpenRead(FilePath))
        {
            _offsets = [.. AudioTags.ReadStream(fs).OfType<IAudioTagOffset>()];
        }

        Notify(nameof(HasUnsavedChanges));
        Notify(nameof(AddableTagKinds));
    }

    private static List<WriteItem> BuildWriteList(
        IReadOnlyList<IAudioTagOffset> existing,
        IEnumerable<IAudioTag> newTags)
    {
        var list = new List<WriteItem>(existing.Count);
        foreach (var offset in existing)
        {
            list.Add(new WriteItem(offset, null, PriorityOf(offset.AudioTag)));
        }

        foreach (var newTag in newTags.OrderBy(PriorityOf))
        {
            var priority = PriorityOf(newTag);
            var insertAt = list.FindIndex(w => w.Priority > priority);
            var item = new WriteItem(null, newTag, priority);
            if (insertAt < 0)
            {
                list.Add(item);
            }
            else
            {
                list.Insert(insertAt, item);
            }
        }

        return list;
    }

    private static void WriteWriteItem(Stream output, WriteItem item, byte[] originalBytes, HashSet<IAudioTag> dirtyTags)
    {
        if (item.NewTag is { } newTag)
        {
            var bytes = newTag.ToByteArray();
            output.Write(bytes, 0, bytes.Length);
            return;
        }

        if (item.Offset is { } offset)
        {
            WriteTagRegion(output, offset, originalBytes, dirtyTags);
        }
    }

    private void SaveFlac()
    {
        if (_flacStream == null)
        {
            throw new InvalidOperationException("FLAC stream not loaded.");
        }

        // The Vorbis block's Data getter re-serializes from its VorbisComments
        // property on every read, so mutations via VorbisTabViewModel are
        // automatically reflected when ToByteArray() is called below.

        var fileBytes = File.ReadAllBytes(FilePath);
        var audioStart = _flacStream.StartOffset;
        if (audioStart < 4)
        {
            throw new InvalidDataException("FLAC audio stream offset is invalid.");
        }

        var blocks = _flacStream.MetadataBlocks.ToList();
        if (blocks.Count == 0)
        {
            throw new InvalidDataException("FLAC file has no metadata blocks.");
        }

        // Recompute the last-block marker: exactly the final block in the list
        // gets bit 7 set, all others clear it.
        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].IsLastBlock = i == blocks.Count - 1;
        }

        var tmp = FilePath + ".avs-tmp";
        using (var outFile = File.Create(tmp))
        {
            // "fLaC" marker — 4 bytes at offset 0.
            outFile.Write(fileBytes, 0, 4);

            foreach (var block in blocks)
            {
                var bytes = block.ToByteArray();
                outFile.Write(bytes, 0, bytes.Length);
            }

            // Audio frames — everything from the first frame's offset to EOF.
            outFile.Write(fileBytes, (int)audioStart, fileBytes.Length - (int)audioStart);
        }

        File.Move(tmp, FilePath, overwrite: true);

        foreach (var tab in TagTabs)
        {
            tab.ResetDirty();
        }

        // Re-read the FlacStream so _flacStream references the fresh blocks.
        using (var fs = File.OpenRead(FilePath))
        {
            _flacStream = AudioStreams.ReadStream(fs).OfType<FlacStream>().FirstOrDefault();
        }

        Notify(nameof(HasUnsavedChanges));
    }

    private static bool IsStartOrigin(IAudioTag tag) => tag is Id3v2Tag;

    private static bool IsEndOrigin(IAudioTag tag) => tag is Id3v1Tag or ApeTag or Lyrics3v2Tag or MusicMatchTag;

    // File-layout convention: end tags fall in this order on disk, with Id3v1 always last.
    private static int PriorityOf(IAudioTag tag) => tag switch
    {
        Id3v2Tag => 0,
        MusicMatchTag => 100,
        Lyrics3v2Tag => 200,
        ApeTag => 300,
        Id3v1Tag => 1000,
        _ => 500,
    };

    private readonly record struct WriteItem(IAudioTagOffset? Offset, IAudioTag? NewTag, int Priority);

    private static void WriteTagRegion(Stream output, IAudioTagOffset offset, byte[] originalBytes, HashSet<IAudioTag> dirtyTags)
    {
        byte[] bytes;
        if (dirtyTags.Contains(offset.AudioTag))
        {
            // Re-serialize from the edited library-level tag.
            bytes = offset.AudioTag.ToByteArray();
        }
        else
        {
            // Preserve byte-for-byte from the original file — avoids round-trip
            // drift for tag formats the library can read but not perfectly re-emit.
            var length = (int)(offset.EndOffset - offset.StartOffset);
            bytes = new byte[length];
            Buffer.BlockCopy(originalBytes, (int)offset.StartOffset, bytes, 0, length);
        }

        output.Write(bytes, 0, bytes.Length);
    }

    private void Load()
    {
        using var fs = File.OpenRead(FilePath);
        var fileLength = fs.Length;
        FileSizeText = FormatSize(fileLength);

        _offsets = [.. AudioTags.ReadStream(fs).OfType<IAudioTagOffset>()];
        var tagOffsets = _offsets;

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
        _flacStream = audio as FlacStream;
        _isFlac = _flacStream != null;
        BuildTechCards(audio);
        BuildEncoder(audio);

        if (_flacStream != null)
        {
            var vorbisBlock = _flacStream.MetadataBlocks
                .OfType<FlacVorbisCommentsMetadataBlock>()
                .FirstOrDefault();
            if (vorbisBlock?.VorbisComments != null)
            {
                TagTabs.Add(new VorbisTabViewModel(vorbisBlock.VorbisComments));
            }
        }

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
