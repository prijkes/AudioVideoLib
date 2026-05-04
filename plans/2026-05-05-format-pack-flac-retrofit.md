# Format pack — FlacStream retrofit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retrofit `FlacStream` onto the offset-based byte-passthrough model. Drop encoder code paths from audio-frame classes (`FlacFrame`, `FlacSubFrame` and variants, `FlacResidual`, `FlacRicePartition`, `FlacSubFrameHeader`, `FlacFrameHeader`); make their properties read-only and add `StartOffset`/`Length` to per-frame objects. Preserve existing metadata-block encoders (tags live there). `FlacStream.WriteTo` becomes: emit `fLaC` magic + each metadata block (encoded via existing path) + per-frame `_source.CopyTo(frame.StartOffset, frame.Length, destination)`.

**Architecture:** `FlacStream` is declared `partial` across two files in `IO/` — both are in scope: `IO/FlacStream.cs` (the primary file: identifier, frame list, `ReadStream`, `WriteTo`, lifecycle) and `IO/FlacStreamMetadataBlocks.cs` (typed accessors for metadata blocks; metadata-block encoders stay untouched). The retrofit adds a `private ISourceReader? _source` to the primary file, populates it during `ReadStream`, replaces the per-frame encoder loop in `WriteTo` with an offset-based splice, and replaces the no-op `Dispose()` stub from Phase 0 with the real one. Audio-frame format classes (`FlacFrame`, `FlacFrameHeader`, all `FlacSubFrame` variants, `FlacResidual`, `FlacRicePartition`, `FlacSubFrameHeader`) shed their encoder paths — most of which are already commented out per spec §5.1's "Note on existing state" callout — and their properties become read-only.

**Tech Stack:** C# 13 / .NET 10, xUnit.

**Worktree:** `feat/flac-retrofit` (per spec §8 Phase 1).

**Reference:** Spec §3 (architecture), §5.1 (FLAC retrofit table), §7.3 (retrofit acceptance), §8 Phase 1 row A5. Phase 0 plan (`plans/2026-05-05-format-pack-phase0-foundation.md`) Task 5 — assumed executed; this plan replaces the no-op `Dispose()` it added.

**Canonical exception message** (from spec §3.1):
```
Source stream was detached or never read. WriteTo requires a live source.
```

**Pattern source:** `AudioVideoLib/IO/Mp4Stream.cs` (specifically the `_source` field declaration at line 35, the `ReadStream` populate at 113-114, the `WriteTo` null-check at 144-148, and `Dispose` at 165-169).

---

## File scope

### Audio-frame files — encoder paths drop, properties become read-only

| File | Status of existing encoder | Retrofit action |
|---|---|---|
| `Formats/FlacFrame.cs` | Live `ToByteArray()` (lines 87-127) | Delete `ToByteArray()`. Add `Length` to companion (`StartOffset` already present from existing `IAudioFrame` impl). |
| `Formats/FlacFrameHeader.cs` | Decoder only; no encoder | Properties already `private set;`. No code change beyond the audit step. |
| `Formats/FlacSubFrame.cs` | Live `virtual ToByteArray()` (lines 56-62) | Delete the override-able `ToByteArray()`. Properties stay `{ get; private set; }`. |
| `Formats/FlacSubFrameHeader.cs` | Decoder only | Audit only — no setters to remove. |
| `Formats/FlacConstantSubFrame.cs` | **Live** `override ToByteArray()` (lines 23-29). Read still commented out. | Delete the override. Property `UnencodedConstantValue` stays read-only (already `private set;`). |
| `Formats/FlacVerbatimSubFrame.cs` | No encoder; only `Read()` is live | Audit only. |
| `Formats/FlacFixedSubFrame.cs` | Encoder & read **already commented out** (lines 28-48) | Remove the commented-out blocks for cleanliness. Property accessors already read-only. |
| `Formats/FlacLinearPredictorSubFrame.cs` | Decoder only | Audit only. |
| `Formats/FlacResidual.cs` | Encoder **already commented out** (lines 30-40) | Remove commented-out block. |
| `Formats/FlacRicePartition.cs` | Encoder **already commented out** (lines 52-68) | Remove commented-out block. |

### Metadata-block files — NO CHANGE

These hold the tag data and remain mutable & encodable:

- `Formats/FlacMetadataBlock.cs`
- `Formats/FlacMetadataBlockHeaderFlags.cs`
- `Formats/FlacMetadataBlockType.cs`
- `Formats/FlacStreamInfoMetadataBlock.cs`
- `Formats/FlacApplicationMetadataBlock.cs`
- `Formats/FlacPaddingMetadataBlock.cs`
- `Formats/FlacSeekTableMetadataBlock.cs`
- `Formats/FlacSeekPoint.cs`
- `Formats/FlacVorbisCommentsMetadataBlock.cs`
- `Formats/FlacPictureMetadataBlock.cs`
- `Formats/FlacPictureType.cs`
- `Formats/FlacCueSheetMetadataBlock.cs`
- `Formats/FlacCueSheetTrack.cs`
- `Formats/FlacCueSheetTrackIndexPoint.cs`
- `Formats/FlacCueSheetPreEmphasis.cs`
- `Formats/FlacCueSheetTrackType.cs`
- `Formats/FlacBlockingStrategy.cs` (enum — no methods)
- `Formats/FlacChannelAssignment.cs` (enum)
- `Formats/FlacResidualCodingMethod.cs` (enum)
- `Formats/FlacSubFrameType.cs` (enum)

### IO files — primary retrofit

- `IO/FlacStream.cs` — replace the no-op `Dispose()` stub from Phase 0 with the real one (`_source?.Dispose(); _source = null;`); add `private ISourceReader? _source` field; update `ReadStream` to populate `_source`; rewrite `WriteTo` to byte-passthrough audio frames.
- `IO/FlacStreamMetadataBlocks.cs` — confirms it still compiles after the audio-frame encoders are removed (it should — its surface is metadata-block accessors only). No code change expected.

