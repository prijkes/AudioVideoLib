# AiffStream — AIFF / AIFF-C

Same shape as RIFF but big-endian. Surfaces `COMM` (channels, sample
frames, sample size, sample rate as IEEE 80-bit extended precision,
optional AIFC compression 4-CC), `SSND` (offset and size of audio),
and the text chunks `NAME` / `AUTH` / `ANNO` / `COMT` via
`TextChunks`.

For the AIFF text chunks themselves see
[WAV / AIFF chunks](../tag-formats/wav-aiff-chunks.md).
