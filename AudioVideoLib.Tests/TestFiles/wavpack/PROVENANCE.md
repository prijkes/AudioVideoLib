# WavPack TestFiles provenance

| File | Size | Encoder | Source | Notes |
|---|---|---|---|---|
| `sample-stereo-44100-16.wv` | 6462 B | ffmpeg 7.1.1 (libavcodec built-in `wavpack` encoder, `-compression_level 3`) | self-generated 0.25 s 440 Hz sine, stereo, 44.1 kHz, 16-bit (`s16p`) | Magic verified: `77 76 70 6b` (`wvpk`) at offset 0. Single block, `block_samples=11025`, flags `0x04bc1821` (BYTES_STORED=2 -> 16-bit, stereo, SRATE index 9 -> 44100 Hz). ffmpeg's encoder appended a 92-byte APEv2 footer with `encoder=Lavf61.7.100`; that footer was truncated post-encode so the wvpk block stream is the entire file (matches the round-trip test's expectation). |
| `sample-mono-48000-24.wv` | 5696 B | ffmpeg 7.1.1 (libavcodec built-in `wavpack` encoder, `-compression_level 3`, `-bits_per_raw_sample 24`) | self-generated 0.25 s 880 Hz sine, mono, 48 kHz, 24-bit (`s32p` planar input clipped to 24-bit by `bits_per_raw_sample`) | Magic verified: `wvpk`. Single block, `block_samples=12000`, flags `0x05301906` (BYTES_STORED=3 -> 24-bit, MONO_FLAG set, SRATE index 10 -> 48000 Hz). ffmpeg's appended APEv2 footer truncated post-encode. |
| `sample-with-apev2.wv` | 6555 B | `sample-stereo-44100-16.wv` (above) + APEv2 footer | APEv2 footer (`Title=WavPack walker test`, `Artist=AudioVideoLib`) appended via the in-tree `AudioVideoLib.Tags.ApeTag` serializer (no `wvtag` available) | Footer-only APEv2 (no header); 93 bytes of APE data after the wvpk block stream. Used for the tag-edit round-trip test. |

## Acquisition method (Bundle D)

The plan's preferred encoder, `wavpack` from `3rdparty/WavPack`, was **not**
acquired because:

1. The CLI (`wavpack`, `wvtag`) was not on `PATH` and the Bundle D plan
   time-boxes building from source to 15 minutes; no `cmake` or `msbuild`
   was on `PATH` and bringing up the C toolchain inside the time budget
   was not feasible.
2. ffmpeg 7.1.1 ships a built-in `wavpack` encoder
   (`Encoder wavpack [WavPack]`), which produces standards-compliant
   `.wv` block streams. This was the path used (option 1, just via
   ffmpeg-as-libwavpack-frontend rather than the standalone CLI).
3. The APEv2 footer for `sample-with-apev2.wv` was added by running a
   tiny `dotnet run` shim that referenced the project's own
   `AudioVideoLib.Tags.ApeTag.ToByteArray()` — guaranteed compatible
   with the project's `ApeTagReader`, which would otherwise have been a
   risk with a hand-rolled or third-party APE writer.

ffmpeg's encoder also auto-appends a 92-byte APEv2 footer with
`encoder=Lavf61.7.100`. That footer was stripped from the two
"clean" stereo and mono samples by truncating each file at the end of
the last `wvpk` block, so the round-trip test (which expects no
trailing tag bytes) holds.

Phase 2 will reference these fragments from `src/TestFiles.txt`. Do not
edit `src/TestFiles.txt` from this plan.
