namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

public sealed class FileDossier : INotifyPropertyChanged, IDisposable
{
    private List<IAudioTagOffset> _offsets = [];
    private readonly HashSet<IAudioTag> _newTags = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IAudioTag> _removedTags = new(ReferenceEqualityComparer.Instance);
    private readonly List<ValidationIssue> _parseWarnings = [];
    private FlacStream? _flacStream;
    private MediaContainers? _containers;

    /// <summary>
    /// Non-fatal errors collected while parsing the file — e.g. a single ID3v2 frame with an
    /// invalid language code. Surfaced in the validation/lint panel so the rest of the file
    /// can still be inspected.
    /// </summary>
    public IReadOnlyList<ValidationIssue> ParseWarnings => _parseWarnings;

    public bool IsFlac { get; private set; }

    public IMediaContainer? AudioStream { get; private set; }

    public IReadOnlyList<IAudioTagOffset> TagOffsets => _offsets;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string LastModifiedText { get; private set; } = string.Empty;

    public byte[] FileBytes { get; private set; } = [];

    public InspectorNode? InspectorRoot { get; private set; }

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

    public long FileSize { get; private set; }

    public string FileSizeText { get; private set; } = string.Empty;

    public FileDossier(string filePath)
        : this(filePath, File.ReadAllBytes(filePath))
    {
    }

    private FileDossier(string filePath, byte[] fileBytes)
    {
        FilePath = filePath;
        Load(fileBytes);
    }

    /// <summary>
    /// Asynchronously reads and parses a file on a background thread, keeping the caller (typically
    /// the UI thread) responsive while large files load.
    /// </summary>
    public static Task<FileDossier> CreateAsync(string filePath, CancellationToken cancellationToken = default) =>
        Task.Run(
            async () =>
            {
                var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                return new FileDossier(filePath, bytes);
            },
            cancellationToken);

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

    public void Dispose()
    {
        _containers?.Dispose();
        _containers = null;
    }

    public void Save() => SaveTo(FilePath);

    public void SaveAs(string destinationPath)
    {
        // Write the modified file to the destination path (original file untouched)
        SaveTo(destinationPath);
    }

    private void SaveTo(string targetPath)
    {
        if (IsFlac)
        {
            SaveFlac(targetPath);
        }
        else
        {
            SaveMp3Style(targetPath);
        }
    }

    private void SaveMp3Style(string targetPath)
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
                case Lyrics3v1TabViewModel l3v1:
                    l3v1.CommitToTag();
                    dirtyTags.Add(l3v1.Tag);
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

        var tmp = targetPath + ".avs-tmp";
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

        File.Move(tmp, targetPath, overwrite: true);

        _newTags.Clear();
        _removedTags.Clear();