### Test files

The repo has exactly one FLAC-related test file: `AudioVideoLib.Tests/VorbisFlacTests.cs`. A grep for `FlacFrame`, `FlacSubFrame`, or `new FlacStream(` returns zero hits within it — every existing FLAC test exercises metadata-block parsing only. The retrofit can therefore proceed without rewriting any existing test, but **must add new tests**:

- `AudioVideoLib.Tests/IO/FlacStreamTests.cs` (new file) — round-trip identity, tag-edit round-trip, detached-source error, `_source` null-check.

### What this plan does NOT touch

- `IO/MediaContainers.cs` — Phase 2.
- `_doc_snippets/Program.cs` — Phase 2.
- `docs/getting-started.md`, `docs/container-formats.md`, `docs/release-notes.md`, `docs/container-formats/flac.md` — Phase 2.
- `src/TestFiles.txt` — Phase 2 (per spec §7.1).

### Files this plan creates

- `AudioVideoLib.Tests/IO/FlacStreamTests.cs` — new test file (the existing `VorbisFlacTests.cs` covers metadata-block parsing only; round-trip and lifecycle tests need a dedicated home).
- `AudioVideoLib.Tests/TestFiles/flac/PROVENANCE.md` — provenance fragment for the new sample file (per spec §7.1).
- `AudioVideoLib.Tests/TestFiles/flac/sample.flac` — small (a few KB) FLAC sample used by the round-trip test. **This is binary; the agent must source one** (synthesise a 1-second sine via a 3rd-party encoder or grab a permissively-licensed sample). Provenance recorded in the fragment.

---

## Tasks

### Task 1: Drop a small FLAC sample into the test tree and write the round-trip identity test (Red)

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/flac/sample.flac` (binary — synth or licensed source)
- Create: `AudioVideoLib.Tests/TestFiles/flac/PROVENANCE.md`
- Create: `AudioVideoLib.Tests/IO/FlacStreamTests.cs`

- [ ] **Step 1: Acquire a small FLAC sample (2-30 KB)**

The sample must be a real FLAC: `fLaC` magic, a STREAMINFO block, optional VORBIS_COMMENT, at least 2 audio frames. Synthesise one with `flac` (the upstream encoder) from a 0.25–0.5 second WAV, or use a Public Domain / CC0 sample. Save as `AudioVideoLib.Tests/TestFiles/flac/sample.flac`.

- [ ] **Step 2: Record provenance**

Create `AudioVideoLib.Tests/TestFiles/flac/PROVENANCE.md`:

```markdown
# FLAC test samples

| File | Source | Licence | Notes |
|---|---|---|---|
| sample.flac | <fill in: e.g., "synthesised from a 0.25 s 440 Hz sine WAV via flac 1.4.3, default settings"> | CC0 / synth | <duration, sample rate, channels, bits/sample> |
```

This fragment is referenced by `src/TestFiles.txt` in Phase 2 (per spec §7.1).

- [ ] **Step 3: Write the failing round-trip test**

Create `AudioVideoLib.Tests/IO/FlacStreamTests.cs`:

```csharp
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using AudioVideoLib.IO;
using Xunit;

public sealed class FlacStreamTests
{
    private const string SamplePath = "TestFiles/flac/sample.flac";

    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void RoundTrip_UnmodifiedInput_ProducesByteIdenticalOutput()
    {
        var original = File.ReadAllBytes(SamplePath);

        using var input = new MemoryStream(original, writable: false);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(original, output.ToArray());
    }
}
```

- [ ] **Step 4: Run the new test**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~FlacStreamTests.RoundTrip_UnmodifiedInput"
```

