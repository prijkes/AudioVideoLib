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

## On-disk layout

RIFF (Resource Interchange File Format, IBM/Microsoft 1991) is the
structural foundation of WAV, built around variable-length typed
chunks preceded by a 4-byte ID and 4-byte size. The library supports
the little-endian RIFF format and its big-endian variant RIFX; the
initial magic and a 4-byte declared size sit at the container's head,
followed immediately by the WAVE form-type identifier.

The container itself is a sequence of chunks, each with a
4-character ID and a 32-bit size field in the file's native
endianness. The walker captures the `fmt ` chunk to expose format
metadata (channels, sample rate, bits per sample, byte rate, block
align) and the `data` chunk to record the audio payload's absolute
offset and length. All other chunks — whether `LIST INFO`, `id3 `,
`bext`, `iXML`, or arbitrary application-defined types — are
preserved structurally in file order with their start and end byte
offsets and (for recognised metadata types) their parsed payloads.
Chunks are word-aligned: odd-sized chunks are padded to an even byte
boundary, but the size field reports the unpadded length.

Access discovered chunks via `RiffStream.Chunks`; for recognised
metadata, use the dedicated properties `RiffStream.InfoTag`,
`RiffStream.EmbeddedId3v2`, `RiffStream.BextChunk`, and
`RiffStream.IxmlChunk`.

For the WAV side-channel chunks themselves see
[WAV / AIFF chunks](../tag-formats/wav-aiff-chunks.md).
