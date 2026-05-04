# Format pack: Musepack, WavPack, TrueAudio, Monkey's Audio + FLAC/MPA retrofit

**Date:** 2026-05-04
**Status:** Approved, ready for implementation plan
**Author:** Brainstormed with Claude

## 1. Goals

Add `IMediaContainer` walkers for four formats currently missing from AudioVideoLib's coverage relative to TagLib:

- Musepack (`.mpc`) — both stream version 7 (SV7) and stream version 8 (SV8).
- WavPack (`.wv`).
- TrueAudio (`.tta`).
- Monkey's Audio (`.ape`) — the audio container, distinct from the existing APE *tag* support.

Concurrently, retrofit `FlacStream` and `MpaStream` so the entire library uses one uniform model for audio-frame lifecycle: parse-for-inspection, byte-passthrough on save, no encoder code paths anywhere.

Reference implementations are checked into `3rdparty/`:
- `3rdparty/MAC_1284_SDK/` — Monkey's Audio
- `3rdparty/WavPack/` — WavPack
- `3rdparty/libtta-c-2.3/`, `3rdparty/libtta-cpp-2.3/` — TrueAudio
- `3rdparty/musepack_src_r475/` — Musepack

## 2. Non-goals

- **No audio re-encoding.** No format in this library — new or existing — will rebuild audio bytes from the parsed model. The library's value is metadata; audio is preserved bit-identical via splice.
- **No PCM decoding.** "Inspection" means access to the encoded audio's structural fields (frame headers, predictor coefficients, Rice partitions, sub-block summaries). It does not mean reconstructing PCM samples.
- **No new tag formats.** All four new formats use APEv2 and/or ID3v1/v2, all of which are already supported by the existing `AudioTags` scanner. No tag-side work needed.
- **No tracker formats** (MOD/XM/S3M/IT). Out of scope.
- **No raw ADTS AAC, no Speex codec ID in OggStream.** Both are tracked separately as smaller follow-ups.

## 3. Architecture: source-reference lifetime model (interpretation 3)

This pattern already exists in `Mp4Stream` (see `AudioVideoLib/IO/Mp4Stream.cs:35,113-114,185`). The retrofit and the four new walkers adopt it uniformly.

### 3.1 Lifecycle contract

```csharp
public sealed class <Fmt>Stream : IMediaContainer, IDisposable
{
    private ISourceReader? _source;

    public bool ReadStream(Stream stream)
    {
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        // … parse header, build per-frame offset list …
    }

    public void WriteTo(Stream destination)
    {
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }
        // … emit header / metadata blocks / tags from model …
        foreach (var frame in _frames)
        {
            _source.CopyTo(frame.StartOffset, frame.Length, destination);
        }
    }

    public void Dispose() { _source?.Dispose(); _source = null; }
}
```

### 3.2 Caller contract

The caller must keep the source `Stream` alive between `ReadStream` and `WriteTo`. Documented as part of the `IMediaContainer` contract. Closing the source before `WriteTo` produces an `InvalidOperationException` with the message above.

### 3.3 IMediaContainer interface change

`IMediaContainer` extends `IDisposable`. All ten walkers (existing six plus four new) implement `Dispose()`. `MediaContainers` itself becomes `IDisposable` and disposes its walker collection.

This is a pre-1.0 API change. All in-tree consumers (`AudioVideoLib.Cli`, `AudioVideoLib.Demo`, `AudioVideoLib.Samples`, `AudioVideoLib.Tests`, `_doc_snippets`) must be updated to dispose the walker (or the `MediaContainers` instance) when done.

## 4. Per-format work units

Every format follows the layout:

```
AudioVideoLib/
├── Formats/
│   ├── <Fmt>Header.cs           // top-of-file descriptor — read-only
│   ├── <Fmt>Frame.cs            // per-frame model: StartOffset, Length, decoded fields
│   └── <Fmt>SeekEntry.cs        // (only if the format has one)
├── IO/
│   └── <Fmt>Stream.cs           // IMediaContainer walker — sealed, holds _source
```

