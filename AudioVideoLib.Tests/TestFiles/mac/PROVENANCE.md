# MAC (Monkey's Audio) TestFiles provenance

| File | Size | Source | Notes |
|---|---|---|---|
| `sample.ape` | 134528 B | Truncated from `3rdparty/getid3-mp3-testfiles/luckynight.ape` (kept first frame only) | Magic verified: `4d 41 43 20` (`MAC `, integer variant). Version 3990, compression-level 2000 (normal), 16-bit stereo @ 44.1 kHz, 1 frame of 73728 blocks. `CreateWavHeaderFlag` CLEAR (44-byte preserved WAV header sits between the seek table and the audio region). Used for the round-trip identity, frame enumeration, and `_HandlesWavHeaderFlag_Off` tests. |
| `sample-with-apev2.ape` | 134793 B | `sample.ape` (above) + the original 265-byte APEv2 footer copied verbatim from `luckynight.ape` | Magic verified: `MAC `. The trailing tag carries 7 items (`Title=Lucky Night`, `Artist=Jody Marie Gnant`, `Album=Treasure Quest Soundtrack`, `Comment=...`, `Track=9`, `Year=1995`, `Genre=Soundtrack`) and is footer-only (no header form). Used for the tag-edit round-trip test: parse via `AudioTags`, mutate the `Title` field via `ApeTag.SetItem`, splice, re-parse, and assert audio frame ranges + audio bytes preserved. |
| `sample-no-wavheader.ape` | 134484 B | Synthesized from `sample.ape`: removed the 44-byte preserved WAV-header region, set `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` (bit 5, `0x20`) in `formatFlags`, zeroed `descriptor.HeaderDataBytes`, and rebased the seek table by -44. | Magic verified: `MAC `. Audio bytes are byte-for-byte the same upstream-encoded payload as `sample.ape`; only the file layout was rewritten so the walker exercises the `CreatesWavHeaderOnDecode == true` branch. Used for the `_HandlesWavHeaderFlag_On` test. |

## Acquisition method (Bundle D)

Sample acquisition followed **option 3** ("public-domain `.ape` sample on
disk") of the four options enumerated in Bundle D. None of the other
options panned out cleanly:

1. **Reference encoder (`mac` / `monkey` / `monkeyaudio` CLI).** Not on
   `PATH`; no precompiled MAC.exe available on this machine. The MAC
   1284 SDK ships at `3rdparty/MAC_1284_SDK/` (BSD-3-clause), but
   bringing up the C++ toolchain to build the SDK Console encoder
   inside the time budget was not feasible — `cmake` was not on `PATH`
   for the project's `CMakeLists.txt`, and the MSVC solution would
   need a Visual Studio 2022 install. The 15-minute time-box ruled
   this out.
2. **ffmpeg APE support.** `ffmpeg -encoders | grep -i ape` returns
   no APE encoder. ffmpeg has DECODE-only support for Monkey's Audio
   (`ape  Monkey's Audio` shows up in `-codecs` as `D.AI.S`), so it
   cannot be used to produce a `.ape` file.
3. **Found-on-disk sample.** `3rdparty/getid3-mp3-testfiles/` contains
   `luckynight.ape` and `sh3.ape`. `luckynight.ape` is the canonical
   Monkey's Audio demo sample distributed by Matthew Ashland — its
   embedded APEv2 `Comment` field reads literally
   *"1-minute song sample demonstrating Monkey's Audio .ape
   compression"*, with `Title=Lucky Night`, `Artist=Jody Marie Gnant`,
   `Album=Treasure Quest Soundtrack`. It is a long-standing
   redistributable demo file (also bundled with the public PHP getID3
   library's test corpus, where it lives in this checkout). Its 6.5
   MiB size is too large to commit verbatim, so it was *truncated* to
   the first frame only — the descriptor / header / seek table were
   recomputed to reflect the new totals (`totalFrames=1`,
   `apeFrameDataBytes=134404`, single seek entry rebased to the new
   audio start), but the actual frame body bytes are the
   unmodified upstream-encoded payload from the original file. The
   resulting 134 KiB file is a structurally valid APE 3990 container
   that the byte-passthrough walker round-trips bit-exactly.
   `sh3.ape` was not used: it is APE version 3970 (pre-3.98), which
   uses the older `APE_HEADER_OLD` layout that this walker explicitly
   does not support.
4. **Skip-as-fallback.** Not needed — option 3 produced viable
   samples.

## Why three samples instead of one

The plan body (task 14) lists a single `sample.ape`. The user's task
description splits the WAV-header-flag toggle test into `_Off` /
`_On` real-file variants, so two .ape files with different
`MAC_FORMAT_FLAG_CREATE_WAV_HEADER` states are needed. The third
file (`sample-with-apev2.ape`) carries the APEv2 footer that the
tag-edit round-trip test depends on — that footer is intentionally
absent from `sample.ape` to keep the round-trip-identity test's
input "clean" (no trailing tag bytes that the walker doesn't
account for). All three were derived from the same upstream-encoded
audio payload to keep the corpus footprint small.

## csproj wiring

`AudioVideoLib.Tests/AudioVideoLib.Tests.csproj` declares

    <None Include="TestFiles\mac\*.ape" CopyToOutputDirectory="PreserveNewest" />

so all three `*.ape` files are copied to `bin/Debug/net10.0/TestFiles/mac/`
on build.

Phase 2 will reference this fragment from `src/TestFiles.txt`. Do not
edit `src/TestFiles.txt` from this plan.
