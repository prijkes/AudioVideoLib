# Release notes

Change categories follow [Keep a Changelog](https://keepachangelog.com/):
**Added**, **Changed**, **Fixed**, **Breaking**.

| Version | Date | Highlights |
|---|---|---|
| [`0.5.0`](#050--2026-04-27) | 2026-04-27 | `WriteTo(Stream)` is now the canonical serialisation primitive; `ToByteArray` becomes an extension-method convenience. |
| [`0.4.0`](#040--2026-04-26) | 2026-04-26 | Streaming write API; magic-byte fast path; structured parse errors; async entry points; assorted API cleanup. |
| [`0.3.0`](#030--2026-04-25) | 2026-04-25 | `IAudioStream` → `IMediaContainer` rename. |
| [`0.2.0`](#020--2026-04-19) | 2026-04-19 | Nine new tag/container formats; non-fatal parse errors; per-field encoding for ID3v1. |
| [`0.1.0`](#010--2026-04-17) | 2026-04-17 | Initial release. |

---

## 0.5.0 — 2026-04-27

> The serialisation abstraction is flipped: `WriteTo(Stream)` is now
> the canonical primitive every implementer must provide, and
> `ToByteArray` / `GetSerializedSize` / `TryWriteTo` /
> `WriteTo(IBufferWriter<byte>)` are extension-method convenience
> wrappers. The streaming method is no longer a default-impl that
> secretly allocates a full byte array first.

### Breaking

- **`IAudioTag.ToByteArray()` is no longer a member** of the
  interface. The buffer-shaped helper now lives on
  `IAudioTagExtensions.ToByteArray(this IAudioTag)`. Call sites that
  do `tag.ToByteArray()` continue to work because extension-method
  binding picks up the static type. **Implementers** must now provide
  `void WriteTo(Stream destination)` instead of
  `byte[] ToByteArray()`. Concrete classes that want a faster direct
  buffer path may also expose their own `byte[] ToByteArray()`
  instance method, which shadows the extension method for
  concrete-typed call sites.
- **`IMediaContainer.ToByteArray()`** moves to
  `IMediaContainerExtensions.ToByteArray(this IMediaContainer)`. Same
  rules.

### Changed

- All bundled implementers (`Id3v1Tag`, `Id3v2Tag`, `ApeTag`,
  `Lyrics3Tag`, `Lyrics3v2Tag`, `MusicMatchTag`, every
  `IMediaContainer` walker) converted to override `WriteTo(Stream)`.
  The splice rewriters (`Mp4Stream`, `AsfStream`, `MatroskaStream`,
  `DsfStream`, `DffStream`) keep an instance-method
  `byte[] ToByteArray()` as the buffer fast path.

> **Migration:** the only call-site change is for *implementers* of
> `IAudioTag` / `IMediaContainer`. Rename your
> `byte[] ToByteArray()` to `void WriteTo(Stream destination)` and
> replace the final `return bytes;` with
> `destination.Write(bytes, 0, bytes.Length);`. Consumers that just
> *call* `tag.ToByteArray()` need no change — the extension method
> matches the existing call shape.

---

## 0.4.0 — 2026-04-26

> Performance, ergonomics, and API surface clean-up. Streaming write
> hooks across `IAudioTag` / `IMediaContainer`, magic-byte container
> dispatch, structured parse-error classification, async overloads at
> the public entry points, plus a sweep of small API gaps that bit
> real callers.

### Added

- **Streaming write surface** on `IAudioTag` and `IMediaContainer`:
  `WriteTo(Stream)`, `WriteTo(IBufferWriter<byte>)`,
  `TryWriteTo(Span<byte>, out int)`, `GetSerializedSize()`. Default
  implementations forward to `ToByteArray()` for back-compat.
- **`ReadOnlySpan<byte>` parse overloads** on `Mp4MetaTag.Parse`,
  `RiffInfoTag.Parse` / `FromListPayload`, `BwfBextChunk.Parse`,
  `IxmlChunk.Parse`, `AsfMetadataTag.ParseContentDescription` /
  `ParseExtendedContentDescription` / `ParseMetadata`,
  `AiffTextChunks.ReadText` / `ReadComments`. Each `byte[]` overload
  is a one-line wrapper around the new `ReadOnlySpan<byte>` overload.
- **Streaming `WriteTo(Stream)` overrides** on `MpaStream` (frame-by-frame),
  `FlacStream` (magic + blocks + frames), and `Mp4Stream` (chunked head +
  new ilst + tail). Halves peak memory on edits to large files.
- **Magic-byte fast path** in `MediaContainers.ReadStreams`. Files with
  recognisable signatures (FLAC / OGG / WAV / RIFX / AIFF / DSF / DFF /
  Matroska / ASF / MP4) dispatch to the matching walker in O(1)
  instead of probing every walker at every byte position.
- **Strict scan mode** — `AudioTags.MaxTagSpacingLength` and
  `MediaContainers.MaxStreamSpacingLength` accept `0` (a new `Strict`
  constant) to skip the byte-by-byte rescan and only check the
  canonical anchor positions. Useful for clean / pipeline-controlled
  inputs where the rescan is pure overhead.
- **Structured parse errors** — `AudioTagParseErrorEventArgs.Kind` and
  `Id3v2FrameParseErrorEventArgs.Kind` (both typed as
  `AudioTagParseErrorKind`) classify the failure as
  `Truncated` / `MalformedData` / `UnsupportedVersion` /
  `InvalidArgument` / `Unknown`, so callers can dispatch without
  parsing the exception message.
- **`AudioTags.RemoveTag` / `RemoveTags<T>` / `Clear`** — closing the
  obvious gap next to the existing `AddTag`.
- **`AudioInfo.Save(Stream)`** for streaming output.
- **Async entry points** — `AudioInfo.AnalyseAsync`, `SaveAsync`,
  `AudioTags.ReadStreamAsync`, `MediaContainers.ReadStreamAsync`.
  Currently `Task.Run` wrappers around the sync code; honestly
  documented as not-yet-truly-async pending a reader-chain refactor.
- **`Strict` and `Default` named constants** on the spacing-length
  knobs for readability.

### Changed

- **`Mp4Stream.Tag` / `MatroskaStream.Tag` / `AsfStream.MetadataTag`**
  are now publicly settable. `null` is treated as "clear all metadata"
  — assigning resets to a fresh empty model. Previously these were
  `private set;`, forcing callers to mutate in place.
- **`IAudioTag` / `IMediaContainer.GetEnumerator`** typed
  `IEnumerator<T>` is now public; the non-generic version is the
  explicit interface implementation. `foreach (var x in tags)` now
  binds `x` to the typed element instead of `object`.
- **`AudioInfo.Save(string, bool overwrite = true)`** — overwrite is
  now the default. The previous behaviour of throwing on existing files
  is reachable via `overwrite: false`, which yields an `IOException`
  rather than the previous `ApplicationException`.
- **Throw type for "file already exists"** changed from
  `ApplicationException` to `IOException`.

### Breaking

- **`IAudioTagWriter` interface removed.** It was an empty marker;
  serialisation lives on `IAudioTag.ToByteArray()` / the new
  `WriteTo(Stream)` overloads. Any code referencing
  `IAudioTagWriter` should drop the reference; no concrete behaviour
  changes.

> **Migration:** the new `WriteTo` / `TryWriteTo` / `GetSerializedSize`
> members on `IAudioTag` and `IMediaContainer` are default-implemented,
> so existing implementers stay source-compatible.

---

## 0.3.0 — 2026-04-25

> Naming-only release. The `IAudioStream` family is renamed to
> `IMediaContainer` to reflect that those types describe a *media
> container walker* — MP4 / Matroska / ASF carry video and metadata
> alongside audio, and the audio-centric name was misleading.

### Breaking

| Before (`0.2.0`) | After (`0.3.0`) |
|---|---|
| `IAudioStream` | `IMediaContainer` |
| `AudioStreams` (collection / factory) | `MediaContainers` |
| `AudioStreamParseEventArgs` | `MediaContainerParseEventArgs` |
| `AudioStreamParsedEventArgs` | `MediaContainerParsedEventArgs` |
| `AudioStreams.AudioStreamParse` (event) | `MediaContainers.MediaContainerParse` |
| `AudioStreams.AudioStreamParsed` (event) | `MediaContainers.MediaContainerParsed` |
| `IAudioStream.TotalAudioLength` (property) | `IMediaContainer.TotalDuration` |
| `IAudioStream.TotalAudioSize` (property) | `IMediaContainer.TotalMediaSize` |

The tag APIs — `AudioTags`, `IAudioTag`, `IAudioTagReader`,
`IAudioTagOffset`, `AudioTagOffset`, and their event args — are
**unchanged**. Tags genuinely are audio-format concepts (ID3, APE,
Lyrics3, MusicMatch), so the "audio" prefix stays accurate there.

> **Migration:** one `sed` pass handles most projects.
>
> ```bash
> sed -i -E '
>   s/\bIAudioStream\b/IMediaContainer/g
>   s/\bAudioStreams\b/MediaContainers/g
>   s/\bTotalAudioLength\b/TotalDuration/g
>   s/\bTotalAudioSize\b/TotalMediaSize/g
>   s/AudioStreamParseEventArgs/MediaContainerParseEventArgs/g
>   s/AudioStreamParsedEventArgs/MediaContainerParsedEventArgs/g
> ' **/*.cs
> ```

---

## 0.2.0 — 2026-04-19

> Nine additional metadata formats round-trip end-to-end. Parse failures
> are now non-fatal — a single malformed frame no longer aborts the
> file. ID3v1 finally handles real-world per-field encodings.

### Added — new tag / container formats

Round-trip read + write support for:

- **WAV `LIST INFO`** — standard key/value metadata (`INAM`, `IART`,
  `IPRD`, `ICRD`, `ICMT`, `IGNR`, `ITRK`, `IENG`, `ISFT`, `ICOP`, …).
- **ID3v2-in-WAV** — full ID3v2 tag inside a WAV `id3 ` / `ID3 ` chunk.
- **BWF `bext` chunk** — broadcast Wave v0/v1/v2 with loudness fields
  and CodingHistory.
- **iXML chunk** — film/audio production metadata with BOM-preserving
  byte-identical round-trip.
- **AIFF text chunks** — NAME / AUTH / ANNO / COMT (timestamped).
- **DSF / DFF** — Sony / Philips DSD audio containers with embedded
  ID3v2.
- **MP4 / M4A iTunes ilst** — strongly-typed ilst items (text,
  `trkn`/`disk`, `tmpo`, `cpil`, `covr` JPEG/PNG, free-form `----`),
  with a splice-in writer that patches enclosing parent sizes.
- **ASF / WMA** — Content Description + Extended Content Description +
  Metadata + Metadata Library objects; `AsfTypedValue` tagged union.
- **Matroska / WebM** — `Tags` / `Tag` / `SimpleTag` (with nesting,
  `TagBinary`, `TargetTypeValue`), Duration extraction, splice-rewrite
  writer.

### Added — ID3v1 per-field encoding

Each ID3v1 field is now stored as raw `byte[]` with a per-field
`Encoding` override. The reader auto-detects UTF-8 per field. The
Studio's ID3v1 editor gets a per-field encoding picker for Title /
Artist / Album / Comment.

### Changed — non-fatal parse errors

A single malformed frame or item no longer aborts the whole file.
`AudioTags.AudioTagParseError` and `AudioTags.Id3v2FrameParseError`
surface the failure; the scanner continues.

### Fixed

- **APE items were parsed but never attached to the tag** —
  `ApeTag.Items` was always empty because `ApeTagReader` built the list
  locally and never called `tag.SetItems(items)`.
- **Multiple APE tags in one file** — now get a tab each in the Studio.
- **ID3v2 ISO 639-2/T language codes** (`nld`, `deu`, `fra`, `ces`,
  `zho`, …) accepted alongside the /B variants.
- **URL-link frame (`WOAF` etc.) `Data` getter** no longer throws
  `ArgumentNullException` on a just-added frame with unset `Url`.
- **OGG page CRC** recomputed on write.

### Tests

802 → 1064 passing (+262), covering each new format's read and
round-trip paths plus the per-field encoding auto-detection.

---

## 0.1.0 — 2026-04-17

Initial release.

- **Containers:** MPEG audio, FLAC, RIFF/WAV, AIFF, OGG.
- **Tags:** ID3v1, ID3v2 (2.2–2.4), APE (v1/v2), Lyrics3 (v1/v2),
  MusicMatch, Vorbis Comments.
