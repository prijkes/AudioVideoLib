# MatroskaStream — MKV / MKA / WebM

EBML walker for the EBML Header + first Segment. Parses
`Segment.Info` for `TimecodeScale` + `Duration` and `Segment.Tags` for
the tag tree. Skips `Cluster` and `BlockGroup`. Surfaces `DocType`
(`"matroska"` or `"webm"`) and `Tag` (`MatroskaTag` holding the tag
entries).

For the tag tree itself (`Targets`, `SimpleTag` nesting, track / album
levels, write-back) see
[Matroska / WebM](../tag-formats/matroska.md).
