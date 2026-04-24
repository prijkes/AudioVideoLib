# Getting started

## Install

```
dotnet add package AudioVideoLib
```

Requires **.NET 10**. The package is MIT-licensed and ships with
SourceLink + symbol packages so step-through debugging works from
NuGet.

## The two entry points

AudioVideoLib has two orthogonal readers. Most callers use both.

### `AudioTags` — flat tag scanner

Finds tags that live at known byte positions (start or end of file):

- ID3v1 (v1.0, v1.1, 1.1 Extended / TAG+)
- ID3v2 (2.2.0, 2.2.1, 2.3.0, 2.4.0)
- APE (v1, v2)
- Lyrics3 (v1, v2)
- MusicMatch

```csharp
using var fs = File.OpenRead("track.mp3");
var tags = AudioTags.ReadStream(fs);

foreach (var offset in tags)
{
    Console.WriteLine(
        $"{offset.AudioTag.GetType().Name} "
        + $"at 0x{offset.StartOffset:X8}..0x{offset.EndOffset:X8} "
        + $"({offset.TagOrigin})");
}
```

### `MediaContainers` — container probe

Probes for container formats and returns whatever walker matches:

- MPEG audio (Layer I/II/III, v1/v2/2.5)
- FLAC
- RIFF / WAV, RIFX
- AIFF, AIFF-C
- OGG
- MP4 / M4A (ISO/IEC 14496-12 with iTunes-style metadata)
- ASF / WMA / WMV
- Matroska / WebM
- DSF (Sony), DFF (Philips)

```csharp
fs.Position = 0;
var streams = MediaContainers.ReadStream(fs);

foreach (var stream in streams)
{
    if (stream is MpaStream mpa)
    {
        Console.WriteLine($"MPEG: {mpa.Frames.Count()} frames, {mpa.TotalDuration:N0} ms");
    }
    else if (stream is FlacStream flac)
    {
        Console.WriteLine($"FLAC: {flac.Frames.Count()} frames");
    }
    // … etc.
}
```

## Container-embedded tags

MP4, ASF, Matroska, WAV (LIST/INFO), AIFF (NAME/AUTH/ANNO/COMT), DSF, and
DFF all carry metadata inside their container structure rather than at
flat file positions. Those tags surface on the container walker, not
through `AudioTags`:

```csharp
if (streams.OfType<Mp4Stream>().FirstOrDefault() is { Tag: var m4a })
{
    Console.WriteLine($"iTunes title: {m4a.Title}");
}
if (streams.OfType<RiffStream>().FirstOrDefault() is { InfoTag: var info } && info is not null)
{
    Console.WriteLine($"WAV INFO title: {info.Title}");
}
```

## Modifying and saving

Every tag model exposes a `ToByteArray()`:

```csharp
var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();
id3v2.Title = "New title";
var rewrittenTag = id3v2.ToByteArray();
```

For container-embedded tags, the walker itself has `ToByteArray()` that
rewrites the entire file with the updated metadata spliced in:

```csharp
var mp4 = streams.OfType<Mp4Stream>().Single();
mp4.Tag.Title = "New title";
File.WriteAllBytes("out.m4a", mp4.ToByteArray());
```

See [Round-trip semantics](round-trip.md) for what's preserved and
what's rebuilt.

## Non-fatal parse errors

A single malformed frame or item no longer aborts the whole file. The
library surfaces each failure via an event and skips the bad item:

```csharp
var tags = new AudioTags();
tags.Id3v2FrameParseError += (_, e) =>
    Console.Error.WriteLine($"skipped ID3v2 frame at 0x{e.StartOffset:X8}: {e.Exception.Message}");
tags.AudioTagParseError += (_, e) =>
    Console.Error.WriteLine($"skipped {e.Reader.GetType().Name} at 0x{e.StartOffset:X8}: {e.Exception.Message}");
tags.ReadTags(fs);
```

## Next

- [Tag formats](tag-formats.md) — per-format deep dives.
- [Container formats](container-formats.md) — per-container deep dives.
- [Extending](extending.md) — wire up your own format.
