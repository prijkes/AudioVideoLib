# RiffStream — RIFF / WAV / RIFX

Chunk walker. Supports little-endian `RIFF` and big-endian `RIFX`.
Only `WAVE` form type is currently recognised. Surfaces:

- `fmt` (channels, sample rate, bits/sample, byte rate, block align)
- `data` (offset and size of audio payload)
- `LIST INFO` (→ `InfoTag`)
- `id3 ` / `ID3 ` (→ `EmbeddedId3v2`)
- `bext` (→ `BextChunk`)
- `iXML` (→ `IxmlChunk`)
- All other chunks preserved in `Chunks` with start/end offsets and
  raw bytes.

For the WAV side-channel chunks themselves see
[WAV / AIFF chunks](../tag-formats/wav-aiff-chunks.md).
