# FLAC parser revival

**Date:** 2026-05-05
**Status:** Approved, ready for implementation plan
**Author:** Brainstormed with Claude

## 1. Goals

Make the FLAC parser correctly decode every metadata block and audio-frame variant per RFC 9639, reject spec-noncompliant input cleanly, and validate against both synthetic and FLAC-project reference vectors.

The format-pack project retrofitted `FlacStream` onto the byte-passthrough save model successfully, but a parallel audit found 11 distinct bugs in the underlying parser. Both an audit and an independent verification confirmed each finding is real with citations to RFC 9639 and the existing C# code. This project closes those bugs and adds the architecture needed to prevent the same class of bug returning.

## 2. Non-goals

- **No FLAC encoder.** Library policy (per the format-pack spec §2): no audio re-encoding. Byte-passthrough save is preserved.
- **No PCM decoding.** Inspection only. Subframe payloads are *parsed* (predictor coefficients, residual values, partition sizes exposed as read-only model) but never reconstructed to PCM samples.
- **No new public API beyond bug-fixes.** Keep the existing `FlacStream` / `FlacFrame` / etc. shape. Any property whose value was wrong gets corrected; any property that didn't exist stays absent.
- **No legacy spec compatibility shims.** Follow RFC 9639 (the current authoritative spec, 2024). Where it differs from the legacy `xiph.org/flac/format.html` table, prefer RFC 9639 wording; document the difference in comments where relevant.
- **No tolerant-mode flag.** Pre-1.0; parser is strict by default.

## 3. Spec source

RFC 9639, §1 onward. Where ambiguous, defer to libFLAC reference encoder behavior.

## 4. Architecture

The parser is structurally correct; bugs are bit-level mistakes inside otherwise-well-organised classes. Architecture stays unchanged. Work is in four layers:

### 4.1 Layer 1 — CRC primitives

`AudioVideoLib/Cryptography/`:

- `Crc8.cs`: **already correct** (polynomial `0x07`, MSB-first, fixed during the format-pack retrofit, squash-merged into master commit `12eeb57`).
- `Crc16.cs`: **rewrite**. Polynomial change (`0xA001` → `0x8005`) AND algorithm change. The existing reflected/right-shift loop becomes non-reflected/left-shift to match FLAC's MSB-first computation. Table generation and the `Calculate` loop are rewritten together — switching only the polynomial constant would still produce wrong output.

### 4.2 Layer 2 — Bit-field reading

`AudioVideoLib/IO/BitStream.cs` is the canonical bit-cursor for the codebase (proven on MPC SV7). No new helper.

The bug-prone manual shift+mask code in `FlacFrameHeader`, `FlacSubFrame`, `FlacSubFrameHeader`, `FlacResidual`, `FlacRicePartition`, `FlacStreamInfoMetadataBlock` migrates to `BitStream.ReadInt32(width)` calls. Each call site has a comment naming the field and citing the RFC 9639 section it reads.

After the migration, every FLAC bit-field read is a single named `BitStream.ReadInt32(width)` call. The discipline of "right widths" comes from the test corpus, but the migration removes the systematic MSB/LSB confusion the audit identified as the root cause.

### 4.3 Layer 3 — Per-format parsers

`AudioVideoLib/Formats/Flac*.cs` — 11 bug-fixes land here. Reactivating `FlacSubFrame.Read` (uncommenting `Read(sb)`) is the gating change — once that lands, the subframe-payload, residual, and Rice-partition fixes become live and testable.

### 4.4 Layer 4 — Test corpus and infrastructure

`AudioVideoLib.Tests/TestFiles/flac/` and new test classes. Detailed in §6.

## 5. Bug-fix grouping

The 11 bugs cluster into 5 self-contained workstreams executed in order. Each cluster lands as one logical change with its own test additions.

### 5.1 Cluster 1 — CRC-16 fundamentals

| Bug | File | Fix |
|---|---|---|
| 1 | `Cryptography/Crc16.cs` | Polynomial `0xA001` → `0x8005`. Rewrite table generation and `Calculate` loop to be non-reflected (left-shift MSB-first), matching FLAC's CRC-16 spec. |
| 2 | `Formats/FlacFrame.cs:110` | `Crc16.Calculate([])` is replaced with `Crc16.Calculate(frameBytes)` where `frameBytes` is the slice from frame start through the byte before the stored CRC-16. |

Direct CRC unit tests added against published FLAC vectors. Lands first — every other test depends on real CRC validation.

### 5.2 Cluster 2 — Frame header validation

| Bug | File | Fix |
|---|---|---|
| 5 | `Formats/FlacFrame.cs:18,192` | `FrameSync = 0x7FFE` → `0x3FFE`; mask comparison adjusts to a 14-bit field. The 14-bit FLAC sync is `0b11111111111110` exactly. |
| 6 | `Formats/FlacFrameHeader.cs` | Reserved bits at frame-header bit 17 and bit 0 are validated to be `0`; non-zero values cause `ReadFrame` to return `null`. |

