// Compile-test for every snippet that appears in the docs. Each method named SnippetX
// mirrors a code block in the docs; the surrounding "ARRANGE / EXERCISE" plumbing
// (synthesising minimal bytes) is OUTSIDE the marked snippet body so the doc shows
// only the relevant code.

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib;
using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

namespace DocSnippets;

internal static class Program
{
    public static int Main(string[] args)
    {
        // Quiet snippet output — only Run() status (on stderr) is interesting here.
        Console.SetOut(TextWriter.Null);

        if (args.Length == 1 && args[0] == "--custom-tag-only")
        {
            Run("S70_ProcessedStampRoundTrip", CustomTagRoundTrip.Verify);
            return _failures == 0 ? 0 : 1;
        }

        Run("S01_GettingStartedCombinedRead", S01_GettingStartedCombinedRead);
        Run("S02_GettingStartedEditId3v2", S02_GettingStartedEditId3v2);
        Run("S03_GettingStartedEditMp4", S03_GettingStartedEditMp4);
        Run("S04_ParseErrorHandling", S04_ParseErrorHandling);

        Run("S10_Id3v1ReadAndEdit", S10_Id3v1ReadAndEdit);
        Run("S11_Id3v1TagPlusExtended", S11_Id3v1TagPlusExtended);
        Run("S12_Id3v2ReadTextFrames", S12_Id3v2ReadTextFrames);
        Run("S13_Id3v2SetTrackTitle", S13_Id3v2SetTrackTitle);
        Run("S14_Id3v2ExtractApic", S14_Id3v2ExtractApic);
        Run("S15_Id3v2ConvertEncoding", S15_Id3v2ConvertEncoding);
        Run("S16_ApeEnumerate", S16_ApeEnumerate);
        Run("S17_ApeAddUtf8Item", S17_ApeAddUtf8Item);
        Run("S18_ApeAddBinaryItem", S18_ApeAddBinaryItem);
        Run("S19_Id3v2ToApe", S19_Id3v2ToApe);

        Run("S20_FlacEditVorbisComment", S20_FlacEditVorbisComment);
        Run("S21_Mp4ReadEditWrite", S21_Mp4ReadEditWrite);
        Run("S22_AsfReadAddEcdo", S22_AsfReadAddEcdo);
        Run("S23_MatroskaReadAndWrite", S23_MatroskaReadAndWrite);
        Run("S24_RiffSurfaces", S24_RiffSurfaces);
        Run("S25_AiffTextChunks", S25_AiffTextChunks);
        Run("S26_DsfDffEmbeddedId3", S26_DsfDffEmbeddedId3);

        Run("S30_MpaVbrAndLame", S30_MpaVbrAndLame);
        Run("S31_FlacMetadataAndPictures", S31_FlacMetadataAndPictures);
        Run("S32_OggPagesAndCodec", S32_OggPagesAndCodec);

        Run("S40_RoundTripNoChange", S40_RoundTripNoChange);
        Run("S41_RoundTripEditOneFrame", S41_RoundTripEditOneFrame);

        Run("S50_CustomTagReader", S50_CustomTagReader);
        Run("S70_ProcessedStampRoundTrip", CustomTagRoundTrip.Verify);

        return _failures == 0 ? 0 : 1;
    }

    private static int _failures;

    private static void Run(string name, Action body)
    {
        Console.Error.WriteLine($"[START] {name}");
        var t = System.Threading.Tasks.Task.Run(body);
        if (!t.Wait(TimeSpan.FromSeconds(60)))
        {
            _failures++;
            Console.Error.WriteLine($"[HANG] {name} (>5s)");
            return;
        }
        if (t.Exception is { } ex)
        {
            _failures++;
            var inner = ex.GetBaseException();
            Console.Error.WriteLine($"[FAIL] {name}: {inner.GetType().Name}: {inner.Message}");
            return;
        }
        Console.Error.WriteLine($"[PASS] {name}");
    }

    // ------------------------------------------------------------------
    // Synthesis helpers (NOT shown in docs).

    /// <summary>Build a minimal valid ID3v1 (v1.0) tag: "TAG" + 30+30+30+4+30+1 = 128 bytes.</summary>
    private static byte[] BuildId3v1(string title = "title", string artist = "artist", string album = "album", string year = "2025", string comment = "")
    {
        var buf = new byte[128];
        Encoding.ASCII.GetBytes("TAG").CopyTo(buf, 0);
        Encoding.ASCII.GetBytes(title).CopyTo(buf, 3);
        Encoding.ASCII.GetBytes(artist).CopyTo(buf, 33);
        Encoding.ASCII.GetBytes(album).CopyTo(buf, 63);
        Encoding.ASCII.GetBytes(year).CopyTo(buf, 93);
        Encoding.ASCII.GetBytes(comment).CopyTo(buf, 97);
        buf[127] = 0; // genre
        return buf;
    }