All `Frame`/`Block`/`Header`/`SeekEntry` properties are read-only (no public setters). Tag editing happens through the existing `AudioTags`-scanner path.

### 4.1 Musepack (`MpcStream`)

| Aspect | Decision |
|---|---|
| Versions | Both SV7 and SV8 |
| Magic (SV7) | `MP+` at offset 0 |
| Magic (SV8) | `MPCK` at offset 0 |
| Files | `Formats/MpcStreamVersion.cs` (enum: `Sv7`, `Sv8`), `Formats/MpcStreamHeader.cs`, `Formats/MpcPacket.cs`, `IO/MpcStream.cs` |
| Frame model | `MpcPacket { Key, StartOffset, Length, SampleCount }`. `Key` is a 2-character string for SV8 packets (e.g., `AP` for audio, `SH` for stream header, `RG` for replaygain); for SV7 frames `Key` is `null`. Callers branch on `MpcStream.Version` when packet keys matter. |
| Tags carried | APEv2 (footer), ID3v2 (header) |
| Reference | `3rdparty/musepack_src_r475/include/mpc/streaminfo.h`, `mpc_demux.c` |

`MpcStream` dispatches one walker for either sub-version; the walker decides which by inspecting the magic and exposes `MpcStreamVersion Version { get; }`.

### 4.2 WavPack (`WavPackStream`)

| Aspect | Decision |
|---|---|
| Magic | `wvpk` at offset 0 |
| Files | `Formats/WavPackBlockHeader.cs`, `Formats/WavPackSubBlock.cs`, `IO/WavPackStream.cs` |
| Frame model | `WavPackBlock { Header (32-byte block header, fully decoded), StartOffset, Length, SubBlocks (read-only list of summaries: ID, size, offset) }` |
| Tags carried | APEv2 (footer), ID3v1 (footer) |
| Reference | `3rdparty/WavPack/include/wavpack.h`, `src/pack_utils.c` |

Hybrid mode (correction stream `.wvc`) is parsed if present in the same file; multi-file hybrid is out of scope.

### 4.3 TrueAudio (`TtaStream`)

| Aspect | Decision |
|---|---|
| Magic | `TTA1` at offset 0 |
| Files | `Formats/TtaHeader.cs`, `Formats/TtaSeekTable.cs`, `IO/TtaStream.cs` |
| Frame model | `TtaFrame { StartOffset, Length, SampleCount }` (per-frame size from the seek table; no further decode needed for inspection) |
| Tags carried | APEv2 (footer), ID3v1 (footer), ID3v2 (header) |
| Reference | `3rdparty/libtta-c-2.3/libtta.c` |

Simplest of the four — header + seek table fully describes the frame layout.

### 4.4 Monkey's Audio (`MacStream`)

| Aspect | Decision |
|---|---|
| Class name | `MacStream` (matches the upstream "MAC = Monkey's Audio Codec" SDK; avoids collision with `ApeTag`) |
| Magic | `MAC ` at offset 0 |
| Files | `Formats/MacDescriptor.cs`, `Formats/MacHeader.cs`, `Formats/MacSeekEntry.cs`, `IO/MacStream.cs` |
| Frame model | `MacFrame { StartOffset, Length, BlockCount }`. Frame offsets derived from the seek table in `MAC_FORMAT_FLAG_CREATE_WAV_HEADER`-aware fashion. |
| Tags carried | APEv2 (footer), ID3v1 (footer) |
| Reference | `3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp`, `APEInfo.cpp` |

The class name `MacStream` was chosen over `ApeAudioStream` and `MonkeysAudioStream` to keep clear separation from the existing `ApeTag` family.

## 5. FlacStream / MpaStream retrofit

### 5.1 FlacStream

