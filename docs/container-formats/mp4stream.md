# Mp4Stream — MP4 / M4A

Iterative box walker (depth-capped at 16 to avoid stack blow-up).
Handles 64-bit extended size (`size == 1`), "to end of file"
(`size == 0`), full-box version/flags headers on `meta`, and the
standard path `moov.udta.meta.ilst` for iTunes metadata. Surfaces:

- `Boxes` — top-level atoms.
- `Tag` (`Mp4MetaTag`) — strongly-typed ilst model.
- `TotalDuration` from `moov.mvhd` (v0 or v1 duration / timescale).

## On-disk layout

MP4 / M4A files follow the ISO/IEC 14496-12 base media file format
specification, organised around boxes (also called atoms) — self-
describing chunks of data with a 4-byte type, a 32-bit big-endian
size field (or 64-bit extended size when the 32-bit field is `1`),
and a payload. Boxes nest: a container box's payload is itself a
sequence of child boxes.

A typical file begins with an `ftyp` box declaring the major brand
and compatible brand list, followed by two primary structural
components: `moov` (the movie box) which holds all metadata, timing,
and per-track descriptors as a tree of nested boxes; and `mdat` (the
media data box) which holds the encoded audio / video samples. The
relative order matters — files with `moov` before `mdat` ("faststart"
layout) let players begin playback as soon as `moov` is read; files
with `moov` after `mdat` (the typical post-encode layout) require the
metadata tail to be reached before playback can start.

iTunes-style metadata lives at `moov.udta.meta.ilst`. The `meta` box
is a full-box (it has a 4-byte version+flags prefix before its
children); `ilst` contains one child atom per metadata item, each with
a 4-character identifier such as `©nam` (title) or `covr` (cover art).

Access the parsed iTunes tag via `Mp4Stream.Tag` (an `Mp4MetaTag`);
enumerate top-level boxes through `Mp4Stream.Boxes` to inspect raw
offsets and sizes.

For the ilst model itself (well-known atoms, free-form items, cover
art) see [MP4 / iTunes ilst](../tag-formats/mp4-itunes.md).