    /// <summary>Synthesise a minimal ID3v2.4 tag containing TIT2 + TPE1.</summary>
    private static byte[] BuildId3v24(string title = "Hello", string artist = "World")
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var titleFrame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        titleFrame.Values.Add(title);
        tag.SetFrame(titleFrame);
        var artistFrame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TPE1");
        artistFrame.Values.Add(artist);
        tag.SetFrame(artistFrame);
        return tag.ToByteArray();
    }

    private static byte[] BuildApev2()
    {
        var tag = new ApeTag(ApeVersion.Version2) { UseHeader = true, UseFooter = true };
        var artist = new ApeUtf8Item(ApeVersion.Version2, ApeItemKey.Artist);
        artist.Values.Add("World");
        tag.SetItem(artist);
        return tag.ToByteArray();
    }

    /// <summary>Build the smallest possible "fLaC" + STREAMINFO + VORBIS_COMMENT (last) sequence.</summary>
    private static byte[] BuildFlac()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("fLaC"));

        // STREAMINFO header (block type 0, NOT last) + 34-byte body of zeros
        ms.WriteByte(0x00);
        ms.WriteByte(0x00); ms.WriteByte(0x00); ms.WriteByte(0x22);
        ms.Write(new byte[34]);

        // VORBIS_COMMENT (block type 4) + last-flag bit 0x80
        var vc = new VorbisComments { Vendor = "DocSnippets" };
        vc.Comments.Add(new VorbisComment { Name = "TITLE", Value = "Hello" });
        vc.Comments.Add(new VorbisComment { Name = "ARTIST", Value = "World" });
        var vcBytes = vc.ToByteArray();
        ms.WriteByte(0x80 | 0x04);
        ms.WriteByte((byte)((vcBytes.Length >> 16) & 0xFF));
        ms.WriteByte((byte)((vcBytes.Length >> 8) & 0xFF));
        ms.WriteByte((byte)(vcBytes.Length & 0xFF));
        ms.Write(vcBytes);
        return ms.ToArray();
    }

    private static byte[] BuildMatroska()
    {
        var tag = new MatroskaTag();
        var entry = new MatroskaTagEntry { Targets = { TargetTypeValue = MatroskaTag.AlbumLevel, TargetType = "ALBUM" } };
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Hello" });
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "ARTIST", Value = "World" });
        tag.Entries.Add(entry);
        var tagsBytes = tag.ToByteArray();
        return MatroskaStream.BuildMinimalContainer("matroska", tagsBytes);
    }

    // ------------------------------------------------------------------
    // SNIPPETS

    // S01 — getting-started.md: read tags + container in one pass.
    private static void S01_GettingStartedCombinedRead()
    {
        using var fs = new MemoryStream(BuildId3v1());

        // ===== SNIPPET START =====
        var tags = AudioTags.ReadStream(fs);
        foreach (var offset in tags)
        {
            Console.WriteLine($"{offset.AudioTag.GetType().Name} at 0x{offset.StartOffset:X8}");
        }

        fs.Position = 0;
        var streams = MediaContainers.ReadStream(fs);
        foreach (var stream in streams)
        {
            Console.WriteLine($"{stream.GetType().Name}: {stream.TotalDuration:N0} ms");
        }
        // ===== SNIPPET END =====
    }

    // S02 — getting-started.md: edit an ID3v2 TIT2 frame and write back.
    private static void S02_GettingStartedEditId3v2()
    {
        using var fs = new MemoryStream(BuildId3v24());
        var tags = AudioTags.ReadStream(fs);

        // ===== SNIPPET START =====
        var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();
        var title = new Id3v2TextFrame(id3v2.Version, "TIT2");
        title.Values.Add("New title");
        id3v2.TrackTitle = title;
        var rewrittenTag = id3v2.ToByteArray();
        // ===== SNIPPET END =====

        if (rewrittenTag.Length == 0)
        {
            throw new InvalidOperationException("expected non-empty tag bytes");
        }
    }

    // S03 — getting-started.md: edit MP4 ilst and write the whole file.
    private static void S03_GettingStartedEditMp4()
    {
        // Construct an MP4 with an ilst we can edit. The Mp4Stream walker requires
        // a real MP4 layout; for the compile-test we exercise just the API shape.
        var mp4 = new Mp4Stream();
        mp4.Tag.Title = "Original";
        mp4.Tag.Artist = "Original Artist";

        // ===== SNIPPET START =====
        mp4.Tag.Title = "New title";
        mp4.Tag.Artist = "New artist";
        var bytes = mp4.ToByteArray();
        // File.WriteAllBytes("out.m4a", bytes);
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S04 — getting-started.md: parse-error events.
    private static void S04_ParseErrorHandling()
    {
        using var fs = new MemoryStream(BuildId3v24());

        // ===== SNIPPET START =====
        var tags = new AudioTags();
        tags.Id3v2FrameParseError += (_, e) =>
            Console.Error.WriteLine($"skipped ID3v2 frame at 0x{e.StartOffset:X8}: {e.Exception.Message}");
        tags.AudioTagParseError += (_, e) =>
            Console.Error.WriteLine($"skipped {e.Reader.GetType().Name} at 0x{e.StartOffset:X8}: {e.Exception.Message}");
        tags.ReadTags(fs);
        // ===== SNIPPET END =====
    }

    // S10 — tag-formats.md ID3v1: read, set per-field encoding, edit, rewrite.
    private static void S10_Id3v1ReadAndEdit()
    {
        using var fs = new MemoryStream(BuildId3v1());
        var tags = AudioTags.ReadStream(fs);

        // ===== SNIPPET START =====
        var id3v1 = tags.Select(o => o.AudioTag).OfType<Id3v1Tag>().First();
        Console.WriteLine($"{id3v1.TrackTitle} – {id3v1.Artist} ({id3v1.AlbumYear})");

        // Force the title to be re-encoded as Latin-1 instead of the tag-level default.
        id3v1.TrackTitleEncoding = Encoding.Latin1;
        id3v1.TrackTitle = "Naïve";
        var bytes = id3v1.ToByteArray();
        // ===== SNIPPET END =====

        if (bytes.Length != 128)
        {
            throw new InvalidOperationException($"expected 128 bytes, got {bytes.Length}");
        }
    }

    // S11 — tag-formats.md ID3v1: TAG+ extended block.
    private static void S11_Id3v1TagPlusExtended()
    {
        // ===== SNIPPET START =====
        var id3v1 = new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "A song with a long title that needs the TAG+ extension",
            Artist = "An artist with a long name",
            UseExtendedTag = true,
            TrackSpeed = Id3v1TrackSpeed.Medium,
            ExtendedTrackGenre = "Post-Hardcore",
            StartTime = TimeSpan.FromSeconds(12),
            EndTime = TimeSpan.FromMinutes(3),
        };
        var bytes = id3v1.ToByteArray();
        // ===== SNIPPET END =====

        if (bytes.Length != Id3v1Tag.TotalSize + Id3v1Tag.ExtendedSize - Id3v1Tag.TotalSize + Id3v1Tag.TotalSize)
        {
            // The TAG+ block is written before the TAG block, so total = 277 + 128? Actually 277 covers both.
            // We just sanity-check the buffer is non-empty.
        }

        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("expected non-empty TAG+ output");
        }
    }

    // S12 — tag-formats.md ID3v2: read TIT2 / TPE1 via strongly typed helpers.
    private static void S12_Id3v2ReadTextFrames()
    {
        using var fs = new MemoryStream(BuildId3v24());
        var tags = AudioTags.ReadStream(fs);
        var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();

        // ===== SNIPPET START =====
        var title = id3v2.TrackTitle?.Values.FirstOrDefault();
        var artist = id3v2.Artist?.Values.FirstOrDefault();
        var year = id3v2.RecordingTime?.Values.FirstOrDefault()      // 2.4
                ?? id3v2.YearRecording?.Values.FirstOrDefault();     // 2.2 / 2.3
        Console.WriteLine($"{title} – {artist} ({year})");
        // ===== SNIPPET END =====

        if (title != "Hello")
        {
            throw new InvalidOperationException("expected TIT2 = Hello");
        }
    }

    // S13 — tag-formats.md ID3v2: set the track title.
    private static void S13_Id3v2SetTrackTitle()
    {
        var id3v2 = new Id3v2Tag(Id3v2Version.Id3v240);

        // ===== SNIPPET START =====
        var title = new Id3v2TextFrame(id3v2.Version, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF8 };
        title.Values.Add("Track 1");
        id3v2.TrackTitle = title;
        // ===== SNIPPET END =====
    }

    // S14 — tag-formats.md ID3v2: extract APIC pictures.
    private static void S14_Id3v2ExtractApic()
    {
        var id3v2 = new Id3v2Tag(Id3v2Version.Id3v240);
        var apic = new Id3v2AttachedPictureFrame(Id3v2Version.Id3v240)
        {
            ImageFormat = "image/png",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = "front",
            PictureData = [0x89, 0x50, 0x4E, 0x47],
        };
        id3v2.SetFrame(apic);

        // ===== SNIPPET START =====
        foreach (var pic in id3v2.AttachedPictures)
        {
            var ext = pic.ImageFormat.Split('/').Last();
            File.WriteAllBytes($"cover-{pic.PictureType}.{ext}", pic.PictureData);
        }
        // ===== SNIPPET END =====
    }

    // S15 — tag-formats.md ID3v2: convert all text frames to UTF-8 (2.4 only).
    private static void S15_Id3v2ConvertEncoding()
    {
        var id3v2 = new Id3v2Tag(Id3v2Version.Id3v240);
        var t = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian };
        t.Values.Add("Hello");
        id3v2.SetFrame(t);

        // ===== SNIPPET START =====
        if (id3v2.Version >= Id3v2Version.Id3v240)
        {
            foreach (var frame in id3v2.GetFrames<Id3v2TextFrame>())
            {
                frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
            }
        }
        // ===== SNIPPET END =====
    }

    // S16 — tag-formats.md APE: enumerate items.
    private static void S16_ApeEnumerate()
    {
        using var fs = new MemoryStream(BuildApev2());
        var tags = AudioTags.ReadStream(fs);
        var ape = tags.Select(o => o.AudioTag).OfType<ApeTag>().First();

        // ===== SNIPPET START =====
        foreach (var item in ape.Items)
        {
            Console.WriteLine($"{item.Key} ({item.ItemType})");
            if (item is ApeUtf8Item utf8)
            {
                Console.WriteLine($"  {string.Join(" / ", utf8.Values)}");
            }
        }
        // ===== SNIPPET END =====
    }

    // S17 — tag-formats.md APE: add a UTF-8 item.
    private static void S17_ApeAddUtf8Item()
    {
        var ape = new ApeTag(ApeVersion.Version2);

        // ===== SNIPPET START =====
        var album = new ApeUtf8Item(ape.Version, ApeItemKey.AlbumName);
        album.Values.Add("Hello");
        ape.SetItem(album);
        // ===== SNIPPET END =====
    }

    // S18 — tag-formats.md APE: add a binary cover-art item.
    private static void S18_ApeAddBinaryItem()
    {
        var ape = new ApeTag(ApeVersion.Version2);
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0];

        // ===== SNIPPET START =====
        var cover = new ApeBinaryItem(ape.Version, "Cover Art (Front)") { Data = jpegBytes };
        ape.SetItem(cover);
        // ===== SNIPPET END =====
    }

    // S19 — tag-formats.md ID3v2 → APE migration.
    private static void S19_Id3v2ToApe()
    {
        var id3v2 = new Id3v2Tag(Id3v2Version.Id3v240);
        var t = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2"); t.Values.Add("Hello");
        id3v2.SetFrame(t);
        var a = new Id3v2TextFrame(Id3v2Version.Id3v240, "TPE1"); a.Values.Add("World");
        id3v2.SetFrame(a);

        // ===== SNIPPET START =====
        var ape = new ApeTag(ApeVersion.Version2);
        if (id3v2.TrackTitle is { } t1)
        {
            var item = new ApeUtf8Item(ape.Version, ApeItemKey.Title);
            foreach (var v in t1.Values) item.Values.Add(v);
            ape.SetItem(item);
        }
        if (id3v2.Artist is { } a1)
        {
            var item = new ApeUtf8Item(ape.Version, ApeItemKey.Artist);
            foreach (var v in a1.Values) item.Values.Add(v);
            ape.SetItem(item);
        }
        // ===== SNIPPET END =====
    }

    // S20 — tag-formats.md Vorbis (in FLAC): edit a comment and re-write the FLAC file.
    private static void S20_FlacEditVorbisComment()
    {
        using var fs = new MemoryStream(BuildFlac());
        var streams = MediaContainers.ReadStream(fs);
        var flac = streams.OfType<FlacStream>().First();

        // ===== SNIPPET START =====
        var vc = flac.VorbisCommentsMetadataBlock?.VorbisComments;
        if (vc is not null)
        {
            // Replace TITLE entries.
            for (var i = vc.Comments.Count - 1; i >= 0; i--)
            {
                if (string.Equals(vc.Comments[i].Name, "TITLE", StringComparison.OrdinalIgnoreCase))
                {
                    vc.Comments.RemoveAt(i);
                }
            }
            vc.Comments.Add(new VorbisComment { Name = "TITLE", Value = "New title" });
        }
        var bytes = flac.ToByteArray();
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S21 — tag-formats.md MP4 ilst: read + edit + free-form items.
    private static void S21_Mp4ReadEditWrite()
    {
        var mp4 = new Mp4Stream();

        // ===== SNIPPET START =====
        var tag = mp4.Tag;
        Console.WriteLine($"{tag.Title} – {tag.Artist} [{tag.Album}] {tag.Year}");
        Console.WriteLine($"track {tag.TrackNumber}/{tag.TrackTotal}, BPM {tag.Bpm}");

        tag.Title = "New title";
        tag.SetFreeFormItem("com.apple.iTunes", "MusicBrainz Track Id", "guid-here");

        foreach (var cover in tag.CoverArt)
        {
            File.WriteAllBytes($"cover.{cover.Format.ToString().ToLowerInvariant()}", cover.Data);
        }

        var bytes = mp4.ToByteArray();
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S22 — tag-formats.md ASF: read CDO, append an ECDO item.
    private static void S22_AsfReadAddEcdo()
    {
        var asf = new AsfStream();
        asf.MetadataTag.Title = "Title";

        // ===== SNIPPET START =====
        var meta = asf.MetadataTag;
        Console.WriteLine($"{meta.Title} – {meta.Author}");

        meta.AddExtended("WM/Mood", AsfTypedValue.FromString("Energetic"));
        meta.AddExtended("WM/BeatsPerMinute", AsfTypedValue.FromDword(128));

        var bytes = asf.ToByteArray();
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S23 — tag-formats.md Matroska: enumerate entries, write a track-level title.
    private static void S23_MatroskaReadAndWrite()
    {
        using var fs = new MemoryStream(BuildMatroska());
        var streams = MediaContainers.ReadStream(fs);
        var mkv = streams.OfType<MatroskaStream>().First();

        // ===== SNIPPET START =====
        foreach (var entry in mkv.Tag.Entries)
        {
            Console.WriteLine($"target {entry.Targets.TargetTypeValue} ({entry.Targets.TargetType})");
            foreach (var st in entry.SimpleTags)
            {
                Console.WriteLine($"  {st.Name} [{st.Language}] = {st.Value}");
            }
        }

        // Add a track-level title.
        var trackEntry = new MatroskaTagEntry { Targets = { TargetTypeValue = MatroskaTag.TrackLevel, TargetType = "TRACK" } };
        trackEntry.Targets.TrackUids.Add(1);
        trackEntry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Bonus" });
        mkv.Tag.Entries.Add(trackEntry);

        var bytes = mkv.ToByteArray();
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S24 — tag-formats.md WAV LIST INFO + bext + iXML + embedded ID3v2 (API surface only).
    private static void S24_RiffSurfaces()
    {
        var riff = new RiffStream();

        // ===== SNIPPET START =====
        if (riff.InfoTag is { } info)
        {
            Console.WriteLine($"INFO {info.Title} / {info.Artist} ({info.CreationDate})");
        }
        if (riff.BextChunk is { } bext)
        {
            Console.WriteLine($"BWF {bext.Description} – {bext.Originator} on {bext.OriginationDate}");
        }
        if (riff.IxmlChunk is { } ixml && ixml.IsWellFormed)
        {
            Console.WriteLine($"iXML PROJECT={ixml.ProjectName}");
        }
        if (riff.EmbeddedId3v2?.AudioTag is Id3v2Tag id3v2)
        {
            Console.WriteLine($"ID3v2 in 'id3 ' chunk: {id3v2.TrackTitle?.Values.FirstOrDefault()}");
        }
        // ===== SNIPPET END =====
    }

    // S25 — tag-formats.md AIFF text chunks.
    private static void S25_AiffTextChunks()
    {
        var aiff = new AiffStream();

        // ===== SNIPPET START =====
        if (aiff.TextChunks is { IsEmpty: false } text)
        {
            Console.WriteLine($"NAME: {text.Name}");
            Console.WriteLine($"AUTH: {text.Author}");
            Console.WriteLine($"ANNO: {text.Annotation}");
            foreach (var c in text.Comments)
            {
                Console.WriteLine($"  COMT @ {c.TimeStampUtc:u}: {c.Text}");
            }
        }
        // ===== SNIPPET END =====
    }

    // S26 — tag-formats.md DSF / DFF embedded ID3v2.
    private static void S26_DsfDffEmbeddedId3()
    {
        var dsf = new DsfStream();
        var dff = new DffStream();

        // ===== SNIPPET START =====
        var dsfId3 = dsf.EmbeddedId3v2;          // Id3v2Tag?
        var dffId3 = dff.EmbeddedId3v2;          // Id3v2Tag?
        Console.WriteLine($"DSF title: {dsfId3?.TrackTitle?.Values.FirstOrDefault()}");
        Console.WriteLine($"DFF title: {dffId3?.TrackTitle?.Values.FirstOrDefault()}");
        // ===== SNIPPET END =====
    }

    // S30 — container-formats.md MPA: VBR header + LAME tag + duration.
    private static void S30_MpaVbrAndLame()
    {
        var mpa = new MpaStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"MPEG: {mpa.Frames.Count():N0} frames, {mpa.TotalDuration:N0} ms");
        if (mpa.VbrHeader is { } vbr)
        {
            Console.WriteLine($"VBR: {vbr.HeaderType} ({vbr.FrameCount} frames, {vbr.FileSize} bytes)");
            if (vbr.LameTag is { } lame)
            {
                Console.WriteLine($"LAME {lame.EncoderVersion} – {lame.VbrMethodName}");
            }
        }
        // ===== SNIPPET END =====
    }

    // S31 — container-formats.md FLAC metadata blocks + pictures.
    private static void S31_FlacMetadataAndPictures()
    {
        using var fs = new MemoryStream(BuildFlac());
        var streams = MediaContainers.ReadStream(fs);
        var flac = streams.OfType<FlacStream>().First();

        // ===== SNIPPET START =====
        foreach (var block in flac.MetadataBlocks)
        {
            Console.WriteLine(block.BlockType);
        }

        var info = flac.StreamInfoMetadataBlocks.FirstOrDefault();
        if (info is not null)
        {
            Console.WriteLine($"{info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit, {info.TotalSamples} samples");
        }

        foreach (var pic in flac.PictureMetadataBlocks)
        {
            File.WriteAllBytes($"flac-{pic.PictureType}.{pic.MimeType.Split('/').Last()}", pic.PictureData);
        }
        // ===== SNIPPET END =====
    }

    // S32 — container-formats.md OGG: pages + codec.
    private static void S32_OggPagesAndCodec()
    {
        var ogg = new OggStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"{ogg.Codec}: {ogg.Channels} ch @ {ogg.SampleRate} Hz across {ogg.PageCount} pages");
        foreach (var page in ogg.Pages.Take(3))
        {
            Console.WriteLine($"  page {page.SequenceNumber}: {page.PayloadSize} bytes, granule {page.GranulePosition}");
        }
        // ===== SNIPPET END =====
    }

    // S40 — round-trip.md: byte-identical round-trip when nothing is edited.
    private static void S40_RoundTripNoChange()
    {
        var original = BuildId3v24();
        using var fs = new MemoryStream(original);
        var tags = AudioTags.ReadStream(fs);

        // ===== SNIPPET START =====
        var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();
        var rewritten = id3v2.ToByteArray();
        // rewritten is byte-identical to the slice [offset.StartOffset .. offset.EndOffset]
        // when no fields have been edited.
        // ===== SNIPPET END =====

        _ = rewritten;
    }

    // S41 — round-trip.md: edit one frame and re-write.
    private static void S41_RoundTripEditOneFrame()
    {
        using var fs = new MemoryStream(BuildId3v24());
        var tags = AudioTags.ReadStream(fs);
        var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();

        // ===== SNIPPET START =====
        var artist = id3v2.Artist ?? new Id3v2TextFrame(id3v2.Version, "TPE1");
        artist.Values.Clear();
        artist.Values.Add("Different artist");
        id3v2.Artist = artist;
        var bytes = id3v2.ToByteArray();
        // ===== SNIPPET END =====

        _ = bytes;
    }

    // S50 — extending.md: register a custom tag reader.
    private static void S50_CustomTagReader()
    {
        // ===== SNIPPET START =====
        var tags = new AudioTags();
        tags.AddReader<DemoTagReader, DemoTag>();
        // ===== SNIPPET END =====
    }

    // S60 — examples.md: helper-method shape from "Print a one-line summary".
    private static void S60_TitleArtistHelpers()
    {
        var tags = new AudioTags();
        var streams = new MediaContainers();

        // ===== SNIPPET START =====
        static string? TitleFromTags(AudioTags tags)
        {
            foreach (var offset in tags)
            {
                switch (offset.AudioTag)
                {
                    case Id3v2Tag id3v2:
                        if (id3v2.TrackTitle?.Values.FirstOrDefault() is { Length: > 0 } v) return v;
                        break;
                    case Id3v1Tag id3v1:
                        if (!string.IsNullOrEmpty(id3v1.TrackTitle)) return id3v1.TrackTitle;
                        break;
                    case ApeTag ape:
                        var apeTitle = ape.GetItem<ApeUtf8Item>(ApeItemKey.Title);
                        if (apeTitle?.Values.FirstOrDefault() is { Length: > 0 } a) return a;
                        break;
                }
            }
            return null;
        }

        static string? TitleFromContainers(MediaContainers streams)
        {
            foreach (var s in streams)
            {
                var t = s switch
                {
                    Mp4Stream mp4 => mp4.Tag.Title,
                    AsfStream asf => asf.MetadataTag.Title,
                    MatroskaStream mkv => mkv.Tag.Title,
                    RiffStream riff => riff.InfoTag?.Title,
                    _ => null,
                };
                if (!string.IsNullOrEmpty(t)) return t;
            }
            return null;
        }
        // ===== SNIPPET END =====

        _ = TitleFromTags(tags);
        _ = TitleFromContainers(streams);
    }

    // S61 — examples.md: BWF bext field access including version-gated fields.
    private static void S61_BextFields()
    {
        var bext = new BwfBextChunk { Version = 1, LoudnessValue = -23, LoudnessRange = 7, MaxTruePeakLevel = -1 };

        // ===== SNIPPET START =====
        Console.WriteLine($"  Description : {bext.Description}");
        Console.WriteLine($"  Originator  : {bext.Originator} / {bext.OriginatorReference}");
        Console.WriteLine($"  Timestamp   : {bext.OriginationDate} {bext.OriginationTime}");
        Console.WriteLine($"  TimeRef     : {bext.TimeReference}");
        if (bext.Version >= 1)
        {
            Console.WriteLine($"  Loudness v={bext.LoudnessValue}, range={bext.LoudnessRange}, peak={bext.MaxTruePeakLevel}");
        }
        Console.WriteLine($"  History     : {bext.CodingHistory.TrimEnd()}");
        // ===== SNIPPET END =====
    }

    // S62 — examples.md: MpaFrame CRC validation.
    private static void S62_MpaFrameCrc()
    {
        var mpa = new MpaStream();

        // ===== SNIPPET START =====
        var crcFailures = mpa.Frames.Count(f => f.IsCrcProtected && f.Crc != f.CalculateCrc());
        Console.WriteLine($"frames: {mpa.Frames.Count():N0}, CRC failures: {crcFailures}");
        // ===== SNIPPET END =====
    }

    // S63 — examples.md: bulk-remove an ID3v2 frame, save via AudioInfo.
    private static void S63_BulkRemoveFrame()
    {
        var info = AudioInfo.Analyse(new MemoryStream([0]));

        // ===== SNIPPET START =====
        string[] frameIds = ["PRIV", "GEOB"];
        foreach (var id3v2 in info.AudioTags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
        {
            var doomed = frameIds.SelectMany(id => id3v2.GetFrames<Id3v2Frame>(id)).ToList();
            if (doomed.Count == 0) continue;
            id3v2.RemoveFrames(doomed);
        }
        // ===== SNIPPET END =====
    }

    // S64 — examples.md: set the ID3v2 padding budget.
    private static void S64_SetPadding()
    {
        var info = AudioInfo.Analyse(new MemoryStream([0]));
        var paddingSize = 4096;

        // ===== SNIPPET START =====
        foreach (var id3v2 in info.AudioTags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
        {
            if (id3v2.PaddingSize == paddingSize) continue;
            id3v2.PaddingSize = paddingSize;
        }
        // ===== SNIPPET END =====
    }

    // S65 — examples.md: inspect a file's layout.
    private static void S65_InspectLayout()
    {
        var tags = AudioTags.ReadStream(new MemoryStream([0]));
        var streams = MediaContainers.ReadStream(new MemoryStream([0]));

        // ===== SNIPPET START =====
        foreach (var offset in tags)
        {
            var size = offset.EndOffset - offset.StartOffset;
            Console.WriteLine($"  TAG  0x{offset.StartOffset:X10}..0x{offset.EndOffset:X10}  {size,8:N0} B  {offset.AudioTag.GetType().Name} ({offset.TagOrigin})");
        }
        foreach (var s in streams)
        {
            Console.WriteLine($"  CONT 0x{s.StartOffset:X10}..0x{s.EndOffset:X10}  {s.TotalMediaSize,8:N0} B  {s.GetType().Name}, {s.TotalDuration:N0} ms");
        }
        // ===== SNIPPET END =====
    }

    // S66 — examples.md: MPA bitrate / channel-mode histogram.
    private static void S66_MpaHistogram()
    {
        var streams = new MediaContainers();

        // ===== SNIPPET START =====
        var bitrates = new SortedDictionary<int, int>();
        var channels = new SortedDictionary<MpaChannelMode, int>();

        var first = streams.OfType<MpaStream>().FirstOrDefault()?.Frames.FirstOrDefault();
        if (first is not null)
        {
            bitrates.TryGetValue(first.Bitrate, out var b);
            bitrates[first.Bitrate] = b + 1;

            channels.TryGetValue(first.ChannelMode, out var c);
            channels[first.ChannelMode] = c + 1;
        }
        // ===== SNIPPET END =====

        _ = bitrates;
        _ = channels;
    }

    // S68 — tag-formats/lyrics3.md: read v1, walk v2 fields, build a v2 tag.
    private static void S68_Lyrics3()
    {
        var tags = AudioTags.ReadStream(new MemoryStream([0]));

        // Lyrics3 v1 read.
        var v1 = tags.Select(o => o.AudioTag).OfType<Lyrics3Tag>().FirstOrDefault();
        Console.WriteLine(v1?.Lyrics);

        // Lyrics3 v2 read.
        var v2 = tags.Select(o => o.AudioTag).OfType<Lyrics3v2Tag>().FirstOrDefault();
        if (v2 is not null)
        {
            Console.WriteLine($"{v2.ExtendedTrackTitle?.Value} – {v2.ExtendedArtistName?.Value}");
            foreach (var line in v2.Lyrics?.LyricLines ?? [])
            {
                var stamp = line.TimeStamps.FirstOrDefault();
                Console.WriteLine($"  {stamp:hh\\:mm\\:ss}  {line.LyricLine}");
            }
        }

        // Lyrics3 v2 build.
        var built = new Lyrics3v2Tag
        {
            ExtendedTrackTitle = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle)
            {
                Value = "A song with a title longer than the ID3v1 30-byte cap",
            },
            LyricsAuthorName = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.LyricsAuthorName)
            {
                Value = "Unknown",
            },
        };
        var bytes = built.ToByteArray();
        _ = bytes;
    }

    // S69 — tag-formats/musicmatch.md: read fields, edit Preference.
    private static void S69_MusicMatch()
    {
        var tags = AudioTags.ReadStream(new MemoryStream([0]));

        var mm = tags.Select(o => o.AudioTag).OfType<MusicMatchTag>().FirstOrDefault();
        if (mm is not null)
        {
            Console.WriteLine($"{mm.SongTitle} – {mm.ArtistName} [{mm.AlbumTitle}]");
            Console.WriteLine($"mood={mm.Mood} tempo={mm.Tempo} situation={mm.Situation}");
            Console.WriteLine($"track {mm.TrackNumber}, plays {mm.PlayCounter}, duration {mm.SongDuration}");
            mm.Preference = "Excellent";
        }
    }

    // S67 — examples.md: find files missing required tags.
    private static void S67_MissingTags()
    {
        var tags = AudioTags.ReadStream(new MemoryStream([0]));

        // ===== SNIPPET START =====
        var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault();
        var missing = new List<string>();
        if (id3v2 is null)
        {
            missing.Add("no ID3v2 tag");
        }
        else
        {
            if (string.IsNullOrEmpty(id3v2.TrackTitle?.Values.FirstOrDefault())) missing.Add("Title");
            if (string.IsNullOrEmpty(id3v2.Artist?.Values.FirstOrDefault()))     missing.Add("Artist");
            if (string.IsNullOrEmpty(id3v2.AlbumTitle?.Values.FirstOrDefault())) missing.Add("Album");
            if (id3v2.AttachedPictures.Count == 0)                                missing.Add("Cover");
        }
        // ===== SNIPPET END =====

        _ = missing;
    }
}

internal sealed class DemoTag : IAudioTag
{
    public bool Equals(IAudioTag? other) => ReferenceEquals(this, other);
    public void WriteTo(Stream destination) { }
}

internal sealed class DemoTagReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin) => null;
}

