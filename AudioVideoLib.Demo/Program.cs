namespace AudioVideoLib.Demo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return PrintUsage();
        }

        try
        {
            return args[0] switch
            {
                "read" or "-r" => RequireArg(args, 1, "file") ?? ReadFile(args[1]),
                "tag" or "-t" => (RequireArg(args, 1, "in-file") ?? RequireArg(args, 2, "out-file")) ?? WriteAllTagFormats(args[1], args[2]),
                "frames" or "-f" => RequireArg(args, 1, "file") ?? PrintFrames(args[1]),
                "help" or "-h" or "--help" => PrintUsage(),
                _ => Fail($"unknown command: {args[0]}"),
            };
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine("AudioVideoLib.Demo — showcase CLI for the AudioVideoLib library");
        Console.WriteLine();
        Console.WriteLine("usage:");
        Console.WriteLine("  demo read    <file>            dump every detected tag + audio stream");
        Console.WriteLine("  demo frames  <file>            dump MPEG/VBR/LAME/FLAC stream info only");
        Console.WriteLine("  demo tag     <in> <out>        copy <in> to <out>, attaching sample tags");
        Console.WriteLine("                                 in every supported format");
        Console.WriteLine();
        Console.WriteLine("supported formats:");
        Console.WriteLine("  tags:   ID3v1, ID3v2 (2.2/2.3/2.4), APE v1/v2, Lyrics3 v1/v2,");
        Console.WriteLine("          MusicMatch, Vorbis Comments (via FLAC)");
        Console.WriteLine("  audio:  MPEG Audio (layers I/II/III, MPEG 1/2/2.5), FLAC");
        Console.WriteLine("  VBR:    Xing / Info / VBRI / LAME");
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        Console.Error.WriteLine("run 'demo help' for usage");
        return 1;
    }

    private static int? RequireArg(string[] args, int index, string name)
    {
        return index < args.Length ? null : Fail($"missing argument: {name}");
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// read
    ////------------------------------------------------------------------------------------------------------------------------------

    private static int ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            return Fail($"file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        PrintHeader($"file: {Path.GetFileName(path)} ({stream.Length:N0} bytes)");

        DumpAudioTags(stream);
        stream.Position = 0;
        DumpAudioStreams(stream);
        stream.Position = 0;
        DumpVorbisInsideFlac(stream);
        return 0;
    }

    private static void DumpAudioTags(Stream stream)
    {
        PrintSection("Tags");

        stream.Position = 0;
        var tags = AudioTags.ReadStream(stream).ToList();
        if (tags.Count == 0)
        {
            Console.WriteLine("  (none detected)");
            return;
        }

        foreach (var offset in tags)
        {
            var origin = offset.TagOrigin == TagOrigin.Start ? "start" : "end";
            Console.WriteLine($"  [{offset.AudioTag.GetType().Name}] @ {offset.StartOffset:N0}..{offset.EndOffset:N0} ({origin})");
            DumpTag(offset.AudioTag);
            Console.WriteLine();
        }
    }

    private static void DumpTag(IAudioTag tag)
    {
        switch (tag)
        {
            case Id3v1Tag v1: DumpId3v1(v1); break;
            case Id3v2Tag v2: DumpId3v2(v2); break;
            case ApeTag ape: DumpApe(ape); break;
            case Lyrics3Tag l3v1: DumpLyrics3v1(l3v1); break;
            case Lyrics3v2Tag l3v2: DumpLyrics3v2(l3v2); break;
            case MusicMatchTag mm: DumpMusicMatch(mm); break;
            default: Console.WriteLine($"    (no detailed dumper for {tag.GetType().Name})"); break;
        }
    }

    private static void DumpId3v1(Id3v1Tag tag)
    {
        Print("version", tag.Version);
        Print("title", tag.TrackTitle);
        Print("artist", tag.Artist);
        Print("album", tag.AlbumTitle);
        Print("year", tag.AlbumYear);
        Print("comment", tag.TrackComment);
        Print("track#", tag.TrackNumber == 0 ? "(none)" : tag.TrackNumber.ToString());
        Print("genre", tag.Genre);
        if (tag.UseExtendedTag)
        {
            Print("track-speed", tag.TrackSpeed);
            Print("ext-genre", tag.ExtendedTrackGenre);
            Print("start", tag.StartTime);
            Print("end", tag.EndTime);
        }
    }

    private static void DumpId3v2(Id3v2Tag tag)
    {
        Print("version", tag.Version);
        Print("use-unsync", tag.UseUnsynchronization);
        Print("use-ext-header", tag.UseExtendedHeader);
        Print("use-footer", tag.UseFooter);
        Print("experimental", tag.TagIsExperimental);
        Print("padding-size", tag.PaddingSize);
        Print("frame-count", tag.Frames.Count());

        if (tag.UseExtendedHeader && tag.ExtendedHeader != null)
        {
            Print("  ext-padding", tag.ExtendedHeader.PaddingSize);
            Print("  ext-crc", tag.ExtendedHeader.CrcDataPresent ? $"0x{tag.CalculateCrc32():X8}" : "(none)");
            Print("  ext-restricted", tag.ExtendedHeader.TagIsRestricted);
        }

        var frameGroups = tag.Frames
            .GroupBy(f => f.Identifier, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key);

        foreach (var group in frameGroups)
        {
            var count = group.Count();
            var summary = count == 1
                ? DescribeFrame(group.First())
                : $"{count} frames";
            Console.WriteLine($"    {group.Key,-5} {summary}");
        }
    }

    private static string DescribeFrame(Id3v2Frame frame)
    {
        return frame switch
        {
            Id3v2TextFrame text => string.Join(" / ", text.Values),
            Id3v2UserDefinedTextInformationFrame utxt => $"[{utxt.Description}] = {utxt.Value}",
            Id3v2UrlLinkFrame url => url.Url ?? "(empty)",
            Id3v2UserDefinedUrlLinkFrame uurl => $"[{uurl.Description}] = {uurl.Url}",
            Id3v2CommentFrame comm => $"[{comm.Language}:{comm.ShortContentDescription}] {comm.Text}",
            Id3v2UnsynchronizedLyricsFrame uslt => $"[{uslt.Language}:{uslt.ContentDescriptor}] {Trunc(uslt.Lyrics, 60)}",
            Id3v2AttachedPictureFrame pic => $"{pic.ImageFormat} {pic.PictureType} {pic.PictureData?.Length ?? 0:N0} bytes",
            Id3v2PrivateFrame priv => $"[{priv.OwnerIdentifier}] {priv.PrivateData?.Length ?? 0:N0} bytes",
            Id3v2UniqueFileIdentifierFrame ufid => $"[{ufid.OwnerIdentifier}] {ufid.IdentifierData?.Length ?? 0:N0} bytes",
            _ => $"({frame.Data?.Length ?? 0:N0} bytes)",
        };
    }

    private static void DumpApe(ApeTag tag)
    {
        Print("version", tag.Version);
        Print("use-header", tag.UseHeader);
        Print("use-footer", tag.UseFooter);
        Print("read-only", tag.IsReadOnly);
        Print("item-count", tag.Items.Count());
        foreach (var item in tag.Items.OrderBy(i => i.Key))
        {
            var valueSummary = item switch
            {
                // ApeLocatorItem derives from ApeUtf8Item — match it first.
                ApeLocatorItem loc => string.Join(" / ", loc.Values),
                ApeUtf8Item utf8 => string.Join(" / ", utf8.Values),
                ApeBinaryItem bin => $"<binary {bin.Data?.Length ?? 0:N0} bytes>",
                _ => $"<{item.GetType().Name}>",
            };
            Console.WriteLine($"    {item.Key,-24} {valueSummary}");
        }
    }

    private static void DumpLyrics3v1(Lyrics3Tag tag)
    {
        Print("lyrics", Trunc(tag.Lyrics, 120));
    }

    private static void DumpLyrics3v2(Lyrics3v2Tag tag)
    {
        Print("field-count", tag.Fields.Count());
        foreach (var field in tag.Fields)
        {
            var content = field switch
            {
                Lyrics3v2TextField text => Trunc(text.Value, 80),
                _ when field.Data is { Length: > 0 } d => Trunc(Encoding.Latin1.GetString(d), 80),
                _ => string.Empty,
            };
            Console.WriteLine($"    {field.Identifier,-5} {content}");
        }
    }

    private static void DumpMusicMatch(MusicMatchTag tag)
    {
        Print("version", tag.Version.Trim());
        Print("xing-encoder", tag.XingEncoderVersion);
        Print("use-header", tag.UseHeader);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// audio streams (MPEG / FLAC)
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void DumpAudioStreams(Stream stream)
    {
        PrintSection("Audio streams");

        stream.Position = 0;
        using var streams = MediaContainers.ReadStream(stream);
        var streamList = streams.ToList();
        if (streamList.Count == 0)
        {
            Console.WriteLine("  (none detected)");
            return;
        }

        foreach (var audioStream in streamList)
        {
            Console.WriteLine($"  [{audioStream.GetType().Name}] @ {audioStream.StartOffset:N0}..{audioStream.EndOffset:N0}");
            switch (audioStream)
            {
                case MpaStream mpa: DumpMpa(mpa); break;
                case FlacStream flac: DumpFlac(flac); break;
                default: Console.WriteLine($"    (no detailed dumper for {audioStream.GetType().Name})"); break;
            }
            Console.WriteLine();
        }
    }

    private static void DumpMpa(MpaStream mpa)
    {
        var frames = mpa.Frames.ToList();
        Print("frame-count", frames.Count);
        if (frames.Count == 0)
        {
            return;
        }

        var first = frames[0];
        var hasVbr = mpa.VbrHeader != null;
        Print("  mpeg", first.AudioVersion);
        Print("  layer", first.LayerVersion);
        Print("  bitrate", $"{first.Bitrate} kbps{(hasVbr ? " (VBR)" : string.Empty)}");
        Print("  sampling-rate", $"{first.SamplingRate} Hz");
        Print("  channels", first.ChannelMode);
        Print("  padded", first.IsPadded);
        Print("  frame-length", $"{first.FrameLength} bytes");
        Print("  total-length", FormatDuration(mpa.TotalDuration));

        if (mpa.VbrHeader is { } vbr)
        {
            Print("vbr", vbr.HeaderType);
            Print("  name", vbr.Name);
            Print("  frame-count", vbr.FrameCount);
            Print("  file-size", $"{vbr.FileSize:N0} bytes");
            Print("  quality", vbr.Quality);
            if (vbr.LameTag is { } lame)
            {
                Print("  lame-encoder", lame.EncoderVersion);
                Print("  lame-revision", lame.InfoTagRevision);
                Print("  lame-vbr-method", lame.VbrMethod);
                Print("  lame-lowpass-hz", lame.LowpassFilterValue);
                Print("  lame-music-crc", $"0x{lame.MusicCrc:X4}");
                Print("  lame-info-crc", $"0x{lame.InfoTagCrc:X4}");
            }
        }
    }

    private static void DumpFlac(FlacStream flac)
    {
        var blocks = flac.MetadataBlocks.ToList();
        Print("metadata-block-count", blocks.Count);
        foreach (var block in blocks)
        {
            Console.WriteLine($"    {block.BlockType,-18} {block.Data?.Length ?? 0:N0} bytes");
            if (block is FlacStreamInfoMetadataBlock info)
            {
                Print("      sample-rate", $"{info.SampleRate} Hz");
                Print("      channels", info.Channels);
                Print("      bits-per-sample", info.BitsPerSample);
                Print("      total-samples", info.TotalSamples);
            }
        }
        Print("frame-count", flac.Frames.Count());
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// vorbis (inside FLAC)
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void DumpVorbisInsideFlac(Stream stream)
    {
        stream.Position = 0;
        using var audioStreams = MediaContainers.ReadStream(stream);
        var flac = audioStreams.OfType<FlacStream>().FirstOrDefault();
        if (flac == null)
        {
            return;
        }

        var vorbisBlock = flac.MetadataBlocks.OfType<FlacVorbisCommentsMetadataBlock>().FirstOrDefault();
        if (vorbisBlock?.VorbisComments == null)
        {
            return;
        }

        PrintSection("Vorbis comments");
        Print("vendor", vorbisBlock.VorbisComments.Vendor);
        Print("comment-count", vorbisBlock.VorbisComments.Comments.Count);
        foreach (var comment in vorbisBlock.VorbisComments.Comments)
        {
            Console.WriteLine($"    {comment.Name,-16} {comment.Value}");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// frames (audio-only dump, no tags)
    ////------------------------------------------------------------------------------------------------------------------------------

    private static int PrintFrames(string path)
    {
        if (!File.Exists(path))
        {
            return Fail($"file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        PrintHeader($"file: {Path.GetFileName(path)} ({stream.Length:N0} bytes)");
        DumpAudioStreams(stream);
        return 0;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// tag (copy input file + attach sample tags in every format)
    ////------------------------------------------------------------------------------------------------------------------------------

    private static int WriteAllTagFormats(string inPath, string outPath)
    {
        if (!File.Exists(inPath))
        {
            return Fail($"file not found: {inPath}");
        }

        var audio = File.ReadAllBytes(inPath);

        PrintHeader($"attaching sample tags to {Path.GetFileName(outPath)}");

        var prefixTags = new List<byte[]>();
        var suffixTags = new List<byte[]>();

        // Id3v2.4 at start.
        var id3v2 = BuildSampleId3v2();
        prefixTags.Add(id3v2.ToByteArray());
        Console.WriteLine($"  + Id3v2.4      {id3v2.Frames.Count()} frames");

        // Suffix order follows the conventional tail layout:
        //   audio → Lyrics3v2 → APEv2 → Id3v1
        // Id3v1 always occupies the final 128 bytes; APEv2 usually sits directly in
        // front of it; Lyrics3v2 predates APE and sits further back.

        var lyrics = BuildSampleLyrics3();
        suffixTags.Add(lyrics.ToByteArray());
        Console.WriteLine($"  + Lyrics3v2    {lyrics.Fields.Count()} fields");

        var ape = BuildSampleApe();
        suffixTags.Add(ape.ToByteArray());
        Console.WriteLine($"  + APEv2        {ape.Items.Count()} items");

        var id3v1 = BuildSampleId3v1();
        suffixTags.Add(id3v1.ToByteArray());
        Console.WriteLine($"  + Id3v1        {id3v1.TrackTitle} / {id3v1.Artist}");

        using (var outFile = File.Create(outPath))
        {
            foreach (var tag in prefixTags)
            {
                outFile.Write(tag, 0, tag.Length);
            }

            outFile.Write(audio, 0, audio.Length);

            foreach (var tag in suffixTags)
            {
                outFile.Write(tag, 0, tag.Length);
            }
        }

        var finalSize = new FileInfo(outPath).Length;
        Console.WriteLine($"  wrote {finalSize:N0} bytes");

        // Verify by reading back
        Console.WriteLine();
        Console.WriteLine("verifying round-trip:");
        using var verifyStream = File.OpenRead(outPath);
        var readBack = AudioTags.ReadStream(verifyStream).ToList();
        foreach (var offset in readBack)
        {
            Console.WriteLine($"  [x] {offset.AudioTag.GetType().Name} @ {offset.StartOffset:N0}..{offset.EndOffset:N0}");
        }

        return 0;
    }

    private static Id3v2Tag BuildSampleId3v2()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            PaddingSize = 1024,
        };
        tag.SetFrame(NewText("TIT2", "Demo Track"));
        tag.SetFrame(NewText("TPE1", "AudioVideoLib"));
        tag.SetFrame(NewText("TALB", "Demo Album"));
        tag.SetFrame(NewText("TDRC", "2026"));
        tag.SetFrame(NewText("TRCK", "1/1"));
        tag.SetFrame(NewText("TCON", "Test"));

        var comm = new Id3v2CommentFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            Language = "eng",
            ShortContentDescription = "demo",
            Text = "Sample tag written by AudioVideoLib.Demo",
        };
        tag.SetFrame(comm);

        var wcop = new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, "WCOP") { Url = "http://example.org/copyright" };
        tag.SetFrame(wcop);

        return tag;

        static Id3v2TextFrame NewText(string id, string value)
        {
            var f = new Id3v2TextFrame(Id3v2Version.Id3v240, id) { TextEncoding = Id3v2FrameEncodingType.UTF8 };
            f.Values.Add(value);
            return f;
        }
    }

    private static ApeTag BuildSampleApe()
    {
        var tag = new ApeTag(ApeVersion.Version2)
        {
            UseFooter = true,
            UseHeader = true,
        };
        tag.SetItem(NewUtf8("Title", "Demo Track"));
        tag.SetItem(NewUtf8("Artist", "AudioVideoLib"));
        tag.SetItem(NewUtf8("Album", "Demo Album"));
        tag.SetItem(NewUtf8("Year", "2026"));
        tag.SetItem(NewUtf8("Track", "1/1"));
        tag.SetItem(NewUtf8("Genre", "Test"));
        tag.SetItem(NewUtf8("Comment", "Sample APE tag written by AudioVideoLib.Demo"));
        return tag;

        static ApeUtf8Item NewUtf8(string key, string value)
        {
            var item = new ApeUtf8Item(ApeVersion.Version2, key);
            item.Values.Add(value);
            return item;
        }
    }

    private static Lyrics3v2Tag BuildSampleLyrics3()
    {
        return new Lyrics3v2Tag
        {
            AdditionalInformation = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.AdditionalInformation) { Value = "10" },
            ExtendedAlbumName = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName) { Value = "Demo Album" },
            ExtendedArtistName = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName) { Value = "AudioVideoLib" },
            ExtendedTrackTitle = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle) { Value = "Demo Track" },
        };
    }

    private static Id3v1Tag BuildSampleId3v1()
    {
        return new Id3v1Tag(Id3v1Version.Id3v11)
        {
            TrackTitle = "Demo Track",
            Artist = "AudioVideoLib",
            AlbumTitle = "Demo Album",
            AlbumYear = "2026",
            TrackComment = "written by demo",
            TrackNumber = 1,
            Genre = Id3v1Genre.Other,
        };
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// formatting helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 72));
        Console.WriteLine($" {title}");
        Console.WriteLine(new string('=', 72));
    }

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"-- {title} " + new string('-', Math.Max(0, 68 - title.Length)));
    }

    private static void Print(string label, object? value)
    {
        Console.WriteLine($"    {label,-18} {value ?? "(null)"}");
    }

    private static string FormatDuration(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        var oneLine = s.Replace('\n', ' ').Replace('\r', ' ');
        return oneLine.Length <= max ? oneLine : oneLine[..max] + "…";
    }
}
