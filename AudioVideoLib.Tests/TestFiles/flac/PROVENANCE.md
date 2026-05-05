# FLAC TestFiles provenance

| File | Size | Encoder | Source | Notes |
|---|---|---|---|---|
| `sample.flac` | 11450 B | ffmpeg 7.1.1 (libavcodec built-in `flac` encoder, `-compression_level 5`) | self-generated 0.25 s 440 Hz sine via `lavfi`, upmixed to stereo (`-ac 2`), 44.1 kHz, 16-bit (`-sample_fmt s16`) | Magic verified: `66 4C 61 43` (`fLaC`) at offset 0. Contains STREAMINFO + at least one VORBIS_COMMENT block (the ffmpeg encoder writes it by default) and multiple audio frames. Used by `FlacStreamTests` for the round-trip identity assertion. |

## Acquisition method (Bundle A)

The plan suggested either the upstream `flac` CLI or ffmpeg's built-in encoder.
ffmpeg 7.1.1 was on `PATH`; the upstream `flac` was not. The ffmpeg path was
chosen for parity with the TTA / WavPack / MAC fixtures already in the test
tree (all generated through ffmpeg). Command:

```
ffmpeg -f lavfi -i "sine=frequency=440:duration=0.25:sample_rate=44100" \
       -ac 2 -sample_fmt s16 -c:a flac -compression_level 5 \
       sample.flac
```

Phase 2 will reference this fragment from `src/TestFiles.txt`. Do not edit
`src/TestFiles.txt` from this plan.