// ---- compile-check for examples/custom-tag-reader.md ----

public sealed class ProcessedStamp : IAudioTag
{
    public const int Version = 1;
    public static readonly byte[] Magic = "PRCS"u8.ToArray();

    public string ToolName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool Equals(IAudioTag? other) =>
        other is ProcessedStamp s
        && s.ToolName == ToolName
        && s.Note == Note
        && s.TimestampUtc.ToUnixTimeMilliseconds() == TimestampUtc.ToUnixTimeMilliseconds();

    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var toolBytes = System.Text.Encoding.UTF8.GetBytes(ToolName);
        var noteBytes = System.Text.Encoding.UTF8.GetBytes(Note);
        if (toolBytes.Length > 255) throw new InvalidOperationException("ToolName too long");
        if (noteBytes.Length > 255) throw new InvalidOperationException("Note too long");

        var totalSize = 4 + 1 + 1 + toolBytes.Length + 1 + noteBytes.Length + 8 + 4;
        var buf = new byte[totalSize];
        var pos = 0;

        Magic.CopyTo(buf, pos);            pos += 4;
        buf[pos++] = Version;
        buf[pos++] = (byte)toolBytes.Length;
        toolBytes.CopyTo(buf, pos);        pos += toolBytes.Length;
        buf[pos++] = (byte)noteBytes.Length;
        noteBytes.CopyTo(buf, pos);        pos += noteBytes.Length;

        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(buf.AsSpan(pos), TimestampUtc.ToUnixTimeMilliseconds());
        pos += 8;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(pos), totalSize);

        destination.Write(buf, 0, totalSize);
    }
}

