# AudioVideoLib

AudioVideoLib is a .NET library for reading and writing metadata in audio files.
It does **not** encode or decode audio itself — it parses containers, frame
headers, and tag formats so you can inspect and edit them.

## Install

```
dotnet add package AudioVideoLib
```

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
| `AudioVideoLib.Cli` | `avs` command-line tool: `info`, `validate`, `batch`. |
| `AudioVideoLib.Studio` | WPF file inspector with per-byte hex highlighting and tag editing. |

## Supported formats

### Audio streams / containers
* MPEG-1 Audio (ISO/IEC 11172-3) — Layer I, II, III
* MPEG-2 Audio (ISO/IEC 13818-3)
* MPEG-2.5 Audio (unofficial)
* FLAC
* RIFF / WAV (chunk walking)
* AIFF / AIFF-C (chunk walking, 80-bit IEEE 754 sample-rate decoding)
* OGG (page walking)

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

A WPF application for inspecting one or more audio files. Features:

* **Inspector tab** — tree view of every structure in the file, property grid, and
  hex view with byte-level highlighting for the selected property.
* **Tags tab** — per-format editors for each tag found in the file (ID3v1, ID3v2,
  APE, Lyrics3, MusicMatch, Vorbis Comments), plus add/remove tag formats.
* **Multi-file tabs** — open and switch between multiple files; per-file selection
  is preserved.
* **Validation & lint** — MPEG frame CRC checks, ID3v2 unsynchronization sanity,
  structure overlap/gap detection; export the report as Markdown or plain text.
* **Diff** — side-by-side comparison of tags and structures between two files.
* **Analysis** — bitrate/size graphs and sanity rows for the audio stream.
* **Playback** — basic transport controls backed by `MediaPlayer`.
* **Batch scanner** — recursively scan a folder and surface lint findings across
  many files.
* **Save / Save As** — rewrites the file preserving untouched structures.
* **Settings** — configurable hex bytes-per-row, font size, and tree truncation
  caps.
* Dark theme.

## License

MIT — see [LICENSE](LICENSE).

## Credits

Format references and prior-art implementations consulted while writing this
library are listed in [CREDITS.md](CREDITS.md).