Lands second — clean frame iteration is required before the subframe stack.

### 5.3 Cluster 3 — Subframe stack reactivation

| Bug | File | Fix |
|---|---|---|
| 3 | `Formats/FlacSubFrame.cs:96` | Uncomment `Read(sb)`. Subframe payload now consumed. |
| 4 | `Formats/FlacSubFrame.cs:69-70` | Type extraction switches to `BitStream` reading byte 0 of the subframe header (1 zero-pad bit + 6-bit type + 1-bit wasted-bits flag). |
| 8 | `Formats/FlacResidual.cs:9-11` | Coding method = `(_values >> 6) & 0x03`; partition order = `(_values >> 2) & 0x0F`. (Method MSB, order next, per RFC 9639 §11.30.) |
| 9 | `Formats/FlacRicePartition.cs:23` | PartitionedRice = 4-bit Rice parameter (mask `0x0F`); PartitionedRice2 = 5-bit (`0x1F`). Inversion fixed. Escape-code comparisons (`riceParameter < 0xF` etc.) similarly inverted. |

Migrate `FlacSubFrameHeader.cs` away from the wrong 4-byte `ReadBigEndianInt32` to single-byte read via `BitStream`.

Lands third — the subframe payload now consumes bits correctly so frame CRCs become checkable.

### 5.4 Cluster 4 — Metadata-block correctness

| Bug | File | Fix |
|---|---|---|
| 7 | `Formats/FlacStreamInfoMetadataBlock.cs:104` | Channels mask `0x1F` → `0x07`. (3-bit field per RFC 9639 §8.2.) |
| 10 | `Formats/FlacMetadataBlock.cs:127` | Length check `length >= stream.Length` → `stream.Position + length > stream.Length`. |
| 11 | `Formats/FlacCueSheetMetadataBlock.cs` | Reserved-padding size 256 → 258 bytes (writer). TrackType / PreEmphasis flag bits move from bits 0/1 → bits 7/6 (both reader and writer). Writer's `&` typo → `|` for combining flags. |

Independent of the audio-frame stack; can land in parallel with cluster 3 if a separate worktree is used, otherwise sequentially.

### 5.5 Cluster 5 — `BitStream` migration audit (preventive)

Sweep `Flac*.cs` for any remaining manual shift+mask bit-field reads; replace with `BitStream.ReadInt32(width)` + inline RFC 9639 reference comment. No bugs to fix in this cluster — purely a regression-prevention pass. Catches any drift the verifier flagged ("codebase oscillates between LE and BE bit-field reading").

### 5.6 Sequencing

Cluster 1 → 2 → 3 → 5, with cluster 4 independent of clusters 2-3 and landable any time after cluster 1. Recommended order on a single feature branch: 1, 2, 3, 4, 5. Each cluster has its own test set proving its bugs are gone. The 3 currently-`[Skip]`-marked FLAC tests (`RoundTrip_UnmodifiedInput_ProducesByteIdenticalOutput`, `TagEdit_PreservesAudioBytes`, `VorbisFlacTests.FlacMetadataBlock_ToByteArray_VorbisCommentBlockRoundTrips`) are reactivated by the end of clusters 3 and 4.

## 6. Test infrastructure

### 6.1 Corpus

`AudioVideoLib.Tests/TestFiles/flac/` with three subdirectories:

| Subdirectory | Contents | Source |
|---|---|---|
| `synthetic/` | ~10 samples covering Constant/Verbatim/Fixed-N/LPC-N variants, both blocking strategies, common channel assignments. WAV inputs are 0.25–0.5s at 44.1/48/96 kHz, 16/24-bit. | Generated via `flac` CLI (or `ffmpeg` if `flac` unavailable); reproducible from checked-in `.wav` source signals. |
| `reference/` | ~5 spec-compliance edge cases (variable blocksize, LPC order 25+, weird channel assignments, max-block-size, non-CD sample rates). | xiph.org/flac project's published test vectors, BSD-3 licensed, redistributable. |
| `pathological/` | ~6 intentionally malformed cases: truncated metadata block, illegal sync `0x3FFF`, reserved bit set, CRC-8 mismatch, CRC-16 mismatch, length-past-EOF. | Hand-built byte arrays inline in test code (NOT checked-in files; static byte literal per case). |

A `PROVENANCE.md` at the corpus root lists every file, the exact `flac`/`ffmpeg` invocation that produced it, and the input WAV's parameters. Mirrors the pattern from MPC/WavPack/TTA/MAC.

### 6.2 New test classes

