# AsfStream — ASF / WMA / WMV

Header Object walker. Descends the Header Object's children and
recurses into the Header Extension Object to pick up Metadata Object
and Metadata Library Object. Surfaces `Objects` (all top-level objects)
and `MetadataTag` (the aggregated `AsfMetadataTag`). Duration comes
from the File Properties Object's Play Duration (100-ns units).

For the metadata model itself (CDO / ECDO / MO / MLO and the
`AsfTypedValue` tagged union) see
[ASF / WMA](../tag-formats/asf.md).
