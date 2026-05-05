# Release notes

Change categories follow [Keep a Changelog](https://keepachangelog.com/):
**Added**, **Changed**, **Fixed**, **Breaking**.

| Version | Date | Highlights |
|---|---|---|
| [`(next release)`](#next-release) | _unreleased_ | Format pack: Musepack / WavPack / TrueAudio / Monkey's Audio walkers; `IMediaContainer` now extends `IDisposable`; `FlacStream` / `MpaStream` switch to byte-passthrough on save. |
| [`0.7.0`](#070--2026-04-27) | 2026-04-27 | `Mp4Stream` and `AsfStream` complete the streaming refactor — every splice rewriter now operates without buffering the source file. |
| [`0.6.0`](#060--2026-04-27) | 2026-04-27 | Large-file performance: streaming Matroska reader/writer (40 GB MKV is now viable); `ISourceReader` random-access abstraction; ID3v2 read-path allocation fixes. |
| [`0.5.0`](#050--2026-04-27) | 2026-04-27 | `WriteTo(Stream)` is now the canonical serialisation primitive; `ToByteArray` becomes an extension-method convenience. |
| [`0.4.0`](#040--2026-04-26) | 2026-04-26 | Streaming write API; magic-byte fast path; structured parse errors; async entry points; assorted API cleanup. |
| [`0.3.0`](#030--2026-04-25) | 2026-04-25 | `IAudioStream` → `IMediaContainer` rename. |
| [`0.2.0`](#020--2026-04-19) | 2026-04-19 | Nine new tag/container formats; non-fatal parse errors; per-field encoding for ID3v1. |
| [`0.1.0`](#010--2026-04-17) | 2026-04-17 | Initial release. |

---

## (next release)

### Added

- `MpcStream` — Musepack walker (SV7 + SV8).
- `WavPackStream` — WavPack walker (`.wv` + optional `.wvc` correction stream in same file).
- `TtaStream` — TrueAudio walker.
- `MacStream` — Monkey's Audio walker (integer + float). Note: separate from the existing `ApeTag` family (which handles APE *tags*, not the Monkey's Audio audio container).

### Changed

- `IMediaContainer` now extends `IDisposable`. Walkers that hold an `ISourceReader` throw `InvalidOperationException` from `WriteTo` if the source has been disposed or was never populated. See the "source-stream lifetime contract" docstring on `IMediaContainer`.
- `FlacStream` and `MpaStream` now use offset-based byte-passthrough on save: per-frame audio bytes are spliced from the source stream rather than re-emitted from the parsed model. Audio is byte-identical on round-trip; no audio re-encoding occurs anywhere in the library.

### Breaking

- All `FlacFrame`, `FlacSubFrame`, `FlacResidual`, `FlacRicePartition`, `MpaFrame`, `MpaFrameHeader`, `MpaFrameData` properties are now read-only. Callers cannot mutate audio-frame data. (Tag editing — Vorbis comments, ID3 frames, etc. — remains fully supported via the metadata-block path.)
- `IMediaContainer` consumers must dispose the walker (or the parent `MediaContainers`) when done. Wrap usage in `using`.

### Fixed

#### FLAC parser revival

- **CRC-16 polynomial wrong** — was `0xA001` (reflected/CRC-16-IBM-ARC), now `0x8005` MSB-first per RFC 9639 §11.1.
- **`Crc16.Calculate([])`** at frame-CRC validation site replaced with the actual frame byte slice (was always computing CRC over an empty span).
- **Frame sync mask** — was 15-bit `0x7FFE`, now 14-bit `0x3FFE`. Rejects illegal `0x3FFF` and EOF sentinel `0xFFFFFFFF`.
- **Frame-header reserved bits** — bits 17 and 0 are now validated to be 0 per RFC 9639 §11.21.
- **CRC-8 strict-rejection** — `FlacFrameHeader` returns false on CRC-8 mismatch instead of throwing `InvalidDataException`, matching the strict-rejection rule and the CRC-16 fix.
- **Subframe payload `Read`** — uncommented; subframe contents are now consumed.
- **Subframe-type extraction** — was reading byte 3 of a 32-bit BE peek, now reads byte 0 with mask `0x3F`.
- **Subframe payload bit-stream migration** — Fixed/LPC/Verbatim/Constant subframe payloads, residual, and Rice partitions now read via `BitStream` (bit-packed) instead of byte-aligned `StreamBuffer`. Resolves position-drift that caused frame CRCs to mismatch.
- **`FlacResidual`** — coding method and partition order bit positions corrected per RFC 9639 §11.30. Reserved coding methods (2, 3) now rejected.
- **`FlacRicePartition`** — PartitionedRice (4-bit) and PartitionedRice2 (5-bit) Rice parameter widths un-swapped.
- **`FlacLinearPredictorSubFrame`** — reserved precision value `0b1111` now rejected per RFC 9639 §11.29.
- **`BitStream`** — `ReadSignedInt32(32)` no longer throws `OverflowException`; `ReadUnaryInt` has a defensive bound against malformed input.
- **`FlacStreamInfoMetadataBlock.Channels`** — mask was 5-bit `0x1F`, now 3-bit `0x07` per RFC 9639 §8.2.
- **`FlacMetadataBlock.ReadBlock`** — length check uses `stream.Position + length`, not just `length`.
- **`FlacCueSheetMetadataBlock`** — reserved padding 256 → 258 bytes; TrackType/PreEmphasis flag bits at MSB (bits 7/6), not LSB; writer's `&` typo for flag combine corrected to `|`.
- **`VorbisComments.ToByteArray`** — removed redundant outer length prefix; each `VorbisComment` already self-prefixes per the Vorbis comment spec.

#### New walker bugs

- **`MpcStream` SV8 `EncoderVersion` byte-shift** — major version is now emitted in bits 31-24 (was 23-16) per the reference decoder `streaminfo.c:228-230`. Decoded encoder versions are no longer scaled by 1/256 of the intended value.
- **`WavPackSubBlock.UniqueId` mask widened from 5 bits to 6** — IDs ≥ `0x20` such as `ID_RIFF_HEADER` no longer collide with low IDs after the mask, so RIFF / channel-info / Wavpack-extra sub-blocks are now identified correctly.
- **`MacSeekEntry.FileOffset` widened to `long` with C++-style wrap correction** — APE files larger than 4 GiB no longer have garbled frame offsets when the 32-bit seek-table entries wrap.
- **`VorbisComments.ToByteArray`** — no longer writes a redundant outer length prefix (cross-listed under FLAC parser revival above; the same fix benefits standalone OGG-Vorbis comments and FLAC-embedded VORBIS_COMMENT blocks).

---

## 0.7.0 — 2026-04-27

> Mechanical mirror of the 0.6.0 Matroska refactor applied to the
> remaining splice rewriters. `Mp4Stream` and `AsfStream` now keep an
> `ISourceReader` reference instead of buffering the source file, and
> `WriteTo(Stream)` streams unchanged regions directly from source to
> destination. All five splice rewriters are now large-file-friendly.

### Changed

- **`Mp4Stream` no longer buffers the input.** The walker reads
  structurally from the live stream during `ReadStream` (mdat is
  seeked past, never copied), and `WriteTo(Stream)` streams head + new
  ilst + tail directly. The patches to ancestor box headers
  (`moov` / `udta` / `meta`) are emitted segmentally, so even
  non-faststart files (mdat first, moov last) write in bounded
  memory regardless of file size. Internal offset fields widened from
  `int` to `long` for proper 64-bit file support.
- **`AsfStream` no longer buffers the entire file.** Only the small
  Header Object is materialised in memory; the multi-GB Data Object +
  Index Object that follows is streamed straight through to the
  destination at write time. ASF parse helpers consume
  `ReadOnlySpan<byte>` directly.

### Breaking

- **`Mp4Stream` and `AsfStream` lifetime contract:** the source
  `Stream` passed to `ReadStream` must stay alive until `WriteTo` /
  `ToByteArray` finishes — same change `MatroskaStream` got in 0.6.0.
  Most callers wrap in a `using` block that already covers the full
  lifetime.
- **Both classes now implement `IDisposable`** to release their
  `ISourceReader`. The user's source stream is **not** closed; it
  remains caller-owned.

> The remaining DSF and DFF walkers were already selective (they
> capture chunk metadata at parse time, not the whole file) and don't
> need the refactor.

---

## 0.6.0 — 2026-04-27

> Large-file performance round. `MatroskaStream` no longer copies the
> entire input file into memory — a 40 GB MKV that previously peaked
> around **120 GB RAM** during a tag edit now stays in the **low MB**
> range, with the unchanged regions streamed directly from source to
> destination. Plus a sweep of allocation hotspots in the read path.

### Added

- **`ISourceReader`** — random-access read facade with two
  implementations: `MemorySourceReader` (wraps a `byte[]`) and
  `StreamSourceReader` (wraps a seekable `Stream`). Splice-rewriter
  walkers use this to copy unchanged byte ranges from source to
  destination at write time without materialising the full file.

### Changed

- **`MatroskaStream` no longer buffers the source file.**
  `ReadStream` now wraps the supplied stream in a
  `StreamSourceReader` and walks the structure on the live stream
  (Cluster / BlockGroup payloads are seeked past, never copied).
  `WriteTo(Stream)` streams head + new Tags + tail directly. Peak
  memory drops from O(file size) to O(metadata) on edits.
- **ID3v2 CRC validation** (`Id3v2TagReader`) no longer materialises
  the whole tag-bearing stream three times via
  `ToByteArray().Skip().Take().ToArray()`. The CRC is now hashed
  incrementally over a 4 KB stack-allocated chunk.
- **`StreamBufferReader.ReadString`** uses `ArrayPool<byte>` and span
  slicing for BOM stripping — no extra buffer allocation per call.
  Hot for ID3v2 frame parsing where dozens of short reads happen per
  frame.
- **`Id3v2FrameReader.ReadIdentifier`** stack-allocates the 4-byte
  identifier buffer instead of `new byte[4]` per frame.

### Breaking

- **`MatroskaStream` lifetime contract:** the source `Stream` passed
  to `ReadStream` must stay alive until `WriteTo` / `ToByteArray`
  finishes — the walker reads from it on demand at write time. The
  previous behaviour of capturing all bytes up-front is gone (it's
  what made multi-GB files unworkable). Most callers use
  `using var fs = File.OpenRead(...); var info = AudioInfo.Analyse(fs); info.Save(...)`
  inside the `using` already, which works as expected.
- **`MatroskaStream` now implements `IDisposable`** — its
  `ISourceReader` is disposed on `Dispose`. The user's source stream
  is **not** closed; that remains caller-owned.

### Queued for follow-up (not in this release)

- **`Mp4Stream`** still buffers the full input. Same refactor pattern
  as MatroskaStream applies; deferred to a separate release for
  reviewability.
- **`AsfStream`** likewise. Recursive Header-Object walker makes the
  refactor more involved.

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