- **`FlacParserComplianceTests`** — `[Theory]` over each corpus file. Per-file assertions: header values match expected (sample rate, channels, bits-per-sample, total samples); subframe types per channel match expected; frame CRCs validate against `Crc16.Calculate(...)`; metadata-block walker visits every block.
- **`FlacFrameCrcTests`** — direct unit tests on `Crc8` and `Crc16` against published FLAC vectors. At minimum `crc8([0xFF, 0xF8, 0x59, 0x88, 0x00]) == 0x8a` and one CRC-16 vector from a known FLAC frame.
- **`FlacRejectsMalformedTests`** — `[Theory]` over the pathological cases. Each must produce a clean rejection (`ReadStream` returns `false` OR raises `MediaContainerParse` event with the malformed offset).

### 6.3 Reactivation of currently-skipped tests

- `FlacStreamTests.RoundTrip_UnmodifiedInput_ProducesByteIdenticalOutput` — un-Skip after cluster 3.
- `FlacStreamTests.TagEdit_PreservesAudioBytes` — un-Skip after cluster 3.
- `VorbisFlacTests.FlacMetadataBlock_ToByteArray_VorbisCommentBlockRoundTrips` — un-Skip; this is the metadata-block round-trip case disabled due to an unrelated VorbisComments redundant-length-prefix bug surfaced during the format-pack work. Verify whether that's still the cause; fix if so, otherwise document.

### 6.4 External tool dependency

`flac` (or `ffmpeg`) must be on PATH for synthetic-corpus regeneration (one-time + when adding new variants). The test suite does NOT shell out to encoders at runtime — corpus files are checked in. Tests run with no external dependencies.

## 7. Validation behavior

**Stance: strict.** Spec-noncompliant input is rejected, not best-effort-parsed.

| Condition | Behavior |
|---|---|
| Frame sync 14 bits != `0x3FFE` | `FlacFrame.ReadFrame` returns `null`; `MediaContainers` advances by 1 byte. |
| Frame header reserved bits 17 or 0 != 0 | Reject as not-a-frame. |
| Frame header CRC-8 mismatch | Reject; raise event. |
| Frame footer CRC-16 mismatch | Reject the *frame* (not the whole stream); event raised; walker continues looking for next valid sync. |
| Metadata-block length runs past stream end | `FlacMetadataBlock.ReadBlock` returns `null`; `FlacStream.ReadStream` stops accepting metadata blocks. |
| Subframe type is reserved | `FlacSubFrame.ReadSubFrame` returns `null`; parent frame rejected. |
| CueSheet padding wrong size or flag bits unexpected | Decode what's there; no re-validation. The writer fix ensures we *produce* spec-compliant output going forward. |

The existing `MediaContainers.MediaContainerParse` and `MediaContainerParsedEventArgs` event types are reused. No new event types.

## 8. Acceptance criteria

The project is done when, against master:

1. **All 11 audited bugs are fixed.** Each has a regression test that fails before its fix and passes after.
2. **The 3 currently-`[Skip]`-marked FLAC tests are unconditionally green.**
3. **CRC primitives validate against published FLAC vectors.** `Crc8.Calculate([0xFF, 0xF8, 0x59, 0x88, 0x00]) == 0x8a`. At least one CRC-16 vector from a real FLAC frame validates (computed via `metaflac` or the FLAC reference decoder externally as ground truth, then asserted in test).
4. **Synthetic corpus passes.** Every file in `TestFiles/flac/synthetic/` parses, all assertions in `FlacParserComplianceTests` hold, every frame's CRC-16 validates.
5. **Reference corpus passes.** xiph.org test vectors in `TestFiles/flac/reference/` parse cleanly.
6. **Pathological corpus rejects cleanly.** Every malformed-sample test produces a clean rejection (no exception beyond the documented `ArgumentNullException` boundary).
7. **Bit-field reads are uniform.** `grep` for manual shift+mask patterns (`>>` followed by `& 0x[0-9A-F]+`) in `Flac*.cs` files produces only matches where the inline comment cites the RFC 9639 section and the field name. No naked shift+mask bit-field reads remain.
8. **Build and test gates clean.** `dotnet build -c Release` zero new warnings, `dotnet test` zero failures, `dotnet run --project _doc_snippets` exits 0, `docfx docfx.json` clean (six pre-existing `InvalidCref` warnings allowed).
9. **Byte-passthrough invariant preserved.** An unmodified FLAC reads then writes byte-identically (the format-pack retrofit invariant still holds).
10. **Documentation:**
    - `docs/release-notes.md` gains a "Fixed" section listing the 11 bugs with one-line summaries each.
    - `docs/container-formats/flacstream.md` gains a "Validation rules" section documenting strict-rejection semantics.

## 9. Out of scope (tracked separately)

- PCM decoding from subframe payloads.
- FLAC encoder.
- Tolerant-mode flag.
- Migration of non-FLAC walkers to use the same `BitStream` audit pattern (could be done as a follow-up but is not gated on this project).
