# Examples

Self-contained, runnable programs that combine multiple APIs. Each
listing is a complete `Program.cs` — drop it into a `dotnet new console`
project that references `AudioVideoLib` and run it against a real file.

The shorter inline snippets that demonstrate single-API usage live in
the format pages — see [Tag formats](tag-formats.md) and
[Container formats](container-formats.md).

---

## Print a one-line summary for every audio file in a directory

Walks a folder, opens each file, and prints title / artist / duration.
Demonstrates `AudioTags`, `MediaContainers`, the strongly-typed ID3v2
properties, and the `Mp4MetaTag` / `MatroskaTag` / `AsfMetadataTag` /
`RiffStream.InfoTag` / `FlacStream.VorbisCommentsMetadataBlock` fall-throughs.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: summary <directory>");
    return 1;
}

foreach (var path in Directory.EnumerateFiles(args[0]).OrderBy(p => p))
{
    using var fs = File.OpenRead(path);

    var tags = AudioTags.ReadStream(fs);
    fs.Position = 0;
    var streams = MediaContainers.ReadStream(fs);

    var title  = TitleFromTags(tags) ?? TitleFromContainers(streams);
    var artist = ArtistFromTags(tags) ?? ArtistFromContainers(streams);
    var totalMs = streams.Sum(s => s.TotalDuration);

    Console.WriteLine($"{Path.GetFileName(path),-40}  {artist} – {title}  ({totalMs} ms)");
}

return 0;

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

