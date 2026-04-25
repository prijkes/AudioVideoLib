# Print a one-line summary for every audio file in a directory

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
