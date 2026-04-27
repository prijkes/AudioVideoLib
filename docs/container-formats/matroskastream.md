# MatroskaStream — MKV / MKA / WebM

EBML walker for the EBML Header + first Segment. Parses
`Segment.Info` for `TimecodeScale` + `Duration` and `Segment.Tags` for
the tag tree. Skips `Cluster` and `BlockGroup`. Surfaces `DocType`
(`"matroska"` or `"webm"`) and `Tag` (`MatroskaTag` holding the tag
entries).

## On-disk layout

Matroska is built on EBML — a binary container format where every
piece of data is a self-describing element of the form *id + size +
payload*. Both the id and the size are encoded as variable-length
integers (VINTs): the highest bits of the first byte signal the total
length (1–8 bytes), with the remaining bits forming the value. That
encoding lets the same wire format describe both small and very
large elements without a fixed width, and the splice writer relies on
it: by re-emitting the segment size with the *same* VINT byte length,
internal segment-relative offsets in `SeekHead` and `Cues` stay
valid.

A file opens with the EBML root element (id `0x1A45DFA3`) carrying
the format declaration — most importantly `DocType` (`"matroska"` or
`"webm"`). The remainder is a single Segment (id `0x18538067`) whose
master children include `Info` (with `TimecodeScale` and `Duration`),
`Tracks`, `Cues`, `Cluster` (the bulk of the file — the encoded
frames), `Tags`, `Attachments`, and `Chapters`. Cluster and
BlockGroup payloads are skipped at parse time so a 40 GB MKV walks
in O(metadata) bytes touched.

```text
EBML │ Segment ┌── Info (TimecodeScale, Duration)
                ├── Tracks
                ├── Cluster ... Cluster (skipped)
                ├── Cues
                ├── Tags  ◄── metadata of interest
                └── Attachments / Chapters
```

Access the parsed `Segment.Tags` via `MatroskaStream.Tag` (a
`MatroskaTag` holding one entry per top-level `Tag` element).

For the tag tree itself (`Targets`, `SimpleTag` nesting, track / album
levels, write-back) see
[Matroska / WebM](../tag-formats/matroska.md).
