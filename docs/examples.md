# Examples

Self-contained, runnable programs that combine multiple APIs. Each
example is a complete `Program.cs` — drop it into a `dotnet new console`
project that references `AudioVideoLib` and run it against a real file.

The shorter inline snippets that demonstrate single-API usage live in
the format pages — see [Tag formats](tag-formats.md) and
[Container formats](container-formats.md).

| Scenario | What it shows |
|---|---|
| [One-line summary per file](examples/summary-per-file.md) | combining `AudioTags` + `MediaContainers`, falling through ID3v2 / ID3v1 / APE / MP4 / ASF / Matroska / WAV |
| [Migrate ID3v2 → APEv2](examples/id3v2-to-ape.md) | format-to-format conversion, including `APIC` → APE binary cover-art |
| [Bulk-convert ID3v2.4 frame text encodings](examples/bulk-encoding-conversion.md) | walking every text frame, gating on tag version, sidecar output |
| [Cross-format cover-art extractor](examples/cover-art-extractor.md) | one pass across `APIC`, FLAC `PICTURE`, and MP4 `covr` |
| [BWF metadata report](examples/bwf-report.md) | `RiffStream.BextChunk`, including the v1+ loudness fields |
| [Validate MPEG audio](examples/validate-mpa.md) | `MpaStream` frame count + per-frame CRC + Xing/VBRI/LAME header |
| [Register a custom tag reader](examples/custom-tag-reader.md) | `IAudioTagReader` + `AudioTags.AddReader<TR, TT>()` |
