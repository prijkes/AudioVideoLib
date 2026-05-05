---
_layout: landing
---

# AudioVideoLib

A .NET library for reading and writing audio metadata. Parses container
formats and tag formats — does **not** encode or decode audio samples.

## Install

```
dotnet add package AudioVideoLib
```

Requires .NET 10.

## Quick look

```csharp
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using var fs = File.OpenRead("track.mp3");

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
```

## Sections

- [**Getting started**](docs/getting-started.md) — open a file, read tags, save.
- [**Tag formats**](docs/tag-formats.md) — ID3v1/v2, APE, Lyrics3, MusicMatch, Vorbis, MP4/iTunes, ASF, Matroska.
- [**Container formats**](docs/container-formats.md) — MPEG audio, FLAC, WAV, AIFF, OGG, MP4, ASF, Matroska, DSF/DFF.
- [**Round-trip semantics**](docs/round-trip.md) — what's preserved, what's rewritten, when offsets shift.
- [**Extending**](docs/extending.md) — add a new tag reader or container walker.
- [**Examples**](docs/examples.md) — full programs that combine the readers and writers.
- [**API reference**](api/index.md) — every public type, auto-generated from the XML doc comments.

## License

MIT — see [LICENSE](https://github.com/audiovideolib/AudioVideoLib/blob/master/LICENSE).
