# API reference

This section is generated from the XML doc comments in the source.

Top-level namespaces:

- `AudioVideoLib` — shared exception types (`InvalidVersionException`).
- `AudioVideoLib.Collections` — `NotifyingList<T>` and its event args, used by tag readers to raise per-item events.
- `AudioVideoLib.Cryptography` — `Crc8`, `Crc16` helpers used by MPEG frame checksums and ID3v2 extended-header CRC.
- `AudioVideoLib.Formats` — plain records for non-tag data: MPEG audio frames, FLAC metadata blocks, VBR headers (Xing / LAME / VBRI), RIFF / AIFF chunks, OGG pages, MP4 boxes, DSD chunks, EBML elements, BWF `bext`, iXML, `RiffInfoTag`, `AiffTextChunks`.
- `AudioVideoLib.IO` — stream walkers (`IMediaContainer` implementations), the `MediaContainers` registry, and the seekable buffer primitives `StreamBuffer` / `BitStream`.
- `AudioVideoLib.Tags` — tag readers and tag models (ID3v1, ID3v2, APE, Lyrics3, MusicMatch, Vorbis, MP4 / iTunes, ASF, Matroska), plus the `AudioTags` scanner and the shared interfaces `IAudioTag`, `IAudioTagReader`, `IAudioTagOffset`.