static string? ArtistFromTags(AudioTags tags)
{
    foreach (var offset in tags)
    {
        switch (offset.AudioTag)
        {
            case Id3v2Tag id3v2:
                if (id3v2.Artist?.Values.FirstOrDefault() is { Length: > 0 } v) return v;
                break;
            case Id3v1Tag id3v1:
                if (!string.IsNullOrEmpty(id3v1.Artist)) return id3v1.Artist;
                break;
            case ApeTag ape:
                var apeArtist = ape.GetItem<ApeUtf8Item>(ApeItemKey.Artist);
                if (apeArtist?.Values.FirstOrDefault() is { Length: > 0 } a) return a;
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

static string? ArtistFromContainers(MediaContainers streams)
{
    foreach (var s in streams)
    {
        var a = s switch
        {
            Mp4Stream mp4 => mp4.Tag.Artist,
            AsfStream asf => asf.MetadataTag.Author,
            MatroskaStream mkv => mkv.Tag.Artist,
            RiffStream riff => riff.InfoTag?.Artist,
            _ => null,
        };
        if (!string.IsNullOrEmpty(a)) return a;
    }
    return null;
}
```

---

## Migrate ID3v2 → APEv2 alongside the original file

Reads the ID3v2 tag from each input file and emits a sidecar `.apev2`
binary containing the equivalent APEv2 tag. Demonstrates the
two-model walk that underpins format-to-format conversion.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: id3v2-to-ape <directory>");
    return 1;
}

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var tags = AudioTags.ReadStream(fs);
    if (tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault() is not { } id3v2)
    {
        continue;
    }

    var ape = new ApeTag(ApeVersion.Version2) { UseHeader = true, UseFooter = true };

    Copy(id3v2.TrackTitle,    ape, ApeItemKey.Title);
    Copy(id3v2.Artist,        ape, ApeItemKey.Artist);
    Copy(id3v2.AlbumTitle,    ape, ApeItemKey.AlbumName);
    Copy(id3v2.ComposerName,  ape, ApeItemKey.Composer);
    Copy(id3v2.ConductorName, ape, ApeItemKey.Conductor);
    Copy(id3v2.RecordingTime, ape, ApeItemKey.Year);
    Copy(id3v2.YearRecording, ape, ApeItemKey.Year);

    foreach (var apic in id3v2.AttachedPictures)
    {
        var key = apic.PictureType == Id3v2AttachedPictureType.CoverFront
            ? "Cover Art (Front)"
            : $"Cover Art ({apic.PictureType})";
        ape.SetItem(new ApeBinaryItem(ape.Version, key) { Data = apic.PictureData });
    }

    File.WriteAllBytes(Path.ChangeExtension(path, ".apev2"), ape.ToByteArray());
}

return 0;

static void Copy(Id3v2TextFrame? source, ApeTag ape, ApeItemKey key)
{
    if (source is null || source.Values.Count == 0) return;
    var item = new ApeUtf8Item(ape.Version, key);
    foreach (var v in source.Values) item.Values.Add(v);
    ape.SetItem(item);
}
```

---

## Bulk-convert ID3v2.4 frame text encodings

Walks every `.mp3` in a directory, switches all text frames in v2.4
tags to UTF-8, and writes the rebuilt tag bytes back into a sidecar.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var tags = AudioTags.ReadStream(fs);
    if (tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault() is not { } id3v2)
    {
        continue;
    }
    if (id3v2.Version < Id3v2Version.Id3v240)
    {
        continue;
    }

    var changed = 0;
    foreach (var frame in id3v2.GetFrames<Id3v2TextFrame>())
    {
        if (frame.TextEncoding == Id3v2FrameEncodingType.UTF8)
        {
            continue;
        }
        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
        changed++;
    }

    if (changed > 0)
    {
        File.WriteAllBytes(Path.ChangeExtension(path, ".id3v2"), id3v2.ToByteArray());
        Console.WriteLine($"{Path.GetFileName(path)}: re-encoded {changed} frame(s) to UTF-8");
    }
}

return 0;
```

---

## Extract every embedded picture, regardless of format

Pulls cover art out of ID3v2 (`APIC`), FLAC (`PICTURE` block), and
MP4 (`covr` atom) — all in one pass. Output filenames encode the
source container and picture type so multiple covers don't collide.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: extract-art <input-file> <output-dir>");
    return 1;
}

var (inputPath, outDir) = (args[0], args[1]);
Directory.CreateDirectory(outDir);

using var fs = File.OpenRead(inputPath);
var tags = AudioTags.ReadStream(fs);
fs.Position = 0;
var streams = MediaContainers.ReadStream(fs);

var stem = Path.GetFileNameWithoutExtension(inputPath);
var written = 0;

foreach (var id3v2 in tags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
{
    foreach (var pic in id3v2.AttachedPictures)
    {
        var ext = pic.ImageFormat.Split('/').Last();
        var name = $"{stem}.id3v2-{pic.PictureType}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), pic.PictureData);
        written++;
    }
}

foreach (var flac in streams.OfType<FlacStream>())
{
    foreach (var pic in flac.PictureMetadataBlocks)
    {
        var ext = pic.MimeType.Split('/').Last();
        var name = $"{stem}.flac-{pic.PictureType}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), pic.PictureData);
        written++;
    }
}

foreach (var mp4 in streams.OfType<Mp4Stream>())
{
    var idx = 0;
    foreach (var cover in mp4.Tag.CoverArt)
    {
        var ext = cover.Format.ToString().ToLowerInvariant();
        var name = $"{stem}.mp4-{idx++}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), cover.Data);
        written++;
    }
}

Console.WriteLine($"Wrote {written} picture(s) for {stem}.");
return 0;
```

---

## Report broadcast-WAV (BWF) metadata

Pulls the `bext` chunk out of every `.wav` in a directory and prints
the description / originator / timestamp / loudness fields.
Demonstrates `RiffStream.BextChunk`.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

foreach (var path in Directory.EnumerateFiles(args[0], "*.wav"))
{
    using var fs = File.OpenRead(path);
    var streams = MediaContainers.ReadStream(fs);
    var riff = streams.OfType<RiffStream>().FirstOrDefault();
    if (riff?.BextChunk is not { } bext)
    {
        continue;
    }

    Console.WriteLine($"== {Path.GetFileName(path)} ==");
    Console.WriteLine($"  Description : {bext.Description}");
    Console.WriteLine($"  Originator  : {bext.Originator} / {bext.OriginatorReference}");
    Console.WriteLine($"  Timestamp   : {bext.OriginationDate} {bext.OriginationTime}");
    Console.WriteLine($"  TimeRef     : {bext.TimeReference}");
    if (bext.Version >= 1)
    {
        Console.WriteLine($"  Loudness v={bext.LoudnessValue}, range={bext.LoudnessRange}, peak={bext.MaxTruePeakLevel}");
    }
    Console.WriteLine($"  History     : {bext.CodingHistory.TrimEnd()}");
}
```

---

## Validate MPEG audio frames and report VBR header / LAME tag

Useful sanity check for an MP3 archive. Counts frames, verifies CRC
where present, and reports the VBR header (Xing / VBRI) plus any
LAME tag fields.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var streams = MediaContainers.ReadStream(fs);
    var mpa = streams.OfType<MpaStream>().FirstOrDefault();
    if (mpa is null)
    {
        Console.WriteLine($"{Path.GetFileName(path)}: no MPEG audio stream found");
        continue;
    }

    var crcFailures = mpa.Frames.Count(f => f.IsCrcProtected && f.Crc != f.CalculateCrc());
    Console.WriteLine($"{Path.GetFileName(path)}: {mpa.Frames.Count():N0} frames, {mpa.TotalDuration:N0} ms, {crcFailures} CRC failures");

    if (mpa.VbrHeader is { } vbr)
    {
        Console.WriteLine($"  VBR : {vbr.HeaderType} ({vbr.FrameCount} frames, {vbr.FileSize} bytes)");
        if (vbr.LameTag is { } lame)
        {
            Console.WriteLine($"  LAME: {lame.EncoderVersion} – {lame.VbrMethodName}, lowpass {lame.LowpassFilterValue} Hz");
        }
    }
}
```

---

## Register a custom tag reader

`AudioTags.AddReader<TR, TT>` plugs in any `IAudioTagReader`. Useful
for proprietary headers or trailers that aren't part of the built-in
list.

```csharp
using System.IO;

using AudioVideoLib.Tags;

public sealed class MyTagReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        // Probe; return null if not a match. On a match, build a MyTag and
        // return an AudioTagOffset(tagOrigin, startOffset, endOffset, tag).
        return null;
    }
}

public sealed class MyTag : IAudioTag
{
    public bool Equals(IAudioTag? other) => ReferenceEquals(this, other);
    public byte[] ToByteArray() => [];
}

// Wire it in alongside the built-in readers.
var tags = new AudioTags();
tags.AddReader<MyTagReader, MyTag>();
tags.ReadTags(File.OpenRead("track.mp3"));
```
