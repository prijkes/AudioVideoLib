# AudioVideoLib

The core parsing and serialization library. No UI, no I/O beyond what
callers hand in as `Stream`s. Targets `net10.0`, C# 14, nullable enabled,
warnings treated as errors.

## Directory layout

| Folder | Purpose |
| --- | --- |
| `Collections/` | Generic support — `NotifyingList<T>` and its event args, used to let tag readers raise per-item parse events. |
| `Cryptography/` | CRC helpers (`Crc8`, `Crc16`) and compression shims used by ID3v2 frame flags. |
| `Formats/` | Plain records and readers for non-tag data: MPEG audio frames and headers (`MpaFrame`, `MpaFrameHeader`), FLAC metadata blocks / frames / sub-frames, VBR / Xing / LAME / VBRI headers, RIFF / AIFF chunks, OGG pages, MP4 boxes + ilst items, DSD chunks, EBML elements, BWF bext, iXML, and the container-level tag models (`RiffInfoTag`, `AiffTextChunks`). |
| `IO/` | Stream walkers (`IAudioStream` implementations): `MpaStream`, `FlacStream`, `RiffStream`, `AiffStream`, `OggStream`, `DsfStream`, `DffStream`, `Mp4Stream`, `AsfStream`, `MatroskaStream`. Plus `AudioStreams` (the factory/registry that probes a stream and returns matching walkers) and the seekable buffer primitives `StreamBuffer`, `StreamBufferReader`, `StreamBufferWriter`, `BitStream`. |
| `Tags/` | Tag readers and models: `Id3v1Tag` / `Id3v1TagReader`, the `Id3v2*` family (readers, frame subclasses, helpers, extended header, language/country tables), `ApeTag` / `ApeItem` / `ApeTagReader`, `Lyrics3*`, `MusicMatch*`, `VorbisComments`, plus container-embedded tag models `Mp4MetaTag`, `AsfMetadataTag`, `MatroskaTag`. Also the plumbing interfaces `IAudioTag`, `IAudioTagReader`, `IAudioTagOffset`, `AudioTags` (the end-of-file / start-of-file scanner), and parse-event args. |
| `InvalidVersionException.cs` | Shared exception type used by versioned frame constructors. |

## Two parallel entry points

Callers typically use one or both of these, depending on what's in the file:

### `AudioTags.ReadStream(stream)`

Scans a stream for flat, position-based tags — ID3v2, ID3v1, APE,
Lyrics3v1/v2, MusicMatch. Returns an ordered list of
`IAudioTagOffset`s, each holding the tag instance plus its byte range
and origin (`Start` or `End` of stream). Malformed individual tags /
frames surface via the `AudioTagParseError` and `Id3v2FrameParseError`
events rather than throwing; parsing continues with the next reader.

### `AudioStreams.ReadStream(stream)`

Probes a stream for container formats — MPEG audio, FLAC, RIFF/WAV,
AIFF, OGG, DSF/DFF, MP4/M4A, ASF/WMA, Matroska/WebM. Each container
walker implements `IAudioStream` and exposes format-specific structure
(chunks, pages, boxes, objects, EBML elements) plus any embedded
metadata (e.g. `Mp4Stream.Tag`, `AsfStream.MetadataTag`,
`MatroskaStream.Tag`, `RiffStream.InfoTag`, `DsfStream.EmbeddedId3v2`).
Container-embedded tags do **not** flow through `AudioTags` — they live
on the walker instance.

## Writing

Every tag / chunk / box type has a `ToByteArray()` (or container-level
rewriter) that produces bytes matching the on-disk layout. Container
walkers like `Mp4Stream`, `AsfStream`, `MatroskaStream` rewrite the
enclosing file with a spliced-in tag payload, preserving the bytes
outside the edited region.

## Extending

- **New tag format at a flat byte position:** implement `IAudioTagReader`,
  register it via `AudioTags.AddReader<YourReader, YourTag>()`.
- **New container:** implement `IAudioStream` (see `RiffStream` /
  `OggStream` for compact references), register it in
  `AudioStreams._supportedStreams`.
- **New ID3v2 frame type:** subclass `Id3v2Frame`, override `Identifier`,
  `IsVersionSupported`, and the `Data` getter/setter, then register in
  `Id3v2FrameHelpers` if it should be discovered by default.

## Conventions

- File-scoped namespaces, `is not null`, expression-bodied members,
  `[]` collection expressions, primary constructors where natural.
- Public API gets XML docs; the SDK suppresses `CS1591` so they're not
  enforced per-file, but new public surface should be documented.
- Readers tolerate malformed input: return `null` / `false` or raise
  a parse-error event; they don't throw on bad bytes. They only throw
  on `ArgumentNullException` for contract violations.
- Length fields are bounds-checked against the enclosing container.