        foreach (var tab in TagTabs)
        {
            tab.ResetDirty();
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

    private void SaveFlac(string targetPath)
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

        var tmp = targetPath + ".avs-tmp";
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

        File.Move(tmp, targetPath, overwrite: true);

        foreach (var tab in TagTabs)
        {
            tab.ResetDirty();
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

    private void Load(byte[] fileBytes)
    {
        FileBytes = fileBytes;
        var fileLength = (long)FileBytes.Length;
        FileSize = fileLength;
        FileSizeText = $"{fileLength:N0} bytes  ({FormatSize(fileLength)})";
        LastModifiedText = File.GetLastWriteTime(FilePath).ToString("yyyy-MM-dd HH:mm:ss");

        using var fs = new MemoryStream(FileBytes);

        var audioTags = new AudioTags();
        audioTags.Id3v2FrameParseError += (_, e) =>
            _parseWarnings.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                $"ID3v2 frame at 0x{e.StartOffset:X8} ({e.Version}) skipped: {e.Exception.Message}"));
        audioTags.AudioTagParseError += (_, e) =>
            _parseWarnings.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                $"{e.Reader.GetType().Name} at 0x{e.StartOffset:X8} skipped: {e.Exception.Message}"));
        audioTags.ReadTags(fs);
        _offsets = [.. audioTags.OfType<IAudioTagOffset>()];
        var tagOffsets = _offsets;

        var id3v2 = tagOffsets.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault();
        var id3v1 = tagOffsets.Select(o => o.AudioTag).OfType<Id3v1Tag>().FirstOrDefault();
        var apeTags = tagOffsets.Select(o => o.AudioTag).OfType<ApeTag>().ToList();
        var lyrics3v1 = tagOffsets.Select(o => o.AudioTag).OfType<Lyrics3Tag>().FirstOrDefault();
        var lyrics3v2 = tagOffsets.Select(o => o.AudioTag).OfType<Lyrics3v2Tag>().FirstOrDefault();
        var musicMatch = tagOffsets.Select(o => o.AudioTag).OfType<MusicMatchTag>().FirstOrDefault();

        if (id3v2 != null)
        {
            TagTabs.Add(new Id3v2TabViewModel(id3v2));
        }

        for (var i = 0; i < apeTags.Count; i++)
        {
            // Multiple APE tags in a single file is uncommon but legal — give each its own tab,
            // suffixed with "(N)" so the user can tell them apart.
            var suffix = apeTags.Count > 1 ? $" ({i + 1})" : string.Empty;
            TagTabs.Add(new ApeTabViewModel(apeTags[i], suffix));
        }

        if (lyrics3v1 != null)
        {
            TagTabs.Add(new Lyrics3v1TabViewModel(lyrics3v1));
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

        // Skip past any start-origin tags (e.g. large ID3v2) so the scanner
        // doesn't give up before reaching the audio frames. MediaContainers only
        // tolerates MaxStreamSpacingLength (128) bytes of non-audio before
        // bailing out.
        var startTagEnd = tagOffsets
            .Where(o => o.TagOrigin == TagOrigin.Start)
            .Select(o => o.EndOffset)
            .DefaultIfEmpty(0L)
            .Max();
        fs.Position = startTagEnd;
        _containers = MediaContainers.ReadStream(fs);
        var audio = _containers.FirstOrDefault();
        AudioStream = audio;
        _flacStream = audio as FlacStream;
        IsFlac = _flacStream != null;
        BuildTechCards(audio);
        BuildEncoder(audio);
        InspectorRoot = InspectorTreeBuilder.Build(FilePath, FileBytes, _offsets, audio);

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

        AddContainerTabs(audio);

        BuildHexRegions(fs, tagOffsets);

        Notify(nameof(EncoderSummary));
        Notify(nameof(HasEncoder));
        Notify(nameof(FileSize));
        Notify(nameof(FileSizeText));
        Notify(nameof(LastModifiedText));
    }

    private void BuildTechCards(IMediaContainer? audio)
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
                    TechCards.Add(new TechCard("Duration", FormatDuration(mpa.TotalDuration)));
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

            case RiffStream riff:
                TechCards.Add(new TechCard("Format", $"WAV ({riff.FormatType})"));
                if (riff.SampleRate > 0)
                {
                    TechCards.Add(new TechCard("Sample Rate", $"{riff.SampleRate / 1000.0:0.#} kHz"));
                    TechCards.Add(new TechCard("Channels", riff.Channels.ToString()));
                    TechCards.Add(new TechCard("Bits", $"{riff.BitsPerSample}-bit"));
                    if (riff.TotalDuration > 0)
                    {
                        TechCards.Add(new TechCard("Duration", FormatDuration(riff.TotalDuration)));
                    }
                }
                break;

            case AiffStream aiff:
                TechCards.Add(new TechCard("Format", aiff.FormatType));
                if (aiff.SampleRate > 0)
                {
                    TechCards.Add(new TechCard("Sample Rate", $"{aiff.SampleRate / 1000.0:0.#} kHz"));
                    TechCards.Add(new TechCard("Channels", aiff.Channels.ToString()));
                    TechCards.Add(new TechCard("Bits", $"{aiff.SampleSize}-bit"));
                    if (aiff.TotalDuration > 0)
                    {
                        TechCards.Add(new TechCard("Duration", FormatDuration(aiff.TotalDuration)));
                    }
                }
                break;

            case OggStream ogg:
                TechCards.Add(new TechCard("Format", "OGG container"));
                TechCards.Add(new TechCard("Pages", ogg.PageCount.ToString("N0")));
                break;

            default:
                TechCards.Add(new TechCard("Format", "Unknown"));
                break;
        }

        TechCards.Add(new TechCard("File Size", FileSizeText));
    }

    private void AddContainerTabs(IMediaContainer? audio)
    {
        switch (audio)
        {
            case RiffStream riff:
                AddRiffContainerTabs(riff);
                break;
            case AiffStream aiff:
                AddAiffContainerTabs(aiff);
                break;
            case Mp4Stream mp4 when Mp4HasContent(mp4.Tag):
                TagTabs.Add(BuildMp4Tab(mp4.Tag));
                break;
            case AsfStream asf when AsfHasContent(asf.MetadataTag):
                TagTabs.Add(BuildAsfTab(asf.MetadataTag));
                break;
            case MatroskaStream mkv when mkv.Tag.Entries.Count > 0:
                TagTabs.Add(BuildMatroskaTab(mkv.Tag));
                break;
            case DsfStream dsf when dsf.EmbeddedId3v2 is { } id3:
                TagTabs.Add(new Id3v2TabViewModel(id3));
                break;
            case DffStream dff when dff.EmbeddedId3v2 is { } id3:
                TagTabs.Add(new Id3v2TabViewModel(id3));
                break;
        }
    }

    private static bool Mp4HasContent(Mp4MetaTag tag) =>
        !string.IsNullOrEmpty(tag.Title) || !string.IsNullOrEmpty(tag.Artist) || !string.IsNullOrEmpty(tag.Album)
        || !string.IsNullOrEmpty(tag.AlbumArtist) || !string.IsNullOrEmpty(tag.Year) || !string.IsNullOrEmpty(tag.Genre)
        || !string.IsNullOrEmpty(tag.Composer) || !string.IsNullOrEmpty(tag.Comment) || !string.IsNullOrEmpty(tag.Tool)
        || tag.TrackNumber.HasValue || tag.DiscNumber.HasValue || tag.Bpm.HasValue || tag.Compilation.HasValue
        || tag.CoverArt.Count > 0 || tag.FreeFormItems.Count > 0 || tag.Items.Count > 0;

    private static bool AsfHasContent(AsfMetadataTag meta) =>
        meta.HasContentDescription || meta.ExtendedItems.Count > 0
        || meta.MetadataItems.Count > 0 || meta.MetadataLibraryItems.Count > 0;

    private static void AddRow(List<ContainerTagRow> rows, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            rows.Add(new ContainerTagRow(key, value));
        }
    }

    private void AddRiffContainerTabs(RiffStream riff)
    {
        if (riff.InfoTag is { } info && info.Items.Count > 0)
        {
            var rows = new List<ContainerTagRow>();
            void Add(string k, string? v) => AddRow(rows, k, v);
            Add("Title", info.Title);
            Add("Artist", info.Artist);
            Add("Product", info.Product);
            Add("Creation date", info.CreationDate);
            Add("Comment", info.Comment);
            Add("Genre", info.Genre);
            Add("Track", info.Track);
            Add("Engineer", info.Engineer);
            Add("Software", info.Software);
            Add("Copyright", info.Copyright);
            foreach (var kv in info.Items)
            {
                if (kv.Key is not ("INAM" or "IART" or "IPRD" or "ICRD" or "ICMT" or "IGNR" or "ITRK" or "IENG" or "ISFT" or "ICOP"))
                {
                    rows.Add(new(kv.Key, kv.Value));
                }
            }

            TagTabs.Add(new ContainerTagTabViewModel("WAV INFO", "INFO", rows));
        }

        if (riff.BextChunk is { } bext)
        {
            var rows = new List<ContainerTagRow>
            {
                new("Description", bext.Description ?? string.Empty),
                new("Originator", bext.Originator ?? string.Empty),
                new("Originator reference", bext.OriginatorReference ?? string.Empty),
                new("Origination date", bext.OriginationDate ?? string.Empty),
                new("Origination time", bext.OriginationTime ?? string.Empty),
                new("Time reference", bext.TimeReference.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)),
                new("Version", bext.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("Coding history", bext.CodingHistory ?? string.Empty),
            };
            TagTabs.Add(new ContainerTagTabViewModel("BWF bext", "BWF", rows));
        }

        if (riff.IxmlChunk is { } ixml)
        {
            var rows = new List<ContainerTagRow>();
            void Add(string k, string? v) => AddRow(rows, k, v);
            Add("Project", ixml.ProjectName);
            Add("Scene", ixml.SceneName);
            Add("Take", ixml.TakeName);
            Add("Tape", ixml.Tape);
            Add("Note", ixml.Note);
            Add("File UID", ixml.FileUid);
            Add("Ubits", ixml.Ubits);
            if (rows.Count == 0)
            {
                rows.Add(new ContainerTagRow("(raw XML)", $"{ixml.Xml.Length:N0} chars"));
            }

            TagTabs.Add(new ContainerTagTabViewModel("iXML", "iXML", rows));
        }

        if (riff.EmbeddedId3v2?.AudioTag is Id3v2Tag wavId3)
        {
            TagTabs.Add(new Id3v2TabViewModel(wavId3));
        }
    }

    private void AddAiffContainerTabs(AiffStream aiff)
    {
        if (aiff.TextChunks is not { } text)
        {
            return;
        }

        var rows = new List<ContainerTagRow>();
        AddRow(rows, "Name", text.Name);
        AddRow(rows, "Author", text.Author);
        AddRow(rows, "Annotation", text.Annotation);
        for (var i = 0; i < text.Comments.Count; i++)
        {
            var c = text.Comments[i];
            rows.Add(new($"Comment {i + 1}", c.Text ?? string.Empty));
        }

        if (rows.Count > 0)
        {
            TagTabs.Add(new ContainerTagTabViewModel("AIFF text", "AIFF", rows));
        }
    }

    private static ContainerTagTabViewModel BuildMp4Tab(Mp4MetaTag tag)
    {
        var rows = new List<ContainerTagRow>();
        void Add(string k, string? v) => AddRow(rows, k, v);
        Add("Title", tag.Title);
        Add("Artist", tag.Artist);
        Add("Album", tag.Album);
        Add("Album artist", tag.AlbumArtist);
        Add("Year", tag.Year);
        Add("Genre", tag.Genre);
        Add("Composer", tag.Composer);
        Add("Comment", tag.Comment);
        Add("Encoder", tag.Tool);
        if (tag.TrackNumber is int tn)
        {
            rows.Add(new("Track", tag.TrackTotal is int tt ? $"{tn} / {tt}" : tn.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (tag.DiscNumber is int dn)
        {
            rows.Add(new("Disc", tag.DiscTotal is int dt ? $"{dn} / {dt}" : dn.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (tag.Bpm is int bpm)
        {
            rows.Add(new("BPM", bpm.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (tag.Compilation == true)
        {
            rows.Add(new("Compilation", "yes"));
        }

        for (var i = 0; i < tag.CoverArt.Count; i++)
        {
            var art = tag.CoverArt[i];
            rows.Add(new($"Cover art {i + 1}", $"{art.Format}, {art.Data.Length:N0} bytes"));
        }

        foreach (var ff in tag.FreeFormItems)
        {
            rows.Add(new($"{ff.Key.Mean}/{ff.Key.Name}", ff.Value));
        }

        return new ContainerTagTabViewModel("MP4 / iTunes", "MP4", rows);
    }

    private static ContainerTagTabViewModel BuildAsfTab(AsfMetadataTag meta)
    {
        var rows = new List<ContainerTagRow>();
        void Add(string k, string? v) => AddRow(rows, k, v);
        Add("Title", meta.Title);
        Add("Author", meta.Author);
        Add("Copyright", meta.Copyright);
        Add("Description", meta.Description);
        Add("Rating", meta.Rating);

        foreach (var kv in meta.ExtendedItems)
        {
            rows.Add(new(kv.Key, AsfValueToString(kv.Value)));
        }

        foreach (var item in meta.MetadataItems)
        {
            rows.Add(new($"[MO] {item.Name}", AsfValueToString(item.Value)));
        }

        foreach (var item in meta.MetadataLibraryItems)
        {
            rows.Add(new($"[MLO] {item.Name}", AsfValueToString(item.Value)));
        }

        return new ContainerTagTabViewModel("ASF / WMA", "ASF", rows);
    }

    private static string AsfValueToString(AsfTypedValue v) => v.Type switch
    {
        AsfDataType.UnicodeString => v.AsString ?? string.Empty,
        AsfDataType.Bytes => $"{v.AsBytes?.Length ?? 0:N0} bytes",
        AsfDataType.Bool => v.AsBool.ToString(),
        AsfDataType.Word => v.AsWord.ToString(System.Globalization.CultureInfo.InvariantCulture),
        AsfDataType.Dword => v.AsDword.ToString(System.Globalization.CultureInfo.InvariantCulture),
        AsfDataType.Qword => v.AsQword.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => string.Empty,
    };

    private static ContainerTagTabViewModel BuildMatroskaTab(MatroskaTag tag)
    {
        var rows = new List<ContainerTagRow>();
        foreach (var entry in tag.Entries)
        {
            var prefix = entry.Targets.TargetTypeValue > 0
                ? $"[{entry.Targets.TargetTypeValue}] "
                : string.Empty;
            FlattenSimpleTags(entry.SimpleTags, prefix, rows);
        }

        return new ContainerTagTabViewModel("Matroska", "MKV", rows);
    }

    private static void FlattenSimpleTags(IList<MatroskaSimpleTag> tags, string prefix, List<ContainerTagRow> rows)
    {
        foreach (var st in tags)
        {
            var key = $"{prefix}{st.Name}";
            var value = st.Value ?? (st.Binary is { Length: > 0 } b ? $"{b.Length:N0} bytes" : string.Empty);
            rows.Add(new(key, value));
            if (st.SimpleTags.Count > 0)
            {
                FlattenSimpleTags(st.SimpleTags, key + " / ", rows);
            }
        }
    }

    private void BuildEncoder(IMediaContainer? audio)
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