| Component | Change |
|---|---|
| `FlacMetadataBlock` and all subclasses (`FlacVorbisCommentsMetadataBlock`, `FlacPictureMetadataBlock`, `FlacStreamInfoMetadataBlock`, `FlacApplicationMetadataBlock`, `FlacPaddingMetadataBlock`, `FlacSeekTableMetadataBlock`, `FlacCueSheetMetadataBlock`) | **No change.** Tags live here. Stay mutable & encodable. |
| `FlacFrame`, `FlacFrameHeader` | Properties become read-only. Add `StartOffset` and `Length`. Drop `ToByteArray()` and any `Write*` methods. |
| `FlacSubFrame` and variants (`FlacConstantSubFrame`, `FlacVerbatimSubFrame`, `FlacFixedSubFrame`, `FlacLinearPredictorSubFrame`), `FlacSubFrameHeader`, `FlacResidual`, `FlacRicePartition` | Properties become read-only. Drop encoder paths. Decoded values (predictor coefficients, residual values, partition counts) remain accessible for inspection. |
| `FlacStream.WriteTo()` | Emit `fLaC` magic, then each metadata block via its existing encoder, then per-frame `_source.CopyTo(frame.StartOffset, frame.Length, destination)`. |
| `FlacStream` lifecycle | Add `_source`, `Dispose()`. |

### 5.2 MpaStream

| Component | Change |
|---|---|
| `MpaFrame`, `MpaFrameHeader`, `MpaFrameData` | Properties become read-only. Add `StartOffset` and `Length`. Drop `ToByteArray()` / `Write*`. |
| `MpaSubbandQuantization`, `MpaBitAllocation`, `MpaFrameTables` | Decoder lookup tables stay; any encoder-side construction paths drop. |
| `VbrHeader`, `XingHeader`, `VbriHeader`, `LameTag` | Already read-only on the public surface. **No change.** |
| `MpaStream.WriteTo()` | Per-frame `_source.CopyTo(frame.StartOffset, frame.Length, destination)`. |
| `MpaStream` lifecycle | Add `_source`, `Dispose()`. |

### 5.3 Acceptance for the retrofit

- Round-trip identity test: read an MP3 / FLAC, write it without modification, assert byte-identical output.
- Tag-edit round-trip test: modify a Vorbis comment / nothing for MPA (MPA tag edits go through the `AudioTags` scanner, not the stream walker), save, re-parse, assert audio frames still byte-identical.
- All in-tree consumers compile against the new read-only surfaces (no `frame.ToByteArray()` calls remain).

## 6. Shared changes & integration

### 6.1 `IMediaContainer.cs`

```csharp
public interface IMediaContainer : IDisposable
{
    // … existing members unchanged …
}
```

Doc comment update: explain the source-stream lifetime contract.

### 6.2 `MediaContainers.cs`

Dispatch dictionary (`MediaContainers.cs:20-29`) gains four entries:

```csharp
{ typeof(MpcStream), () => new MpcStream() },
{ typeof(WavPackStream), () => new WavPackStream() },
{ typeof(TtaStream), () => new TtaStream() },
{ typeof(MacStream), () => new MacStream() },
```

Magic-byte probe block (`MediaContainers.cs:286-342`) gains five branches (MPC has two magic strings — both dispatch to `MpcStream`). `MediaContainers` itself becomes `IDisposable`, disposing each walker on dispose.

### 6.3 Documentation

| File | Change |
|---|---|
| `src/docs/getting-started.md` | Add four rows to the container list (after the DSF/DFF line at ~line 52). |
| `src/docs/container-formats.md` | Add four index entries. |
| `src/docs/container-formats/mpc.md` | New per-format page, follows the layout of `container-formats/flac.md`. |
| `src/docs/container-formats/wavpack.md` | New page. |
| `src/docs/container-formats/tta.md` | New page. |
| `src/docs/container-formats/mac.md` | New page. |
| `src/docs/release-notes.md` | One entry: "Adds MPC, WavPack, TTA, Monkey's Audio walkers; FLAC and MPA walkers switched to byte-passthrough save (no audio re-encode)." |

