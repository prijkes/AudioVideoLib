# AiffStream — AIFF / AIFF-C

Same shape as RIFF but big-endian. Surfaces `COMM` (channels, sample
frames, sample size, sample rate as IEEE 80-bit extended precision,
optional AIFC compression 4-CC), `SSND` (offset and size of audio),
and the text chunks `NAME` / `AUTH` / `ANNO` / `COMT` via
`TextChunks`.

## On-disk layout

AIFF and AIFF-C (Apple, 1988 / 1991) are built around the FORM
container — a big-endian variant of the same IFF chunk structure RIFF
later adopted. The file begins with the magic `FORM`, followed by a
32-bit big-endian size field and the form-type identifier (`AIFF` or
`AIFC`), then a sequence of chunks identified by 4-character ASCII
codes.

The `COMM` (common) chunk is mandatory and carries the essential
format parameters: channel count, number of sample frames, sample
size in bits, and sample rate encoded as an IEEE 754 80-bit
extended-precision value. AIFF-C extends `COMM` with a 4-character
compression identifier and an optional human-readable name. The
`SSND` (sound data) chunk holds the raw audio samples, prefixed by
offset and block-align fields.

Optional text chunks supply track-level metadata: `NAME`, `AUTH`, and
`ANNO` are flat ASCII strings; `COMT` carries timestamped comments
keyed by an optional marker id. All chunk payloads are padded to even
byte length, with the size field reporting the unpadded count.

`AiffStream.Chunks` lists every chunk in file order with start and
end offsets; the parsed `COMM` fields surface as `Channels`,
`SampleFrames`, `SampleSize`, `SampleRate`, and `Compression`; `SSND`
location and extent through `SsndOffset` / `SsndSize`; and the text
metadata via `AiffStream.TextChunks`.

For the AIFF text chunks themselves see
[WAV / AIFF chunks](../tag-formats/wav-aiff-chunks.md).