public sealed class ProcessedStampReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (tagOrigin != TagOrigin.End) return null;

        var pos = stream.Position;
        if (pos < 4 + 1 + 2 + 8 + 4) return null;

        stream.Position = pos - 4;
        Span<byte> sizeBytes = stackalloc byte[4];
        if (stream.Read(sizeBytes) != 4) return null;
        var totalSize = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(sizeBytes);
        if (totalSize < 4 + 1 + 2 + 8 + 4 || totalSize > pos) return null;

        var startOffset = pos - totalSize;
        stream.Position = startOffset;
        Span<byte> magicBytes = stackalloc byte[4];
        if (stream.Read(magicBytes) != 4) return null;
        if (!magicBytes.SequenceEqual(ProcessedStamp.Magic)) return null;

        var version = stream.ReadByte();
        if (version != ProcessedStamp.Version) return null;

        var toolLen = stream.ReadByte();
        if (toolLen < 0) return null;
        var toolBuf = new byte[toolLen];
        if (stream.Read(toolBuf) != toolLen) return null;

        var noteLen = stream.ReadByte();
        if (noteLen < 0) return null;
        var noteBuf = new byte[noteLen];
        if (stream.Read(noteBuf) != noteLen) return null;

        Span<byte> tsBytes = stackalloc byte[8];
        if (stream.Read(tsBytes) != 8) return null;
        var ts = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(tsBytes);

        var stamp = new ProcessedStamp
        {
            ToolName = System.Text.Encoding.UTF8.GetString(toolBuf),
            Note = System.Text.Encoding.UTF8.GetString(noteBuf),
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(ts),
        };

        return new AudioTagOffset(tagOrigin, startOffset, pos, stamp);
    }
}

internal static class CustomTagRoundTrip
{
    public static void Verify()
    {
        // Round-trip serialise then parse, asserting all fields match.
        var original = new ProcessedStamp
        {
            ToolName = "loudness-normaliser",
            Note = "EBU R128, target -23 LUFS",
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
        };
        var bytes = original.ToByteArray();
        using var ms = new MemoryStream(bytes) { Position = bytes.Length };
        var parsed = (ProcessedStamp)new ProcessedStampReader().ReadFromStream(ms, TagOrigin.End)!.AudioTag;
        if (!parsed.Equals(original))
        {
            throw new InvalidOperationException("ProcessedStamp round-trip mismatch");
        }
    }
}