### 6.4 `_doc_snippets/Program.cs`

Adds:
- S33 — Musepack: read header, enumerate packets, report channels/sample rate/duration.
- S34 — WavPack: read header, enumerate blocks, report sub-block IDs.
- S35 — TrueAudio: read header, report frame count and duration.
- S36 — Monkey's Audio: read descriptor, enumerate frames, report compression level.

S20 (`S20_FlacEditVorbisComment`) and S30 (`S30_MpaVbrAndLame`) are reviewed; if either accesses an API that becomes read-only, the snippet is rewritten to use the inspection-only surface.

## 7. Test strategy

### 7.1 Sample inputs

Pre-encoded short samples (a few KB each), checked into `AudioVideoLib.Tests/TestFiles/` with provenance noted in `TestFiles.txt`. The `3rdparty/` encoders are not built as part of the .NET solution.

### 7.2 Per-format tests

`AudioVideoLib.Tests/IO/<Fmt>StreamTests.cs`, each covering:

1. **Header parse** — sample rate, channels, bits-per-sample, total samples match expected.
2. **Frame enumeration** — frame count, sum of frame lengths matches the audio span.
3. **Round-trip identity** — `ReadStream(s); container.WriteTo(ms);` produces byte-identical bytes for unmodified input.
4. **Tag-change round-trip** — edit an APEv2 / ID3 field via `AudioTags`, save, re-parse, audio frame `(StartOffset, Length)` list still references unchanged audio bytes.
5. **Detached-source error** — `Dispose()`, then `WriteTo()` throws `InvalidOperationException` with the documented message.
6. **Magic-byte dispatch** — `MediaContainers.ReadStream` returns the right walker type for a fresh file.

### 7.3 Retrofit tests

- New `FlacStreamTests`/`MpaStreamTests` cases asserting byte-perfect audio passthrough on save (currently un-asserted).
- Audit and rewrite any existing test that mutates `FlacFrame` / `MpaFrame` properties or calls `frame.ToByteArray()`.

### 7.4 Doc-snippet compile gate

`dotnet run --project _doc_snippets` exits 0. CI does not currently run this; it remains a manual pre-publish step.

## 8. Parallel agent dispatch plan

### Phase 0 — foundational (1 agent, sequential, ~1 hour)

- `AudioVideoLib/IO/IMediaContainer.cs` — extend `IDisposable`. Update doc comment.
- Verify `Mp4Stream`, `AsfStream`, `MatroskaStream`, `DsfStream`, `DffStream` still compile (they already implement `Dispose()`).
- `FlacStream`, `MpaStream` — add stub `Dispose() { }` so they compile. Real `_source` lifecycle comes in Phase 1.
- `MediaContainers` — implement `IDisposable`, disposing each walker.
- All in-tree callers (`AudioVideoLib.Cli`, `AudioVideoLib.Demo`, `AudioVideoLib.Samples`, `AudioVideoLib.Tests`, `_doc_snippets`) updated to dispose containers as needed.

Output: a green `dotnet build` and `dotnet test` on master with the new interface contract.

### Phase 1 — six parallel agents in worktrees

