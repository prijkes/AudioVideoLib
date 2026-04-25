# Tag formats

AudioVideoLib reads and writes a wide range of audio metadata formats.
Pick a format below for its on-disk shape, the public API surface, the
quirks worth knowing, and the round-trip behaviour.

| Format | Where it lives | Surfaced on |
|---|---|---|
| [ID3v1](tag-formats/id3v1.md) | last 128 bytes of the file (+ optional 227-byte TAG+) | `AudioTags` → `Id3v1Tag` |
| [ID3v2](tag-formats/id3v2.md) | start (or end, 2.4) — frame-based, versions 2.2 / 2.3 / 2.4 | `AudioTags` → `Id3v2Tag` |
| [APE](tag-formats/ape.md) | end (v1 / v2) or start (v2 only) | `AudioTags` → `ApeTag` |
| [Lyrics3](tag-formats/lyrics3.md) | end-of-file wrapper, before any ID3v1 | `AudioTags` → `Lyrics3Tag` / `Lyrics3v2Tag` |
| [MusicMatch](tag-formats/musicmatch.md) | trailing 8 KB+ block | `AudioTags` → `MusicMatchTag` |
| [Vorbis Comments](tag-formats/vorbis-comments.md) | inside FLAC's `VORBIS_COMMENT` block / OGG vorbis-comment packet | `FlacStream.VorbisCommentsMetadataBlock` |
| [MP4 / iTunes ilst](tag-formats/mp4-itunes.md) | `moov.udta.meta.ilst` inside MP4 / M4A | `Mp4Stream.Tag` |
| [ASF / WMA](tag-formats/asf.md) | ASF Header Object (CDO + ECDO + MO/MLO) | `AsfStream.MetadataTag` |
| [Matroska / WebM](tag-formats/matroska.md) | `Segment.Tags` inside Matroska / WebM | `MatroskaStream.Tag` |
| [WAV / AIFF chunks](tag-formats/wav-aiff-chunks.md) | `LIST INFO`, `bext`, `iXML`, `id3 `, AIFF text chunks | `RiffStream.*`, `AiffStream.TextChunks` |
| [DSF / DFF](tag-formats/dsf-dff.md) | embedded ID3v2 inside DSD audio containers | `DsfStream.EmbeddedId3v2`, `DffStream.EmbeddedId3v2` |

For the difference between flat-position tags (handled by `AudioTags`)
and container-embedded tags (surfaced on a `MediaContainers` walker),
see [Getting started](getting-started.md#container-embedded-tags). For
what `ToByteArray()` guarantees, see
[Round-trip semantics](round-trip.md).
