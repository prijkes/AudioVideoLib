# Mp4Stream — MP4 / M4A

Iterative box walker (depth-capped at 16 to avoid stack blow-up).
Handles 64-bit extended size (`size == 1`), "to end of file"
(`size == 0`), full-box version/flags headers on `meta`, and the
standard path `moov.udta.meta.ilst` for iTunes metadata. Surfaces:

- `Boxes` — top-level atoms.
- `Tag` (`Mp4MetaTag`) — strongly-typed ilst model.
- `TotalDuration` from `moov.mvhd` (v0 or v1 duration / timescale).

For the ilst model itself (well-known atoms, free-form items, cover
art) see [MP4 / iTunes ilst](../tag-formats/mp4-itunes.md).