Expected: **fail** — current `WriteTo` re-encodes audio frames (and the encoder is incomplete per the spec §5.1 callout, so the bytes won't match the input even when nothing is mutated). The failure message will likely be a length or byte-by-byte assertion mismatch. This is the canonical Red.

- [ ] **Step 5: Make sure the test file is wired into the test project**

Confirm `AudioVideoLib.Tests.csproj` has `<None Include="TestFiles\**\*" CopyToOutputDirectory="PreserveNewest" />` (or similar) so the sample is copied to the test output. If not, add it. Re-run the test; if `FileNotFoundException`, fix the csproj first. The test should fail on assertion equality, not file IO.

---

### Task 2: Audit `Formats/FlacFrame.cs` — remove `ToByteArray`, add `Length`

**Files:**
- Modify: `AudioVideoLib/Formats/FlacFrame.cs`

- [ ] **Step 1: Confirm the existing public surface**

`FlacFrame.cs` declares two public properties from `IAudioFrame`:

```csharp
public long StartOffset { get; private set; }
public long EndOffset { get; private set; }
```

Both are populated in `ReadFrame` at lines 135 and 155. `Length` is currently exposed via `FlacFrameHeader.cs` as `FrameLength { get => throw new NotImplementedException(); }`. We will route the byte-passthrough through `EndOffset - StartOffset`; no new field is strictly required, but for parity with the `Mp4Box`-style canonical pattern and for callers that want a friendlier name, expose `Length` as a computed property in `FlacFrame.cs`.

- [ ] **Step 2: Delete the `ToByteArray` method**

Remove lines 83-127 (the `ToByteArray` summary doc and method body). The block to delete is exactly:

```csharp
    /// <summary>
    /// Returns the frame in a byte array.
    /// </summary>
    /// <returns>The frame in a byte array.</returns>
    public byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.WriteBigEndianInt32(_header);
        sb.Write(_sampleFrameNumberBytes);
        var blockSize = (_header >> 12) & 0xF;
        switch (blockSize)
        {
            case 0x06:
                sb.WriteByte((byte)(BlockSize - 1));
                break;

            case 0x07:
                sb.WriteBigEndianInt16((short)(BlockSize - 1));
                break;
        }
        var samplingRate = (_header >> 8) & 0xF;
        switch (samplingRate)
        {
            case 0x0C:
                sb.WriteByte((byte)(SamplingRate / 1000));
                break;

            case 0x0D:
                sb.WriteBigEndianInt16((short)SamplingRate);
                break;

            case 0x0E:
                sb.WriteBigEndianInt16((short)(SamplingRate / 10));
                break;
        }
        sb.WriteByte((byte)_crc8);

        foreach (var subFrame in SubFrames)
        {
            sb.Write(subFrame.ToByteArray());
        }

        sb.WriteBigEndianInt16((short)_crc16);
        return sb.ToByteArray();
    }
```

- [ ] **Step 3: Add the `Length` computed property**

Just below the existing `EndOffset` property (line 45), add:

```csharp
    /// <summary>
    /// Gets the frame length in source-stream bytes (<see cref="EndOffset"/> − <see cref="StartOffset"/>).
    /// Used by <see cref="IO.FlacStream.WriteTo"/> for byte-passthrough.
    /// </summary>
    public long Length => EndOffset - StartOffset;
```

- [ ] **Step 4: Build to verify**

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected outcome: `FlacStream.cs` will fail to compile — the `WriteTo` body still calls `frame.ToByteArray()`. That's the next task. `FlacFrame.cs` itself should compile clean.

---

### Task 3: Audit `Formats/FlacFrameHeader.cs` — read-only confirmed, no code change

**Files:**
- Modify (audit only — confirm no edits needed): `AudioVideoLib/Formats/FlacFrameHeader.cs`

- [ ] **Step 1: Verify all public properties are read-only**

Open `FlacFrameHeader.cs`. Walk every `public` property and confirm it is one of:

```csharp
public X Property { get; private set; }
public X Property => /* expression */;
```

Properties to verify: `BlockingStrategy`, `BlockSize`, `SampleSize`, `SamplingRate`, `ChannelAssignment`, `Channels`, `Samples`, `FrameNumber`, `Bitrate`, `FrameLength`, `FrameSize`, `AudioLength`. As of the current file all of these are already conformant.

- [ ] **Step 2: Note the `NotImplementedException` properties**

Three properties throw `NotImplementedException`: `Bitrate`, `FrameLength`, `FrameSize`, `AudioLength`. These are pre-existing bugs unrelated to the retrofit. **Do not fix them in this plan** — they are independent of the byte-passthrough retrofit and would expand scope. Track separately.

- [ ] **Step 3: Confirm no encoder-shaped helpers remain**

Grep within the file for `ToByteArray`, `WriteTo`, `Encode`, `Serialize`, `Write*Endian`. There should be none. (The decoder-side `ReadBigEndianUtf8Int64` is fine — it reads, not writes.)

```
grep -nE "ToByteArray|WriteTo|Encode|Serialize|Write(Big|Little)" AudioVideoLib/Formats/FlacFrameHeader.cs
```

Expected: empty output.

- [ ] **Step 4: No edit; advance**

If steps 1-3 all pass, this file needs no modification. If any property has a public setter or any encoder-shaped method exists, lock the property and remove the method (matching the pattern in Task 2).

---

### Task 4: Audit `Formats/FlacSubFrame.cs` — remove the virtual `ToByteArray`

**Files:**
- Modify: `AudioVideoLib/Formats/FlacSubFrame.cs`

- [ ] **Step 1: Delete the virtual `ToByteArray`**

Lines 52-62 (the virtual `ToByteArray` and its doc comment) need removal:

```csharp
    /// <summary>
    /// Returns the frame in a byte array.
    /// </summary>
    /// <returns>The frame in a byte array.</returns>
    public virtual byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.WriteBigEndianInt32(Header);
        sb.WriteUnaryInt(WastedBits);
        return sb.ToByteArray();
    }
```

Remove the whole block.

- [ ] **Step 2: Confirm the sealed members compile**

The remaining surface is the constructor, `FlacFrame`, `SubFrames`, `ReadFrame`, the protected virtual `Read`, and the private dispatch helpers. Build:

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected: subclass `FlacConstantSubFrame.cs` will fail because it has `override byte[] ToByteArray()` — that's Task 5. Other build errors in `FlacStream.cs` from Task 2 also persist. `FlacSubFrame.cs` itself is now clean.

---

### Task 5: Audit `Formats/FlacConstantSubFrame.cs` — remove the live override

**Files:**
- Modify: `AudioVideoLib/Formats/FlacConstantSubFrame.cs`

- [ ] **Step 1: Delete the override**

Remove lines 22-29:

```csharp
    /// <inheritdoc />
    public override byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.Write(base.ToByteArray());
        sb.WriteBigEndianBytes(UnencodedConstantValue, SampleSize / 8);
        return sb.ToByteArray();
    }
```

Also remove the unused `using AudioVideoLib.IO;` at line 3 if no other reference to that namespace remains in the file (the only consumer was `StreamBuffer`).

- [ ] **Step 2: Confirm the commented-out `Read` block stays as documentation or remove it**

Lines 31-34 are a commented-out `protected override void Read`. Per Task scope (clean-up of dead encoder paths), remove it:

```csharp
    ////protected override void Read(StreamBuffer sb)
    ////{
    ////    UnencodedConstantValue = sb.ReadBigEndianInt(SampleSize / 8);
    ////}
```

(Note: this block is technically a *decoder* stub — a never-implemented `Read` override. Removing it tightens the file but does not change behavior; the live decoder dispatch in `FlacSubFrame.ReadSubFrame` doesn't call `Read` for `FlacConstantSubFrame` anyway.)

- [ ] **Step 3: Property surface confirmation**

`UnencodedConstantValue` is `{ get; private set; }`. No edit. The setter is currently unreachable (the commented-out `Read` was the only writer). Track separately if the team wants to start populating it; not in scope for this plan.

- [ ] **Step 4: Build**

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected: `FlacConstantSubFrame.cs` builds clean. Other errors persist (`FlacStream.WriteTo` still references `frame.ToByteArray()`).

---

### Task 6: Audit `Formats/FlacVerbatimSubFrame.cs` — read-only, no change

**Files:**
- Modify (audit only): `AudioVideoLib/Formats/FlacVerbatimSubFrame.cs`

- [ ] **Step 1: Verify there is no encoder method**

The file declares no `ToByteArray`. The single property `UnencodedSubblocks` is `{ get; private set; }`. The `Read` override is decoder-only.

- [ ] **Step 2: No edit**

Move on to the next variant.

---

### Task 7: Audit `Formats/FlacFixedSubFrame.cs` — strip commented-out blocks

**Files:**
- Modify: `AudioVideoLib/Formats/FlacFixedSubFrame.cs`

- [ ] **Step 1: Remove the commented-out encoder & decoder stubs**

Per spec §5.1 the encoder methods here are already commented out. Remove lines 28-48 — both the `ToByteArray` and the `Read` blocks:

```csharp
    //public override byte[] ToByteArray()
    //{
    //    using (StreamBuffer sb = new StreamBuffer())
    //    {
    //        sb.Write(base.ToByteArray());
    //        for (int i = 0; i < Order; i ++)
    //            sb.WriteBigEndianBytes(UnencodedWarmUpSamples[i], SampleSize / 8);

    //        sb.Write(Residual.ToByteArray());
    //        return sb.ToByteArray();
    //    }
    //}

    //protected override void Read(StreamBuffer sb)
    //{
    //    UnencodedWarmUpSamples = new int[Order];
    //    for (int i = 0; i < Order; i++)
    //        UnencodedWarmUpSamples[i] = sb.ReadInt(SampleSize * 8);

    //    Residual = FlacResidual.Read(sb, FlacFrame.BlockSize, Order);
    //}
```

- [ ] **Step 2: Property surface**

`UnencodedWarmUpSamples` and `Residual` both `{ get; private set; }` — already read-only.

- [ ] **Step 3: Build**

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected: file builds clean.

---

### Task 8: Audit `Formats/FlacLinearPredictorSubFrame.cs` — read-only, no change

**Files:**
- Modify (audit only): `AudioVideoLib/Formats/FlacLinearPredictorSubFrame.cs`

- [ ] **Step 1: Verify no encoder method**

The file declares no `ToByteArray`. All properties are `{ get; private set; }`. The `Read` override is decoder-only.

- [ ] **Step 2: No edit; advance**

---

### Task 9: Audit `Formats/FlacResidual.cs` — strip the commented-out encoder

**Files:**
- Modify: `AudioVideoLib/Formats/FlacResidual.cs`

- [ ] **Step 1: Remove the commented-out `ToByteArray`**

Delete lines 30-40:

```csharp
    ////public byte[] ToByteArray()
    ////{
    ////    using (StreamBuffer sb = new StreamBuffer())
    ////    {
    ////        sb.WriteByte((byte)_values);
    ////        foreach (byte[] data in RicePartitions.Select(r => r.ToByteArrray()))
    ////            sb.Write(data);

    ////        return sb.ToByteArray();
    ////    }
    ////}
```

- [ ] **Step 2: Property surface**

`CodingMethod`, `PartitionOrder` are computed; `RicePartitions` is `{ get; private set; }` and assigned only inside `Read`. Read-only.

---

### Task 10: Audit `Formats/FlacRicePartition.cs` — strip the commented-out encoder

**Files:**
- Modify: `AudioVideoLib/Formats/FlacRicePartition.cs`

- [ ] **Step 1: Remove the commented-out `ToByteArrray` (note: typo in the original)**

Delete lines 52-68:

```csharp
    //public byte[] ToByteArrray()
    //{
    //    using (StreamBuffer sb = new StreamBuffer())
    //    {
    //        sb.WriteBigEndianInt32(_riceParameter);
    //        int riceParameter = _riceParameter & ((_codingMethod == FlacResidualCodingMethod.PartitionedRice) ? 0x1F : 0xF);
    //        if ((riceParameter < 0xF) || ((_codingMethod == FlacResidualCodingMethod.PartitionedRice2) && (riceParameter < 0x1F)))
    //        {
    //            for ()
    //        }
    //        else
    //        {

    //        }
    //        return sb.ToByteArray();
    //    }
    //}
```

- [ ] **Step 2: Property surface**

`Samples`, `EncodingResidual`, `Residuals` all `{ get; private set; }`. Read-only.

---

### Task 11: Audit `Formats/FlacSubFrameHeader.cs` — read-only confirmed, no change

**Files:**
- Modify (audit only): `AudioVideoLib/Formats/FlacSubFrameHeader.cs`

- [ ] **Step 1: Verify no encoder method**

The file declares only the `Read`-side `ReadHeader` and properties (`Type`, `SampleSize`, `WastedBits`, `Header`). All `{ get; private set; }`. No `ToByteArray`.

- [ ] **Step 2: No edit; advance**

---

### Task 12: Commit milestone — audio-frame format classes are encoder-free and read-only

- [ ] **Step 1: Build to confirm `Formats/Flac*.cs` files all compile**

`AudioVideoLib.csproj` will still fail because `IO/FlacStream.cs:180` references `frame.ToByteArray()`. That is expected; the next tasks fix it. But isolate the format-side build:

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug 2>&1 | grep -E "Formats[\\/]Flac.*\.cs" || echo "No format-side errors"
```

Expected: "No format-side errors".

- [ ] **Step 2: Stage and commit**

```bash
git add AudioVideoLib/Formats/FlacFrame.cs \
        AudioVideoLib/Formats/FlacSubFrame.cs \
        AudioVideoLib/Formats/FlacConstantSubFrame.cs \
        AudioVideoLib/Formats/FlacFixedSubFrame.cs \
        AudioVideoLib/Formats/FlacResidual.cs \
        AudioVideoLib/Formats/FlacRicePartition.cs
git commit -m "refactor(flac): drop audio-frame encoder paths; properties are read-only

Removes the live FlacFrame.ToByteArray, FlacSubFrame.ToByteArray, and
FlacConstantSubFrame override; drops the already-commented-out encoder
stubs in FlacFixedSubFrame, FlacResidual, FlacRicePartition. All
audio-frame properties remain { get; private set; }.

Adds FlacFrame.Length (= EndOffset - StartOffset) used by the upcoming
byte-passthrough WriteTo.

Per specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md §5.1.
Metadata-block encoders untouched — tags still round-trip via that path."
```

The commit knowingly leaves the build broken (`IO/FlacStream.cs` still calls the removed methods); the next task fixes it.

---

### Task 13: Add the `_source` field to `IO/FlacStream.cs`

**Files:**
- Modify: `AudioVideoLib/IO/FlacStream.cs`

- [ ] **Step 1: Locate the existing field block**

`FlacStream.cs` lines 13-19 declare:

```csharp
public sealed partial class FlacStream : IMediaContainer
{
    private const string Identifier = "fLaC";

    private readonly List<FlacFrame> _frames = [];

    private readonly List<FlacMetadataBlock> _metadataBlocks = [];
```

- [ ] **Step 2: Add the source-reader field**

Insert below the existing private fields (after line 19), before the `////---` separator at line 21:

```csharp
    /// <summary>
    /// Live source reader populated by <see cref="ReadStream"/>; consumed by
    /// <see cref="WriteTo"/> for byte-passthrough of audio frames. Disposed by
    /// <see cref="Dispose"/>. <c>null</c> until <see cref="ReadStream"/> succeeds.
    /// </summary>
    private ISourceReader? _source;
```

- [ ] **Step 3: Build**

The file should compile (no consumers yet). Other errors elsewhere persist.

---

### Task 14: Populate `_source` inside `ReadStream`

**Files:**
- Modify: `AudioVideoLib/IO/FlacStream.cs`

- [ ] **Step 1: Locate `ReadStream`**

Lines 97-150 in the current file. The body opens with the null-check and identifier check.

- [ ] **Step 2: Insert source-capture immediately after the identifier check passes**

Find the line:

```csharp
        // Check 'fLaC' identifier.
        var identifier = sb.ReadString(Identifier.Length);
        if (!string.Equals(identifier, Identifier, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
```

Immediately after this block (before `long spacing = 0;`), add:

```csharp
        // Capture the live source for byte-passthrough WriteTo. We rewind the StreamBuffer
        // wrapper to the FLAC start before handing the underlying stream to StreamSourceReader,
        // so the reader's offset 0 == start of 'fLaC' magic.
        var flacStart = sb.Position - Identifier.Length;
        var underlyingStream = stream;
        var sourceBaseline = underlyingStream.Position;
        underlyingStream.Position = flacStart;
        _source?.Dispose();
        _source = new StreamSourceReader(underlyingStream, leaveOpen: true);
        underlyingStream.Position = sourceBaseline;
```

**Note on stream identity.** `ReadStream` accepts `Stream`. If the caller already passed a `StreamBuffer`, `sb` *is* `stream`; otherwise `sb` is a fresh wrapper around `stream`. The `_source` should be over the *underlying* stream (so callers who later mutate it through `MediaContainers` get the same view). `StreamSourceReader` ctor uses `stream.Position` as `_baseOffset`, so we set `Position = flacStart` immediately before the constructor runs, then restore.

- [ ] **Step 3: Verify the existing parse path still works**

Build:

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

`ReadStream` should now compile (`FlacStream.cs:180` still won't, that's the next task).

---

### Task 15: Rewrite `WriteTo` to byte-passthrough audio frames

**Files:**
- Modify: `AudioVideoLib/IO/FlacStream.cs`

- [ ] **Step 1: Replace the existing `WriteTo` body**

Lines 158-183 currently read:

```csharp
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var identifierBytes = System.Text.Encoding.ASCII.GetBytes(Identifier);
        destination.Write(identifierBytes, 0, identifierBytes.Length);

        var streamInfoMetadataBlock = StreamInfoMetadataBlocks.FirstOrDefault();
        if (streamInfoMetadataBlock is not null)
        {
            var bytes = streamInfoMetadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        foreach (var metadataBlock in MetadataBlocks.Where(m => !ReferenceEquals(m, streamInfoMetadataBlock)))
        {
            var bytes = metadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        foreach (var frame in Frames)
        {
            var bytes = frame.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }
    }
```

Replace with:

```csharp
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        // 'fLaC' magic.
        var identifierBytes = System.Text.Encoding.ASCII.GetBytes(Identifier);
        destination.Write(identifierBytes, 0, identifierBytes.Length);

        // Metadata blocks: encoded via their existing per-block ToByteArray() path
        // (these stay encodable — tags live here). STREAMINFO is emitted first per
        // the FLAC spec, then every other block in original order.
        var streamInfoMetadataBlock = StreamInfoMetadataBlocks.FirstOrDefault();
        if (streamInfoMetadataBlock is not null)
        {
            var bytes = streamInfoMetadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        foreach (var metadataBlock in MetadataBlocks.Where(m => !ReferenceEquals(m, streamInfoMetadataBlock)))
        {
            var bytes = metadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        // Audio frames: byte-passthrough from the captured source. No re-encode.
        // frame.StartOffset is absolute against the source-stream start; _source's
        // offset 0 == FLAC start (see ReadStream), so we translate by the FLAC
        // container start to keep this offset model identical to Mp4Stream's.
        var flacStartInSource = StartOffset; // file-absolute start of 'fLaC' magic
        foreach (var frame in Frames)
        {
            var offsetInSource = frame.StartOffset - flacStartInSource;
            _source.CopyTo(offsetInSource, frame.Length, destination);
        }
    }
```

**Offset translation note.** `FlacFrame.StartOffset` is set in `ReadFrame` from `sb.Position`, which is the absolute position in the underlying stream — same coordinate system the `StreamSourceReader` uses. `StreamSourceReader._baseOffset` is whatever `Position` was at construction (we set it to `flacStart` in Task 14). So `_source.CopyTo(0, …)` reads starting at the `fLaC` magic. A frame whose `StartOffset` is `fileOffset` lives at `_source` offset `fileOffset - flacStart`. The expression `frame.StartOffset - StartOffset` in the loop is exactly that subtraction — `FlacStream.StartOffset` is `_frames[0].StartOffset` per the existing partial declaration at line 29, which equals the first frame's file offset, NOT the `fLaC` magic offset.

This is a subtle bug to avoid: `FlacStream.StartOffset` per its current definition is the first **frame**'s offset, not the container start. The metadata blocks live before that. We need the *container* start. Resolve as follows:

- [ ] **Step 2: Capture the container start during `ReadStream`**

In `ReadStream`, just after computing `flacStart` (Task 14, Step 2), also store it on a private field:

```csharp
    private long _containerStart;
```

(Add to the field block from Task 13.)

And in `ReadStream`, set it:

```csharp
        var flacStart = sb.Position - Identifier.Length;
        _containerStart = flacStart;
        var underlyingStream = stream;
        // … rest of Task 14 Step 2 …
```

Then in `WriteTo` change the offset translation:

```csharp
        foreach (var frame in Frames)
        {
            var offsetInSource = frame.StartOffset - _containerStart;
            _source.CopyTo(offsetInSource, frame.Length, destination);
        }
```

- [ ] **Step 3: Build**

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected: clean build. No `FlacFrame.ToByteArray` references remain.

---

### Task 16: Replace the no-op `Dispose` stub from Phase 0 with the real lifecycle

**Files:**
- Modify: `AudioVideoLib/IO/FlacStream.cs`

- [ ] **Step 1: Locate the Phase-0 stub**

Phase 0 Task 5 added a `public void Dispose() { }` no-op into `FlacStream.cs` (the primary file, with a comment referencing this retrofit plan). Find it.

- [ ] **Step 2: Replace with the real implementation**

Change:

```csharp
    /// <summary>
    /// No-op stub. The real <see cref="ISourceReader"/> lifecycle for <see cref="FlacStream"/>
    /// is added in the FLAC retrofit plan (<c>format-pack-flac-retrofit.md</c>) — at that point
    /// this method will dispose the underlying source. Until then, this stub satisfies the
    /// <see cref="IMediaContainer"/> contract.
    /// </summary>
    public void Dispose()
    {
    }
```

to:

```csharp
    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
    /// <see cref="Stream"/>; the caller still owns that. Idempotent.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }
```

- [ ] **Step 3: Build**

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
```

Expected: clean.

---

### Task 17: Run the round-trip identity test (Green)

- [ ] **Step 1: Run the test from Task 1**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~FlacStreamTests.RoundTrip_UnmodifiedInput"
```

Expected: **pass**. The byte-passthrough path now reproduces the input bytes exactly: magic + metadata-block re-encoding (which is loss-less for the metadata blocks the sample contains) + audio-frame splice.

- [ ] **Step 2: If the metadata-block re-encoding is not bit-exact, investigate**

Some FLAC samples contain padding blocks that may not round-trip identically through `FlacPaddingMetadataBlock.ToByteArray()`. If the test fails on metadata-block bytes (compare hex of original vs. output to identify the divergence offset), one of two things is happening:

1. A metadata-block encoder is reordering data (probably `FlacVorbisCommentsMetadataBlock` writing comments in a different order from the original). Use a sample whose metadata blocks round-trip cleanly through the existing encoders.
2. There is a bug in a metadata-block encoder. Out of scope here — file a separate ticket and pick a sample that doesn't trigger it.

Re-run on a clean sample and confirm pass. The whole point of preserving metadata-block encoders unchanged is that they already work on un-mutated input.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib/IO/FlacStream.cs \
        AudioVideoLib/Formats/FlacFrame.cs \
        AudioVideoLib.Tests/IO/FlacStreamTests.cs \
        AudioVideoLib.Tests/TestFiles/flac/sample.flac \
        AudioVideoLib.Tests/TestFiles/flac/PROVENANCE.md
git commit -m "feat(flac): WriteTo is byte-passthrough; ISourceReader lifecycle wired up

FlacStream now holds a private ISourceReader populated at ReadStream time
and consumed by WriteTo via _source.CopyTo(frame.StartOffset, frame.Length, …).
The fLaC magic and metadata blocks are still emitted via their existing
encoder paths (preserved per spec §5.1). Audio frames are byte-identical
to the input on un-mutated round-trip.

Replaces the Phase-0 no-op Dispose stub with the real
_source?.Dispose(); _source = null; Idempotent.

Adds FlacStreamTests.RoundTrip_UnmodifiedInput_ProducesByteIdenticalOutput
covering the new contract on TestFiles/flac/sample.flac."
```

---

### Task 18: Audit `IO/FlacStreamMetadataBlocks.cs` — confirm clean

**Files:**
- Modify (audit only): `AudioVideoLib/IO/FlacStreamMetadataBlocks.cs`

- [ ] **Step 1: Confirm the file still compiles after the audio-frame changes**

The file's surface is purely typed accessors over `_metadataBlocks` (the field declared in the primary `FlacStream.cs`). It does not reference any of the audio-frame classes that were modified.

```
dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug 2>&1 | grep "FlacStreamMetadataBlocks" || echo "Clean"
```

Expected: "Clean".

- [ ] **Step 2: Confirm metadata-block encoders are still callable**

The `WriteTo` body in the primary file calls `streamInfoMetadataBlock.ToByteArray()` and `metadataBlock.ToByteArray()`. Confirm via grep:

```
grep -nE "ToByteArray|WriteTo" AudioVideoLib/Formats/FlacMetadataBlock.cs \
    AudioVideoLib/Formats/FlacVorbisCommentsMetadataBlock.cs \
    AudioVideoLib/Formats/FlacPictureMetadataBlock.cs \
    AudioVideoLib/Formats/FlacStreamInfoMetadataBlock.cs \
    AudioVideoLib/Formats/FlacApplicationMetadataBlock.cs \
    AudioVideoLib/Formats/FlacPaddingMetadataBlock.cs \
    AudioVideoLib/Formats/FlacSeekTableMetadataBlock.cs \
    AudioVideoLib/Formats/FlacCueSheetMetadataBlock.cs
```

Expected: each file shows live `ToByteArray` (or `WriteTo`) methods. No commented-out encoder paths to clean.

- [ ] **Step 3: No edit; advance**

---

### Task 19: Add the detached-source error test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/FlacStreamTests.cs`

- [ ] **Step 1: Append the test**

```csharp
    [Fact]
    public void WriteTo_AfterDispose_ThrowsInvalidOperationException()
    {
        var original = File.ReadAllBytes(SamplePath);
        using var input = new MemoryStream(original, writable: false);

        var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));
        walker.Dispose();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_BeforeRead_ThrowsInvalidOperationException()
    {
        using var walker = new FlacStream();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }
```

- [ ] **Step 2: Run**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~FlacStreamTests.WriteTo"
```

Expected: 3 tests (round-trip + 2 error paths), all green.

---

### Task 20: Add the tag-edit round-trip test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/FlacStreamTests.cs`

This is the high-value retrofit acceptance test (spec §5.3): mutate a Vorbis comment via the metadata-block path, save, re-parse, audio frames byte-identical to the original.

- [ ] **Step 1: Append the test**

```csharp
    [Fact]
    public void TagEdit_RoundTrip_PreservesAudioFrameBytes()
    {
        var original = File.ReadAllBytes(SamplePath);

        // Read the original; capture (offset, length) of every audio frame, plus
        // a hash of each frame's bytes for value-based comparison after re-parse.
        long[] originalFrameLengths;
        byte[][] originalFrameBytes;
        using (var input1 = new MemoryStream(original, writable: false))
        using (var walker1 = new FlacStream())
        {
            Assert.True(walker1.ReadStream(input1));
            originalFrameLengths = walker1.Frames.Select(f => f.Length).ToArray();
            originalFrameBytes = walker1.Frames
                .Select(f =>
                {
                    var buf = new byte[f.Length];
                    input1.Position = f.StartOffset;
                    input1.ReadExactly(buf);
                    return buf;
                })
                .ToArray();
        }

        // Read again, mutate a Vorbis comment, write out.
        byte[] mutated;
        using (var input2 = new MemoryStream(original, writable: false))
        using (var walker2 = new FlacStream())
        {
            Assert.True(walker2.ReadStream(input2));

            var vc = walker2.VorbisCommentsMetadataBlock;
            Assert.NotNull(vc); // sample.flac must contain a Vorbis comment block
            // Pick a tag the sample is known to carry; if 'TITLE' is missing, add it.
            // Either way the saved output must differ from `original` in the metadata
            // region but match in the audio region.
            vc!.Comments["TITLE"] = "RetrofitTest";

            using var output = new MemoryStream();
            walker2.WriteTo(output);
            mutated = output.ToArray();
        }

        // Byte sequence has changed (metadata grew/shrunk).
        Assert.NotEqual(original, mutated);

        // Re-parse the mutated output; audio frames must be value-identical to the originals.
        using (var input3 = new MemoryStream(mutated, writable: false))
        using (var walker3 = new FlacStream())
        {
            Assert.True(walker3.ReadStream(input3));

            var newFrames = walker3.Frames.ToArray();
            Assert.Equal(originalFrameLengths.Length, newFrames.Length);

            for (var i = 0; i < newFrames.Length; i++)
            {
                Assert.Equal(originalFrameLengths[i], newFrames[i].Length);

                var buf = new byte[newFrames[i].Length];
                input3.Position = newFrames[i].StartOffset;
                input3.ReadExactly(buf);

                Assert.Equal(originalFrameBytes[i], buf);
            }

            // And the new TITLE survived.
            Assert.Equal("RetrofitTest", walker3.VorbisCommentsMetadataBlock!.Comments["TITLE"]);
        }
    }
```

(The exact dictionary-style API on `FlacVorbisCommentsMetadataBlock.Comments` may differ — adapt to whatever the existing class exposes. The shape of the test is the load-bearing part.)

- [ ] **Step 2: Run**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~FlacStreamTests"
```

Expected: 4 tests, all green.

- [ ] **Step 3: If the Vorbis-comment API doesn't expose mutation through the property name used above**, find the actual mutation surface (look at `FlacVorbisCommentsMetadataBlock.cs` for setter properties or `Add`/`Set` methods) and fix the test. The test's specification is "modify some metadata, write, re-parse, audio bytes unchanged" — the exact API call is a detail.

---

### Task 21: Audit the existing `VorbisFlacTests.cs`

**Files:**
- Modify (audit; likely no edits): `AudioVideoLib.Tests/VorbisFlacTests.cs`

- [ ] **Step 1: Confirm no test mutates audio-frame state or calls `frame.ToByteArray()`**

```
grep -nE "FlacFrame[^M]|FlacSubFrame|frame\.(ToByteArray|StartOffset|EndOffset|Length)" \
    AudioVideoLib.Tests/VorbisFlacTests.cs
```

Expected: empty. (The file's tests are scoped to metadata-block parsing only; spec §5.1 leaves those untouched.)

- [ ] **Step 2: Build the test project**

```
dotnet build AudioVideoLib.Tests/AudioVideoLib.Tests.csproj -c Debug
```

Expected: clean.

- [ ] **Step 3: Run the existing tests**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~VorbisFlacTests"
```

Expected: all pre-existing tests still pass — the metadata-block surface is unchanged.

---

### Task 22: Run the full FLAC test suite and full project tests

- [ ] **Step 1: All FLAC tests**

```
dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Flac"
```

Expected: green.

- [ ] **Step 2: Whole library**

```
dotnet test AudioVideoLib.slnx -c Debug
```

Expected: green. If a non-FLAC test in another project references something that depended on a now-removed method (e.g., `FlacFrame.ToByteArray`), that's a real consumer to fix. Most likely there are zero such references; spec §5.3 anticipated this and `FlacFrame.ToByteArray` was already a half-broken encoder.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib.Tests/IO/FlacStreamTests.cs
git commit -m "test(flac): tag-edit and detached-source contract tests for FlacStream

- WriteTo_AfterDispose throws InvalidOperationException with the documented
  source-stream-detached message.
- WriteTo_BeforeRead throws the same.
- TagEdit_RoundTrip mutates a Vorbis comment via the metadata-block path,
  saves, re-parses, and asserts audio-frame bytes are value-identical to
  the originals (the spec §5.3 acceptance test for the retrofit)."
```

---

### Task 23: Final self-review pass

- [ ] **Step 1: Confirm encoder removal is complete**

```
grep -rnE "ToByteArray" AudioVideoLib/Formats/Flac*.cs
```

Expected matches: only metadata-block files (`FlacMetadataBlock.cs`, `FlacStreamInfoMetadataBlock.cs`, `FlacVorbisCommentsMetadataBlock.cs`, `FlacPictureMetadataBlock.cs`, `FlacApplicationMetadataBlock.cs`, `FlacPaddingMetadataBlock.cs`, `FlacSeekTableMetadataBlock.cs`, `FlacCueSheetMetadataBlock.cs`). Audio-frame files (`FlacFrame.cs`, `FlacSubFrame.cs`, `FlacConstantSubFrame.cs`, `FlacVerbatimSubFrame.cs`, `FlacFixedSubFrame.cs`, `FlacLinearPredictorSubFrame.cs`, `FlacResidual.cs`, `FlacRicePartition.cs`) should produce zero matches.

- [ ] **Step 2: Confirm property-setter audit is complete**

```
grep -rnE "public .*\{ get; set;" AudioVideoLib/Formats/Flac*.cs
```

Expected matches: only metadata-block files (tags must be settable). The audio-frame files should produce zero matches (everything `private set;` or computed).

- [ ] **Step 3: Confirm `_source` lifecycle**

```
grep -nE "_source" AudioVideoLib/IO/FlacStream.cs
```

Expected lines:
- declaration (one)
- `_source?.Dispose(); _source = null;` in `Dispose` (two refs on one line)
- `_source?.Dispose(); _source = new StreamSourceReader(…);` in `ReadStream` (two refs)
- `if (_source is null) { throw … }` in `WriteTo` (one)
- `_source.CopyTo(…)` in `WriteTo` loop (one)

That's 7 occurrences total.

- [ ] **Step 4: Confirm exception message**

```
grep -nE "Source stream was detached" AudioVideoLib/IO/FlacStream.cs
```

Expected: exactly one match, with the message exactly matching the spec §3.1 text:

```
Source stream was detached or never read. WriteTo requires a live source.
```

- [ ] **Step 5: Confirm scope discipline — no Phase-2 work crept in**

```
git diff master --stat -- AudioVideoLib/IO/MediaContainers.cs \
    _doc_snippets/Program.cs \
    docs/getting-started.md \
    docs/container-formats.md \
    docs/release-notes.md \
    docs/container-formats/flac.md \
    src/TestFiles.txt
```

Expected: all unchanged (zero diff).

- [ ] **Step 6: Final test sweep**

```
dotnet build AudioVideoLib.slnx -c Release
dotnet test AudioVideoLib.slnx -c Release
```

Expected: green.

- [ ] **Step 7: Final review commit if any nits surface**

If steps 1-6 surface anything that needs a tweak, commit it as `chore(flac): retrofit self-review nits`. Otherwise the worktree is ready for merge.

---

## Acceptance for the FlacStream retrofit (echoes spec §5.3 + §7.3)

- All audio-frame format classes (`FlacFrame`, `FlacFrameHeader`, `FlacSubFrame` and the four variants, `FlacSubFrameHeader`, `FlacResidual`, `FlacRicePartition`) are encoder-free; their public properties are read-only.
- `FlacFrame` exposes `Length` (= `EndOffset - StartOffset`) for byte-passthrough.
- `IO/FlacStream.cs` holds `private ISourceReader? _source`, populated in `ReadStream`.
- `IO/FlacStream.cs` `WriteTo` emits `fLaC` magic + metadata blocks (via existing encoders) + per-frame `_source.CopyTo(offset, length, destination)` — no audio re-encoding.
- `IO/FlacStream.cs` `Dispose` releases `_source` and is idempotent.
- `IO/FlacStreamMetadataBlocks.cs` is unchanged.
- All metadata-block files are unchanged (tags still mutate & encode through the existing path).
- `FlacStreamTests` covers: round-trip identity (4 tests including detached-source error paths and tag-edit round-trip preserving audio bytes).
- Existing `VorbisFlacTests` still passes unmodified.
- Whole `dotnet build -c Release` and `dotnet test` green.
- No Phase-2 surface (`MediaContainers.cs`, doc snippets, docs index, `TestFiles.txt`) touched.