| Agent | Worktree | Creates | Modifies |
|---|---|---|---|
| **A1: MpcStream** | `feat/mpc` | `Formats/Mpc{StreamVersion,StreamHeader,Packet}.cs`, `IO/MpcStream.cs`, `Tests/IO/MpcStreamTests.cs`, `docs/container-formats/mpc.md` | none |
| **A2: WavPackStream** | `feat/wavpack` | `Formats/WavPack{BlockHeader,SubBlock}.cs`, `IO/WavPackStream.cs`, `Tests/IO/WavPackStreamTests.cs`, `docs/container-formats/wavpack.md` | none |
| **A3: TtaStream** | `feat/tta` | `Formats/Tta{Header,SeekTable}.cs`, `IO/TtaStream.cs`, `Tests/IO/TtaStreamTests.cs`, `docs/container-formats/tta.md` | none |
| **A4: MacStream** | `feat/mac` | `Formats/Mac{Descriptor,Header,SeekEntry}.cs`, `IO/MacStream.cs`, `Tests/IO/MacStreamTests.cs`, `docs/container-formats/mac.md` | none |
| **A5: FlacStream retrofit** | `feat/flac-retrofit` | none | All `Formats/Flac*.cs` (drop encoder paths, properties become read-only, frames gain offset/length). `IO/FlacStream.cs` (new `_source` lifecycle, passthrough `WriteTo`). Update `Tests/IO/FlacStream*Tests.cs`. |
| **A6: MpaStream retrofit** | `feat/mpa-retrofit` | none | All `Formats/Mpa*.cs`. `IO/MpaStream.cs`. Update `Tests/IO/MpaStreamTests.cs`. |

A1–A4 create only new files: zero conflict risk among them. A5 and A6 are scoped to disjoint file prefixes (`Flac*` vs. `Mpa*`): zero conflict against each other or against A1–A4. All six can run truly in parallel.

Each agent's brief: this design doc, the relevant `3rdparty/` reference, the canonical pattern in `Mp4Stream.cs`, format file layout from §4 / §5, and §7 acceptance criteria.

### Phase 2 — integration (1 agent, sequential, ~1-2 hours)

Runs after all six worktrees merge.

- `AudioVideoLib/IO/MediaContainers.cs` — 4 dispatch entries + 5 magic-byte probes.
- `_doc_snippets/Program.cs` — S33 (MPC), S34 (WavPack), S35 (TTA), S36 (MAC).
- `docs/getting-started.md`, `docs/container-formats.md`, `docs/release-notes.md` — updates from §6.3.
- `Tests/IO/MediaContainersTests.cs` — cross-format dispatch tests for the new probes.

### Phase 3 — validation (1 agent, ~30 min)

- `dotnet build -c Release` clean.
- `dotnet test` clean (zero failing tests).
- `dotnet run --project _doc_snippets` returns 0.
- `docfx docfx.json` clean (zero new warnings).
- Visual smoke-test: `_site/docs/container-formats/{mpc,wavpack,tta,mac}.html` render correctly with the new H2/H3 hierarchy.

### Delivery shape

All six worktrees merge into a single feature branch. The branch is squash-merged to master as one commit, per the user's preference for one big commit.

## 9. Acceptance criteria

The work is done when, against master:

- All ten `IMediaContainer` walkers (six existing + four new) implement `IDisposable` and the source-reference lifetime contract.
- `FlacStream` and `MpaStream` no longer contain encoder code paths for audio frames; their `WriteTo` is byte-passthrough.
- `MediaContainers.ReadStream` dispatches MPC (SV7 + SV8), WavPack, TTA, and MAC inputs to their walkers.
- For each new format and the two retrofitted ones, the round-trip identity test passes (read → write → byte-identical).
- For each new format, the tag-edit round-trip test passes: modify APE/ID3 via `AudioTags`, save via the walker, then re-parse the saved bytes and assert the audio frame byte ranges in the saved output match the audio frame byte ranges of the original input (i.e., the splice preserved every audio byte exactly).
- All in-tree projects build and all tests pass.
- DocFX site builds clean with new pages for the four new formats and updated index/getting-started.
- `_doc_snippets` compiles and runs with all snippets passing.

## 10. Out of scope (tracked separately)

- Raw ADTS AAC walker.
- Speex codec recognition in `OggStream`.
- Tracker formats (MOD/XM/S3M/IT).
- Decoding any format to PCM samples.
- Audio-frame mutation API for any format.
