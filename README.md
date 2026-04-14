# AudioVideoLib

AudioVideoLib is a .NET library for reading and writing metadata in audio files.
It does **not** encode or decode audio itself — it parses containers, frame
headers, and tag formats so you can inspect and edit them.

## Requirements

* .NET 10
* C# 14

## Solution layout

| Project | Purpose |
| --- | --- |
| `AudioVideoLib` | The core library (formats, tags, streams). |
| `AudioVideoLib.Tests` | xUnit test suite (800+ tests). |
| `AudioVideoLib.Samples` | Example custom frame/tag implementations. |
| `AudioVideoLib.Demo` | CLI tool that exercises every feature of the library. |
| `AudioVideoLib.Studio` | WPF file inspector with per-byte hex highlighting and tag editing. |

## Supported formats

### Audio streams
* MPEG-1 Audio (ISO/IEC 11172-3) — Layer I, II, III
* MPEG-2 Audio (ISO/IEC 13818-3)
* MPEG-2.5 Audio (unofficial)
* FLAC

### Metadata / tags
* APE Tag (v1, v2)
* ID3v1 (1.0, 1.1, 1.1 Extended / TAG+)
* ID3v2 (2.2.0, 2.2.1, 2.3.0, 2.4.0)
* Lyrics3 (v1, v2)
* MusicMatch Tag
* Vorbis Comments (standalone and inside FLAC)

### VBR headers
* Xing
* VBRI
* LAME

## AudioVideoLib.Studio

A WPF application for inspecting a single audio file at a time. Features:

* **Inspector tab** — tree view of every structure in the file, property grid, and
  hex view with byte-level highlighting for the selected property.
* **Tags tab** — per-format editors for each tag found in the file (ID3v1, ID3v2,
  APE, Lyrics3, MusicMatch, Vorbis Comments), plus add/remove tag formats.
* **Save / Save As** — rewrites the file preserving untouched structures.
* Dark theme.

## License

TBD.

## Credits

Format references and prior-art implementations consulted while writing this
library are listed in [CREDITS.md](CREDITS.md).
