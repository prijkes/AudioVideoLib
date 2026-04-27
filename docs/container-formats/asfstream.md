# AsfStream — ASF / WMA / WMV

Header Object walker. Descends the Header Object's children and
recurses into the Header Extension Object to pick up Metadata Object
and Metadata Library Object. Surfaces `Objects` (all top-level objects)
and `MetadataTag` (the aggregated `AsfMetadataTag`). Duration comes
from the File Properties Object's Play Duration (100-ns units).

## On-disk layout

ASF (Microsoft Advanced Systems Format) is built around GUID-prefixed
objects. Each object begins with a 16-byte GUID identifier followed
by an 8-byte little-endian size field that includes the header
itself, giving a minimum legal object size of 24 bytes. All multi-byte
integers are little-endian; GUIDs use ASF's mixed-endian on-disk
layout (three little-endian groups followed by one big-endian group),
which `Guid.ToByteArray()` happens to produce natively.

A file consists of three top-level objects in order: the Header
Object, the Data Object, and an optional Index Object. The Header
Object holds every metadata-bearing child — a Content Description
Object (CDO) with five fixed fields (Title, Author, Copyright,
Description, Rating), an Extended Content Description Object (ECDO)
of typed key/value pairs, and (inside the Header Extension Object)
the Metadata Object (MO) and Metadata Library Object (MLO) for
per-stream typed metadata. The Data Object carries the encoded media
frames; the Index Object accelerates seeking.

```text
[Header Object  ──► metadata]   [Data Object  ──► media]   [Index Object]
```

Access the parsed metadata union via `AsfStream.MetadataTag`;
enumerate every top-level object via `AsfStream.Objects` (each an
`AsfObject` carrying GUID, offsets, and payload extents).

For the metadata model itself (CDO / ECDO / MO / MLO and the
`AsfTypedValue` tagged union) see
[ASF / WMA](../tag-formats/asf.md).
