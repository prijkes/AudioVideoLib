# TTA TestFiles provenance

| File | Size | Encoder | Source | Notes |
|---|---|---|---|---|
| `sample-stereo-16bit.tta` | 10212 B | ffmpeg 7.1.1 (libavcodec built-in `tta` encoder) | self-generated 0.5 s 440 Hz sine, upmixed to stereo (`-ac 2`), 44.1 kHz, 16-bit | Magic verified: `54 54 41 31` (`TTA1`) at offset 0. Single frame (22050 samples), `format=1` (TTA_FORMAT_SIMPLE), `nch=2`, `bps=16`, `sps=44100`. ffmpeg's TTA muxer does not write any tag prefix or footer — the file is the audio container alone, so `StartOffset=0` and `EndOffset` equals the full file length. |
| `sample-with-apev2.tta` | 10301 B | `sample-stereo-16bit.tta` (above) + APEv2 footer | APEv2 footer (`Title=TTA walker test`, `Artist=AudioVideoLib`) appended via the in-tree `AudioVideoLib.Tags.ApeTag` serializer (footer-only, `UseFooter=true`, `UseHeader=false`) | 89 bytes of APE data after the audio container; the trailing 32 bytes carry the `APETAGEX` footer marker so `AudioTags.ReadStream` finds it. Used for the tag-edit round-trip test. |

## Acquisition method (Bundle C)

The plan's preferred encoder, `ttaenc` from `3rdparty/libtta-c-2.3/`, was **not**
acquired because:

1. The reference CLI was not on `PATH` and the Bundle C plan does not require
   building from source; no `cmake` or `msbuild` was on `PATH` and bringing up
   the C toolchain inside the time budget was not feasible.
2. ffmpeg 7.1.1 ships a built-in `tta` encoder
   (`A....D tta                  TTA (True Audio)` in `ffmpeg -encoders`),
   which produces standards-compliant `.tta` containers with the canonical
   `TTA1` magic and seek table layout. This was the path used (option 1, just
   via ffmpeg-as-libtta-frontend rather than the standalone CLI).
3. The plan suggested an ID3v2.3 prefix for `sample-with-id3v2.tta`, but the
   ffmpeg TTA muxer does not write tag containers, and a hand-rolled ID3v2
   prefix would risk drift from the project's own `Id3v2TagReader`. Mirroring
   the WavPack Bundle D approach, the tag fixture was therefore built with
   `AudioVideoLib.Tags.ApeTag.WriteTo(...)` — guaranteed compatible with the
   project's `ApeTagReader` — and named `sample-with-apev2.tta` to reflect the
   actual container. The TTA spec accepts ID3v2 / APEv1 / APEv2 / ID3v1
   wrappers interchangeably; for the tag-edit round-trip test the choice of
   tag flavour is immaterial — what matters is that `AudioTags.ReadStream`
   finds an editable tag and that the walker's frame ranges remain stable
   across the edit.

Phase 2 will reference these fragments from `src/TestFiles.txt`. Do not
edit `src/TestFiles.txt` from this plan.
