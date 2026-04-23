# Release notes

## 0.2.0 — 2026-04-19

### New formats

Round-trip read + write support for nine additional metadata formats:

- **WAV `LIST INFO`** — standard key/value metadata (INAM, IART, IPRD,
  ICRD, ICMT, IGNR, ITRK, IENG, ISFT, ICOP, …).
- **ID3v2-in-WAV** — full ID3v2 tag inside a WAV `id3 ` / `ID3 ` chunk.
- **BWF `bext` chunk** — broadcast Wave v0/v1/v2 with loudness fields
  and CodingHistory.
- **iXML chunk** — film/audio production metadata with BOM-preserving
  byte-identical round-trip.
- **AIFF text chunks** — NAME / AUTH / ANNO / COMT (timestamped).
- **DSF / DFF** — Sony / Philips DSD audio containers with embedded
  ID3v2.
- **MP4 / M4A iTunes ilst** — strongly-typed ilst items (text,
  trkn/disk, tmpo, cpil, covr JPEG/PNG, free-form `----`), with a
  splice-in writer that patches enclosing parent sizes.
- **ASF / WMA** — Content Description + Extended Content Description +
  Metadata + Metadata Library objects; `AsfTypedValue` tagged union.
- **Matroska / WebM** — Tags / Tag / SimpleTag (with nesting,
  TagBinary, TargetTypeValue), Duration extraction, splice-rewrite
  writer.

### Per-field encoding for ID3v1

ID3v1 fields are now stored as raw `byte[]` per field with per-field
`Encoding` overrides. The reader auto-detects UTF-8 per field. The
Studio ID3v1 editor gets a per-field encoding picker for Title /
Artist / Album / Comment.

### Non-fatal parse errors

A single malformed frame or item no longer aborts the whole file.
`AudioTags.AudioTagParseError` and `AudioTags.Id3v2FrameParseError`
surface the failure; the scanner continues.

### Fixes

- **APE items were parsed but never attached to the tag** — `ApeTag.Items`
  was always empty because `ApeTagReader` built the list locally and
  never called `tag.SetItems(items)`.
- **Multiple APE tags in one file** — now get a tab each in the Studio.
- **ID3v2 ISO 639-2/T language codes** (`nld`, `deu`, `fra`, `ces`, `zho`, …)
  accepted alongside the /B variants.
- **URL-link frame (`WOAF` etc.) `Data` getter** no longer throws
  `ArgumentNullException` on a just-added frame with unset `Url`.
- **OGG page CRC** recomputed on write.

### Tests

802 → 1064 passing (262 new tests, covering each new format's read and
round-trip paths plus the per-field encoding auto-detection).

## 0.1.0 — 2026-04-17

Initial release. MPEG audio, FLAC, and basic RIFF/WAV, AIFF, OGG
container support. ID3v1, ID3v2 (2.2–2.4), APE (v1/v2), Lyrics3 (v1/v2),
MusicMatch, Vorbis Comments.
