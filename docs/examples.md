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
| [Bulk-remove an ID3v2 frame](examples/bulk-remove-frame.md) | strip every `PRIV` / `GEOB` across a library; `RemoveFrames` + `AudioInfo.Save` |
| [Set the ID3v2 padding budget](examples/edit-id3v2-padding.md) | `Id3v2Tag.PaddingSize` for in-place edits later |
| [Inspect a file's layout](examples/inspect-layout.md) | print every tag + container offset / size as a layout map |
| [Inspect a Musepack / WavPack / TrueAudio / Monkey's Audio file](examples/inspect-layout.md) | the layout-map example also covers `MpcStream`, `WavPackStream`, `TtaStream`, `MacStream` — `MediaContainers.ReadStream` returns the right walker for each |
| [MP3 bitrate / channel-mode audit](examples/mpa-bitrate-histogram.md) | walk a library, histogram first-frame `Bitrate` / `ChannelMode` |
| [Find files missing required tags](examples/find-missing-tags.md) | gate a release on Title / Artist / Album / Cover presence |
| [Register a custom tag reader](examples/custom-tag-reader.md) | `IAudioTagReader` + `AudioTags.AddReader<TR, TT>()` |
