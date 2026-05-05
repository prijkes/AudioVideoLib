# Round-trip semantics

What does "write the tag back" actually guarantee? This page spells out
the guarantees and limitations per format.

## General rule

All container walkers in this library use byte-passthrough save: audio
frames are spliced verbatim from the source stream to the destination,
not re-emitted from the parsed model. There is no audio re-encoding
anywhere in the library. Tag edits flow through `AudioTags` (for flat,
position-based tags like ID3 / APE / Lyrics3 / MusicMatch) or through
the per-walker metadata model (for container-embedded tags like MP4
ilst, ASF Content Description, Matroska Tags, FLAC Vorbis Comments),
and the surrounding audio bytes are byte-identical.

For a tag that is read and immediately written back with no changes,
the bytes are byte-identical **unless a section below says otherwise**.
When fields are edited, the library re-encodes only the changed field
and rewrites the enclosing container's size / count / offset fields.

Implementers override `void WriteTo(Stream destination)` — the
canonical serialisation primitive on `IAudioTag` / `IMediaContainer`.
Callers can use the consumer-facing `obj.ToByteArray()` extension
method when they want the result as a `byte[]`.

## ID3v1

- **Read then write, no edits:** byte-identical — raw per-field bytes are
  the source of truth.
- **Field edited:** just that field is re-encoded via its effective
  per-field `Encoding`. Others are written verbatim.
- **Gotcha:** the v1 spec is silent on encoding. If you read a UTF-8
  tag (auto-detected), edit the title, and save, the title is
  re-encoded as UTF-8 — which is what the file already used, so this is
  what you want. If you *want* to convert to Latin-1, set
  `TrackTitleEncoding = Encoding.Latin1` before assigning.

## ID3v2

- **Read then write, no edits:** byte-identical for frames the library
  fully round-trips. Some exotic frames may only preserve their raw
  bytes rather than the structured decomposition.
- **Extended header CRC:** recomputed on every write if the flag is set.
- **Unsynchronization:** re-applied on write when the flag is set.
- **Footer:** preserved if `UseFooter` is true (2.4 only).

```csharp
// Read-then-write with no edits is byte-identical to the input bytes within
// the tag's [StartOffset .. EndOffset] window.
using var fs = File.OpenRead("track.mp3");
var tags = AudioTags.ReadStream(fs);
var offset = tags.First(t => t.AudioTag is Id3v2Tag);
var id3v2 = (Id3v2Tag)offset.AudioTag;
var rewritten = id3v2.ToByteArray();

// Now edit one frame and re-emit; only the affected frame and the tag
// header's size field change.
var artist = id3v2.Artist ?? new Id3v2TextFrame(id3v2.Version, "TPE1");
artist.Values.Clear();
artist.Values.Add("Different artist");
id3v2.Artist = artist;
File.WriteAllBytes("tag.id3v2", id3v2.ToByteArray());
```

## APE

- Byte-identical round-trip for read-then-write-no-edits.
- Item count + tag size fields in header and footer are recomputed on
  every write.

## Lyrics3

v1 and v2 round-trip faithfully. The v2 size suffix (6 ASCII digits
before the `LYRICS200` footer) is recomputed on write.

## MusicMatch

The library supports both read and write — `MusicMatchTag` round-trips
faithfully, and `info.Save(path)` persists edits. The Studio's GUI
editor for MusicMatch fields hasn't shipped yet; callers must edit via
the model directly.

## Vorbis Comments

- **Standalone (OGG):** edits recompute the page CRC when the Studio
  writes the OGG back.
- **Inside FLAC:** edits recompute the metadata block length; the
  `is-last` flag on the now-last block is adjusted if the edit changed
  the block count.

## MP4 / iTunes

- **ilst splice:** `Mp4Stream.ToByteArray()` reads the original bytes
  into memory, locates the existing ilst (or walks `moov → udta → meta`
  creating the chain if absent), and splices in the serialized new
  ilst, patching the enclosing atom sizes (`ilst`, `meta`, `udta`,
  `moov`) in-place.

```csharp
// Splice an MP4 metadata change without touching the audio sample tables.
using var streams = MediaContainers.ReadStream(fs);
var mp4 = streams.OfType<Mp4Stream>().Single();
mp4.Tag.Title = "Edited";
File.WriteAllBytes("out.m4a", mp4.ToByteArray());  // entire file, with ilst rewritten
```
- **`stco` / `co64` audio offsets:** when `moov` is after `mdat` (the
  common QuickTime layout), shrinking or growing the metadata doesn't
  move the audio, so these tables stay valid. When `moov` is before
  `mdat` (Apple's streaming-friendly "faststart" layout), changing
  metadata size *would* invalidate them — the library does not
  currently rewrite those tables, so for faststart files you should
  either not change metadata size (the library will try to match the
  old size where possible by padding with a `free` atom) or expect
  playback offsets to break.
- **Atom size encoding:** the walker handles `size == 0` (to EOF),
  `size == 1` (64-bit extended size on the next 8 bytes), and the
  normal 32-bit size. On write, the library uses the minimal encoding
  that fits.

## ASF / WMA

- **Splice rewriter:** original bytes are captured up front. Replaced
  objects (Content Description Object, Extended Content Description
  Object — matched by GUID) are swapped for freshly serialized
  payloads. All other objects (File Properties, Header Extension, Data,
  Index) are preserved verbatim.
- **Header Object size + child count** are recomputed.
- **Metadata Object / Metadata Library Object** currently round-trip
  through synthesised files but a real file's existing Header
  Extension is preserved as-is (not re-emitted from the model). That's
  a follow-up item.

## Matroska / WebM

- **Splice rewriter:** reads the original bytes, locates the existing
  `Tags` element inside the first Segment, splices in new bytes, and
  patches the Segment's size VINT.
- **Segment size VINT length is preserved**, which keeps Cluster /
  Cues offsets valid. If the new Tags element is larger than the
  original plus any following Void element can accommodate, the rest
  of the segment would shift — in that case we fall back to appending
  a fresh Tags element before the end of the segment.
- **SeekHead** entries that point at the old Tags offset are not
  currently updated. Players that rely on SeekHead-only indexing may
  miss the new tags; players that walk the segment linearly will find
  them. Follow-up item.

## WAV / AIFF container chunks

- **LIST INFO, BWF `bext`, iXML, AIFF text chunks:** round-trip
  faithfully. `bext` v0/v1/v2 all preserved.
- **iXML:** `ToByteArray()` returns the raw captured payload (BOM
  preserved), giving byte-identical round-trip for unmodified iXML.
  A `ToByteArray(includeBom)` overload re-serializes from the parsed
  XML string when callers want canonical output.
- **WAV with embedded `id3 ` chunk:** the chunk's payload is a
  complete ID3v2 tag — round-trip is byte-identical if you don't
  touch the ID3v2, and re-encodes the ID3v2 payload if you do.

## DSF / DFF

- `DsfStream.ToByteArray()` regenerates the DSD header so the file
  size + metadata pointer match current state. Audio payload bytes
  are captured at read time and written back verbatim.
- `DffStream.ToByteArray()` re-emits the FRM8 form with every captured
  sub-chunk.

## What never round-trips byte-identically

- Tags where a CRC is only present on write (e.g. ID3v2 extended
  header CRC) will match on repeated writes but may differ from a
  file produced by a different writer that used a different CRC seed.
  The values are semantically equal but not always bitwise-equal to
  arbitrary third-party output.
- OGG page CRCs are recomputed on every write — same caveat.
