# FLAC TestFiles provenance

## Top-level fixture

| File | Size | Encoder | Source | Notes |
|---|---|---|---|---|
| `sample.flac` | 11450 B | ffmpeg 7.1.1 (libavcodec built-in `flac` encoder, `-compression_level 5`) | self-generated 0.25 s 440 Hz sine via `lavfi`, upmixed to stereo (`-ac 2`), 44.1 kHz, 16-bit (`-sample_fmt s16`) | Magic verified: `66 4C 61 43` (`fLaC`) at offset 0. Contains STREAMINFO + at least one VORBIS_COMMENT block (the ffmpeg encoder writes it by default) and multiple audio frames. Used by `FlacStreamTests` for the round-trip identity assertion. |

### Acquisition method (Bundle A)

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

---

## Synthetic corpus (`synthetic/`)

Generated via ffmpeg 7.1.1's libavcodec FLAC encoder (the upstream `flac`
reference encoder was not on `PATH`; ffmpeg was used for parity with MPC /
WavPack / TTA / MAC fixtures already in this tree). Each `.flac` is
reproducible by re-running the corresponding command from a clean lavfi
input signal — no third-party WAV inputs required.

| File | Source signal | Encoder invocation | Notes / variant exercised |
|---|---|---|---|
| `sample-silent-stereo-44100-16.flac` | 0.25 s digital silence, stereo 44.1 kHz | `ffmpeg -f lavfi -i "anullsrc=r=44100:cl=stereo" -t 0.25 -c:a flac -compression_level 8` | Constant subframes (all samples identical). |
| `sample-sine-stereo-44100-16.flac` | 0.25 s 440 Hz sine, stereo 44.1 kHz, 16-bit | `ffmpeg -f lavfi -i "sine=frequency=440:duration=0.25:sample_rate=44100" -ac 2 -sample_fmt s16 -c:a flac -compression_level 5` | Fixed/LPC subframes; identical signal to the top-level `sample.flac`, regenerated under `synthetic/` so the corpus is self-contained. |
| `sample-sine-mono-48000-24.flac` | 0.25 s 880 Hz sine, mono 48 kHz, 24-bit | `ffmpeg -f lavfi -i "sine=frequency=880:duration=0.25:sample_rate=48000" -ac 1 -sample_fmt s32 -c:a flac -bits_per_raw_sample 24` | Mono channel assignment, 24-bit depth. |
| `sample-sine-stereo-96000-24.flac` | 0.25 s 220 Hz sine, stereo 96 kHz, 24-bit | `ffmpeg -f lavfi -i "sine=frequency=220:duration=0.25:sample_rate=96000" -ac 2 -sample_fmt s32 -c:a flac -bits_per_raw_sample 24` | High sample rate, 24-bit, stereo channel assignment. |
| `sample-sine-stereo-48000-16-c0.flac` | 0.25 s 1 kHz sine, stereo 48 kHz, 16-bit | `ffmpeg -f lavfi -i "sine=frequency=1000:duration=0.25:sample_rate=48000" -ac 2 -sample_fmt s16 -c:a flac -compression_level 0` | Compression level 0 — encoder prefers Verbatim / low-order Fixed subframes. |
| `sample-noise-mono-44100-16.flac` | 0.25 s white noise, mono 44.1 kHz, 16-bit | `ffmpeg -f lavfi -i "anoisesrc=d=0.25:c=white:r=44100:a=0.3" -ac 1 -sample_fmt s16 -c:a flac -compression_level 0` | High-entropy input; encoder typically falls back to Verbatim subframes. |
| `sample-sine-mono-22050-16.flac` | 0.25 s 300 Hz sine, mono 22.05 kHz, 16-bit | `ffmpeg -f lavfi -i "sine=frequency=300:duration=0.25:sample_rate=22050" -ac 1 -sample_fmt s16 -c:a flac -compression_level 5` | Non-CD low sample rate. |

Magic verified for each file via `xxd -l 4 <file>` — all show `66 4C 61 43`
(`fLaC`).

The exact subframe variants the encoder produces depend on libavcodec's
heuristics; the goal here is breadth of input characteristics rather than
hand-crafted subframe-type coverage. The IETF cellar reference corpus below
fills in the explicit edge cases (variable blocksize, wasted bits, all-fixed
orders, only-verbatim-subframes, partition-order 8, etc.).

---

## Reference corpus (`reference/`)

Source: <https://github.com/ietf-wg-cellar/flac-test-files> (CC0 — public
domain dedication; see `LICENSE.txt` in that repository). Author: Martijn
van Beurden. The corpus is the canonical FLAC decoder testbench used by
the IETF cellar working group during RFC 9639 development.

Files were downloaded from `https://raw.githubusercontent.com/ietf-wg-cellar/flac-test-files/main/subset/...`
on 2026-05-04 and renamed to ASCII-only filenames (spaces → `_`) so that
csproj globs and Windows path handling stay simple.

| File (local) | Original name | Variant exercised |
|---|---|---|
| `11_-_partition_order_8.flac` | `11 - partition order 8.flac` | Residual partition order 8 (deep partition tree). |
| `14_-_wasted_bits.flac` | `14 - wasted bits.flac` | Subframe wasted-bits-per-sample (unary-coded count > 0). |
| `15_-_only_verbatim_subframes.flac` | `15 - only verbatim subframes.flac` | Verbatim subframes only — exercises the `Read` path that just copies sample bits. |
| `17_-_all_fixed_orders.flac` | `17 - all fixed orders.flac` | Every Fixed-predictor order (0–4) exercised. |
| `21_-_samplerate_22050Hz.flac` | `21 - samplerate 22050Hz.flac` | Non-CD sample rate (22050 Hz). |

Magic verified for each file via `xxd -l 4 <file>` — all show `66 4C 61 43`
(`fLaC`).

The corpus has more than these 5 files (~50 in `subset/`, plus `uncommon/`
and `faulty/` directories). The selection above keeps the test bundle small
while covering the spec edges most likely to break a parser. If a future
phase needs LPC-order > 12 or Picture-block coverage, pull `12 - qlp
precision 15 bit.flac` and any of the larger samples — just keep the
PROVENANCE table in sync.
