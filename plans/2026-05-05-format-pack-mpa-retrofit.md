# Format pack — MpaStream retrofit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retrofit `MpaStream` onto the offset-based byte-passthrough model. Drop encoder code paths from audio-frame classes (`MpaFrame`, `MpaFrameHeader`, `MpaFrameData`); make their properties read-only and add `StartOffset`/`Length`. Lookup-table classes keep their decoder paths; their encoder-side construction paths drop. VBR/Xing/VBRI/LAME models stay unchanged — they're already read-only on the public surface. `MpaStream.WriteTo` becomes per-frame `_source.CopyTo(frame.StartOffset, frame.Length, destination)`.

**Architecture:** `MpaStream` adopts the `Mp4Stream` pattern: hold a `private ISourceReader? _source` populated from `ReadStream`, dispose it in `Dispose()`, and emit each frame in `WriteTo` by copying its byte range from the live source rather than rebuilding it from the parsed model. `MpaFrame` stays as a parsed inspection model but loses its `ToByteArray()` encoder path; its existing `StartOffset` / `EndOffset` are joined by a public `Length` so callers can describe the frame range without subtracting offsets. `VbrHeader`, `XingHeader`, `VbriHeader`, and `LameTag` are untouched per spec §5.2 — they're already read-only on the public surface and continue to read raw bytes from `MpaFrame.AudioData`, which remains exposed as a read-only inspection field.

**Tech Stack:** C# 13 / .NET 10, xUnit.

**Worktree:** `feat/mpa-retrofit` (per spec §8 Phase 1).

---

## Files this plan modifies (per spec §5.2 / §8 A6)

- `Formats/MpaFrame.cs`, `Formats/MpaFrameHeader.cs`, `Formats/MpaFrameData.cs`. Drop encoder paths. Make properties read-only on the public surface (most already are; verify). `MpaFrame` gains a public `Length` (`StartOffset` already present).
- `Formats/MpaSubbandQuantization.cs`, `Formats/MpaBitAllocation.cs`, `Formats/MpaFrameTables.cs`. Keep decoder lookup tables. Drop any encoder-side construction paths if present.
- `Formats/MpaAudioVersion.cs`, `Formats/MpaChannelMode.cs`, `Formats/MpaFrameEmphasis.cs`, `Formats/MpaFrameLayerVersion.cs` — confirmed to be plain enums, **NO CHANGE**.
- `Formats/VbrHeader.cs`, `Formats/VbrHeaderType.cs`, `Formats/VbriHeader.cs`, `Formats/XingHeader.cs`, `Formats/XingHeaderFlags.cs`, `Formats/LameTag.cs` — **NO CHANGE** per spec §5.2 (already read-only on the public surface; `LameTag.ToByteArray` is in scope of the Phase-2 read-side surface but not touched here).
- `IO/MpaStream.cs` — replace the no-op `Dispose()` stub from Phase 0 with the real one (`_source?.Dispose(); _source = null;`); add `private ISourceReader? _source`; update `ReadStream` to populate `_source`; rewrite `WriteTo` to passthrough using `_source.CopyTo(...)`.
- `AudioVideoLib.Tests/IO/MpaStreamTests.cs` — **new file**. Repository today has no `MpaStreamTests.cs`; this plan adds one. Existing `AudioVideoLib.Tests/MpaFrameHeaderTests.cs` is audited and the single `ToByteArrayRoundTripsHeaderAndAudioData` test is rewritten to use the new offset/length surface.

> **Path note.** Existing tests live at the test project root (`AudioVideoLib.Tests/MpaFrameHeaderTests.cs`), not under an `IO/` subfolder. The new file is created at `AudioVideoLib.Tests/IO/MpaStreamTests.cs` to match the spec §8 path convention; the test project's MSBuild glob picks up both locations.

Creates only one new file (`AudioVideoLib.Tests/IO/MpaStreamTests.cs`). Phase 1 plans don't touch `MediaContainers.cs`, `_doc_snippets/Program.cs`, or any of the index/release-notes docs — that's Phase 2.

---

## Pre-flight assumptions

- Phase 0 (`plans/2026-05-05-format-pack-phase0-foundation.md`) has been merged to master. `IMediaContainer` extends `IDisposable`. `MpaStream.Dispose()` exists as a no-op stub introduced in Phase 0 Task 6. `Mp4Stream.WriteTo` already throws on detached source.
- The canonical exception message — used verbatim in `WriteTo`'s null-source check and in test assertions below — is:

  ```
  Source stream was detached or never read. WriteTo requires a live source.
  ```

- The existing `MpaFrameHeaderTests` (~26 tests) all pass against the current code. The retrofit must keep them green except for the one test (`ToByteArrayRoundTripsHeaderAndAudioData`) that's rewritten.

---

## Tasks

### Task 1: Add the round-trip identity test (TDD red)

**Files:**
- Create: `AudioVideoLib.Tests/IO/MpaStreamTests.cs`

- [ ] **Step 1: Create the test file with the round-trip identity case as the first test.**

  The repo has no checked-in MP3 sample. Synthesise a minimal stream of three valid MPEG-1 Layer III frames inline using the same header pattern that `MpaFrameHeaderTests.Mp3Mpeg1Layer3128At44100Stereo` uses.

  ```csharp
  namespace AudioVideoLib.Tests.IO;

  using System;
  using System.IO;

  using AudioVideoLib.Formats;
  using AudioVideoLib.IO;

  using Xunit;

  public sealed class MpaStreamTests
  {
      private const string DetachedSourceMessage =
          "Source stream was detached or never read. WriteTo requires a live source.";

      // MPEG-1 Layer III, 128 kbps, 44.1 kHz, stereo, no padding, no CRC.
      // FrameLength = 144 * 128000 / 44100 = 417 bytes per frame.
      private static readonly byte[] CanonicalHeader = [0xFF, 0xFB, 0x90, 0x00];
      private const int CanonicalFrameLength = 417;

      private static byte[] BuildThreeFrameStream()
      {
          var bytes = new byte[CanonicalFrameLength * 3];
          for (var i = 0; i < 3; i++)
          {
              var off = i * CanonicalFrameLength;
              Buffer.BlockCopy(CanonicalHeader, 0, bytes, off, 4);
              // Distinct payload bytes per frame so a swap would be visible.
              for (var j = 4; j < CanonicalFrameLength; j++)
              {
                  bytes[off + j] = (byte)((i * 31) + (j & 0xFF));
              }
          }

          return bytes;
      }

      [Fact]
      public void WriteTo_RoundTripsBytesIdentically()
      {
          var original = BuildThreeFrameStream();

          using var input = new MemoryStream(original);
          using var walker = new MpaStream();
          Assert.True(walker.ReadStream(input));

          using var output = new MemoryStream();
          walker.WriteTo(output);

          Assert.Equal(original, output.ToArray());
      }
  }
  ```

- [ ] **Step 2: Run the test; expect it to fail.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests.WriteTo_RoundTripsBytesIdentically" -v normal
  ```

  Expected outcome (one of):

  - **Pass** — current encoder happens to produce identical bytes for a frame with no CRC and a freshly-parsed audio body. In this case the retrofit must preserve the property; the test stays green throughout.
  - **Fail** — current `MpaFrame.ToByteArray()` re-emits the parsed `_header` (4 bytes) plus `AudioData` (the rest of the frame). For an unedited input that should match — but if the round-trip diverges (e.g., truncation at the trailing partial frame, or audio-data reslicing), the failure shows what we have to fix.

  Whichever outcome the current code produces, **after the retrofit lands the test must pass.** Do not skip or weaken the assertion.

- [ ] **Step 3: Commit the failing (or passing) test.**

  ```bash
  git add AudioVideoLib.Tests/IO/MpaStreamTests.cs
  git commit -m "test(mpa): add round-trip identity test for MpaStream.WriteTo"
  ```

---

### Task 2: Audit `MpaFrameHeader.cs` — confirm read-only properties, drop nothing

**Files:**
- Read: `AudioVideoLib/Formats/MpaFrameHeader.cs`

- [ ] **Step 1: Verify every public property is `{ get; }` or `{ get; private set; }`.**

  Grep: every public property in the file already reads as `=> ...` (computed) or `{ get; private set; }`. There are no public setters and no `Write*` / `Encode*` methods. No change needed in this file.

- [ ] **Step 2: Document the audit result in the commit summary** by leaving this file untouched. Move on.

---

### Task 3: Audit `MpaFrameData.cs` — confirm `AudioData` stays exposed, no encoder paths to drop

**Files:**
- Read: `AudioVideoLib/Formats/MpaFrameData.cs`

- [ ] **Step 1: Confirm the file's content is exactly the public `AudioData` byte[] property (`{ get; private set; }`) plus `AudioDataLength`.**

  No public setters, no `Write*`/`Encode*`/`ToByteArray` methods.

- [ ] **Step 2: Spec §5.2 says properties become read-only.** They already are on the public surface (`{ get; private set; }`). `AudioData` itself is a `byte[]` reference — its bytes are mutable through indexed access by anyone holding the array. We accept that risk: deeply locking down `AudioData` would break `XingHeader.FindHeader` and `VbriHeader.FindHeader`, which are explicitly **NO CHANGE** per spec §5.2. The "read-only on the public surface" mandate is satisfied by the absence of public setters.

  No file edit. Move on.

---

### Task 4: Add `Length` to `MpaFrame`

**Files:**
- Modify: `AudioVideoLib/Formats/MpaFrame.cs`

- [ ] **Step 1: Add the `Length` property next to `StartOffset` / `EndOffset`.**

  BEFORE (lines 21-25):

  ```csharp
  /// <inheritdoc/>
  public long StartOffset { get; private set; }

  /// <inheritdoc/>
  public long EndOffset { get; private set; }
  ```

  AFTER:

  ```csharp
  /// <inheritdoc/>
  public long StartOffset { get; private set; }

  /// <inheritdoc/>
  public long EndOffset { get; private set; }

  /// <summary>
  /// Gets the total length of this frame in the source stream, in bytes
  /// (header + audio data + optional CRC). Equivalent to
  /// <see cref="EndOffset"/> - <see cref="StartOffset"/>; exposed as a property
  /// so callers describing a byte-passthrough write don't have to subtract.
  /// </summary>
  public long Length => EndOffset - StartOffset;
  ```

- [ ] **Step 2: Build to verify.**

  ```bash
  dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
  ```

  Expected: clean build.

---

### Task 5: Drop `MpaFrame.ToByteArray()`

**Files:**
- Modify: `AudioVideoLib/Formats/MpaFrame.cs`

- [ ] **Step 1: Remove the encoder method.**

  BEFORE (lines 177-187):

  ```csharp
  /// <summary>
  /// Returns the frame in a byte array.
  /// </summary>
  /// <returns>The frame in a byte array.</returns>
  public byte[] ToByteArray()
  {
      var buffer = new StreamBuffer();
      buffer.Write(_header);
      buffer.Write(AudioData);
      return buffer.ToByteArray();
  }
  ```

  AFTER: delete the whole block. The public surface no longer exposes per-frame encoding.

- [ ] **Step 2: Build to verify the only call sites that break are the two we're rewriting (`MpaStream.WriteTo` and the existing `MpaFrameHeaderTests.ToByteArrayRoundTripsHeaderAndAudioData`).**

  ```bash
  dotnet build AudioVideoLib.slnx -c Debug
  ```

  Expected errors (confirm exactly these and only these):
  - `MpaStream.cs(223): error CS1061: 'MpaFrame' does not contain a definition for 'ToByteArray'`
  - `MpaFrameHeaderTests.cs(457): error CS1061: 'MpaFrame' does not contain a definition for 'ToByteArray'`

  If any other site fails, stop and investigate before proceeding.

---

### Task 6: Audit `MpaFrame.cs` — keep `CalculateCrc`, `ReadFrame`, drop nothing else

**Files:**
- Read: `AudioVideoLib/Formats/MpaFrame.cs`

- [ ] **Step 1: After Task 5 the file contains: the parameterless private constructor, `StartOffset` / `EndOffset` / `Length`, `ReadFrame(Stream)` factory, `CalculateCrc()` (decoder, keeps), `Crc16` static helper, private `ReadFrame(StreamBuffer)` and `ParseHeader`, private `ReadBits` helper.**

  All of these are inspection / decoder paths. No further drops. Move on.

---

### Task 7: Audit lookup-table files (`MpaFrameTables.cs`, `MpaSubbandQuantization.cs`, `MpaBitAllocation.cs`)

**Files:**
- Read: `AudioVideoLib/Formats/MpaFrameTables.cs`
- Read: `AudioVideoLib/Formats/MpaSubbandQuantization.cs`
- Read: `AudioVideoLib/Formats/MpaBitAllocation.cs`

- [ ] **Step 1: Confirm `MpaFrameTables.cs` only contains static `private static readonly` arrays inside the `MpaFrame` partial.** These are decoder lookups (sampling rates, bitrates, frame-size samples, slot sizes, side-info sizes, allowed bitrate/channel-mode matrix). No encoder construction paths; no edit.

- [ ] **Step 2: Confirm `MpaSubbandQuantization.cs` declares only the private nested class `SubbandQuantization` with `SubbandLimit` and `Offsets` properties.** Both have public setters today (`{ get; set; }`) but the nested class is itself **private** — nothing outside `MpaFrame` can see those setters, so they don't violate the public-surface read-only rule.

  Decision: tighten the inner setters to `{ get; init; }` for hygiene, since the decoder lookup tables are populated once at static-construction time and never mutated.

  BEFORE (`MpaSubbandQuantization.cs`):

  ```csharp
  public int SubbandLimit { get; set; }

  public int[] Offsets { get; set; } = new int[30];
  ```

  AFTER:

  ```csharp
  public int SubbandLimit { get; init; }

  public int[] Offsets { get; init; } = new int[30];
  ```

  Build to verify the existing `MpaFrameTables.cs` initializer (which uses object-initializer syntax `new SubbandQuantization { SubbandLimit = ..., Offsets = ... }`) compiles against `init`-only setters.

  ```bash
  dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
  ```

  Expected: clean build (object-initializer syntax is compatible with `init`).

- [ ] **Step 3: Apply the same `init`-only tightening to `MpaBitAllocation.cs`.**

  BEFORE:

  ```csharp
  public short BitsAllocated { get; set; }

  public short Offset { get; set; }
  ```

  AFTER:

  ```csharp
  public short BitsAllocated { get; init; }

  public short Offset { get; init; }
  ```

  Build to verify.

---

### Task 8: Audit MPA enum files — confirm no edits

**Files:**
- Read: `AudioVideoLib/Formats/MpaAudioVersion.cs`
- Read: `AudioVideoLib/Formats/MpaChannelMode.cs`
- Read: `AudioVideoLib/Formats/MpaFrameEmphasis.cs`
- Read: `AudioVideoLib/Formats/MpaFrameLayerVersion.cs`

- [ ] **Step 1: Confirm each is a plain `public enum` with no helper methods, no `ToByteArray` / `Parse` extensions inside the file.** The four files inspected — `MpaAudioVersion`, `MpaChannelMode`, `MpaFrameEmphasis`, `MpaFrameLayerVersion` — are pure enums. No edit.

---

### Task 9: Audit VBR / Xing / VBRI / LAME files — explicit NO CHANGE

**Files:**
- Read (no edit): `AudioVideoLib/Formats/VbrHeader.cs`, `VbrHeaderType.cs`, `VbriHeader.cs`, `XingHeader.cs`, `XingHeaderFlags.cs`, `LameTag.cs`

- [ ] **Step 1: Spec §5.2 says these are unchanged.** Inspection confirms:

  - `VbrHeader` — `protected set;` on a few properties and `FrameCount`/`FileSize` are `public set` for callers that synthesize headers. Public setters here are part of the existing surface and are explicitly preserved by spec.
  - `XingHeader`, `VbriHeader` — derive from `VbrHeader`. Already read-only on the public surface aside from the inherited setters.
  - `LameTag` — every public property is `{ get; private set; }`. Has its own `ToByteArray()` (line 636 of `LameTag.cs`). Spec §5.2 says **no change** to LAME, so we leave `ToByteArray()` in place. (It is not called by `MpaStream.WriteTo` after the retrofit; it remains as a write-side helper for callers that want to emit a LAME tag standalone.)
  - `XingHeaderFlags` — `[Flags]` enum. No change.

- [ ] **Step 2: Do not touch any of these six files.** This is a load-bearing audit: any retrofit edit here would violate the spec contract A6 ↔ A5/A1-A4 boundary.

---

### Task 10: Add `_source` field and update `ReadStream` in `MpaStream`

**Files:**
- Modify: `AudioVideoLib/IO/MpaStream.cs`

- [ ] **Step 1: Add the `_source` field next to `_frames`.**

  BEFORE (lines 13-16):

  ```csharp
  public sealed class MpaStream : IMediaContainer
  {
      private readonly List<MpaFrame> _frames = [];
  ```

  AFTER:

  ```csharp
  public sealed class MpaStream : IMediaContainer
  {
      private readonly List<MpaFrame> _frames = [];
      private ISourceReader? _source;
  ```

  Verify `using AudioVideoLib.IO;` is already present at the top (it is — line 6 was inferred from the `using AudioVideoLib.Formats;` import; if the IO namespace import is missing, add `using AudioVideoLib.IO;` — though `MpaStream` is itself in `AudioVideoLib.IO` so the using is implicit).

- [ ] **Step 2: Populate `_source` in `ReadStream`.**

  BEFORE (lines 166-169):

  ```csharp
  public bool ReadStream(Stream stream)
  {
      ArgumentNullException.ThrowIfNull(stream);

      var streamLength = stream.Length;
  ```

  AFTER:

  ```csharp
  public bool ReadStream(Stream stream)
  {
      ArgumentNullException.ThrowIfNull(stream);

      _source?.Dispose();
      _source = new StreamSourceReader(stream, leaveOpen: true);

      var streamLength = stream.Length;
  ```

  The rest of `ReadStream` is unchanged; it still walks the live `Stream` to discover frames. The `_source` reference is captured here so `WriteTo` can splice from the same backing stream later.

- [ ] **Step 3: Build.**

  ```bash
  dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug
  ```

  Expected: builds (the encoder-method drop in Task 5 still leaves `MpaStream.WriteTo` referencing `frame.ToByteArray()`, so the project itself fails until Task 11. If the build still fails because of `ToByteArray` references, that's expected — proceed.)

---

### Task 11: Replace `MpaStream.Dispose()` stub with the real one

**Files:**
- Modify: `AudioVideoLib/IO/MpaStream.cs`

- [ ] **Step 1: Locate the no-op `Dispose()` introduced in Phase 0 Task 6.**

  Phase 0 added this at the bottom of `MpaStream.cs`:

  ```csharp
  /// <summary>
  /// No-op stub. The real ISourceReader lifecycle for MpaStream
  /// is added in the MPA retrofit plan (format-pack-mpa-retrofit.md) — at that point
  /// this method will dispose the underlying source. Until then, this stub satisfies the
  /// IMediaContainer contract.
  /// </summary>
  public void Dispose()
  {
  }
  ```

- [ ] **Step 2: Replace with the real disposer.**

  AFTER:

  ```csharp
  /// <summary>
  /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
  /// <see cref="Stream"/>; the caller still owns that.
  /// </summary>
  public void Dispose()
  {
      _source?.Dispose();
      _source = null;
  }
  ```

- [ ] **Step 3: Build.** Still failing on the `ToByteArray` reference in `WriteTo` — that's resolved in the next task.

---

### Task 12: Rewrite `MpaStream.WriteTo` as byte-passthrough

**Files:**
- Modify: `AudioVideoLib/IO/MpaStream.cs`

- [ ] **Step 1: Replace the current `WriteTo` body.**

  BEFORE (lines 207-226):

  ```csharp
  /// <inheritdoc />
  /// <remarks>
  /// Each frame is written directly to <paramref name="destination"/> without building an
  /// intermediate byte array. For multi-MB MP3 files this avoids a peak allocation roughly
  /// equal to the total audio size.
  /// </remarks>
  public void WriteTo(Stream destination)
  {
      ArgumentNullException.ThrowIfNull(destination);
      foreach (var frame in Frames)
      {
          if (frame.AudioData is null)
          {
              continue;
          }

          var bytes = frame.ToByteArray();
          destination.Write(bytes, 0, bytes.Length);
      }
  }
  ```

  AFTER:

  ```csharp
  /// <inheritdoc />
  /// <remarks>
  /// Each frame is streamed verbatim from the live source via
  /// <see cref="ISourceReader.CopyTo(long, long, Stream)"/>; no audio re-encoding happens.
  /// The walker requires the source <see cref="Stream"/> passed to
  /// <see cref="ReadStream(Stream)"/> to still be alive — see the source-stream lifetime
  /// contract on <see cref="IMediaContainer"/>.
  /// </remarks>
  /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
  /// <exception cref="InvalidOperationException">
  /// If the underlying source has been disposed or was never populated.
  /// </exception>
  public void WriteTo(Stream destination)
  {
      ArgumentNullException.ThrowIfNull(destination);
      if (_source is null)
      {
          throw new InvalidOperationException(
              "Source stream was detached or never read. WriteTo requires a live source.");
      }

      foreach (var frame in _frames)
      {
          _source.CopyTo(frame.StartOffset, frame.Length, destination);
      }
  }
  ```

  Note: iterating `_frames` directly (not `Frames`) avoids the read-only-list wrapper allocation per call. Either works; `_frames` is preferred to match the `Mp4Stream.WriteTo` pattern of working over the private collection.

- [ ] **Step 2: Build the whole solution.**

  ```bash
  dotnet build AudioVideoLib.slnx -c Debug
  ```

  Expected errors: only `MpaFrameHeaderTests.cs(457): error CS1061: 'MpaFrame' does not contain a definition for 'ToByteArray'`. The library itself builds clean.

- [ ] **Step 3: Run the round-trip identity test from Task 1.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests.WriteTo_RoundTripsBytesIdentically" -v normal
  ```

  Expected: **green**. Three frames in, three byte-identical frames out, copied directly from the source `MemoryStream`.

  If this fails: investigate `frame.StartOffset` (is it relative to the original stream's `Position` at parse time?) and `frame.Length` (does it equal `FrameLength`?). Spec calls for an exact match.

- [ ] **Step 4: Commit progress.**

  ```bash
  git add AudioVideoLib/IO/MpaStream.cs AudioVideoLib/Formats/MpaFrame.cs AudioVideoLib/Formats/MpaSubbandQuantization.cs AudioVideoLib/Formats/MpaBitAllocation.cs
  git commit -m "refactor(mpa): retrofit MpaStream onto byte-passthrough source-reader model

  - MpaFrame: drop ToByteArray(); add Length property.
  - MpaSubbandQuantization, MpaBitAllocation: tighten internal setters to init.
  - MpaStream: hold ISourceReader from ReadStream; WriteTo splices frame
    ranges directly from source via _source.CopyTo. Real Dispose replaces
    the Phase-0 no-op stub.
  - Throws InvalidOperationException with the documented message when
    WriteTo is called without a live source."
  ```

---

### Task 13: Rewrite the broken `ToByteArrayRoundTripsHeaderAndAudioData` test

**Files:**
- Modify: `AudioVideoLib.Tests/MpaFrameHeaderTests.cs`

- [ ] **Step 1: Locate the failing test.**

  BEFORE (lines 450-460):

  ```csharp
  [Fact]
  public void ToByteArrayRoundTripsHeaderAndAudioData()
  {
      var original = BuildFrame(Mp3Mpeg1Layer3128At44100Stereo);
      var frame = ReadFrame(original);

      Assert.NotNull(frame);
      var roundTripped = frame!.ToByteArray();
      Assert.Equal(original.Length, roundTripped.Length);
      Assert.Equal(original, roundTripped);
  }
  ```

- [ ] **Step 2: Replace with an offset/length assertion that proves the parse captured the same byte range.**

  AFTER:

  ```csharp
  [Fact]
  public void ReadFrameCapturesStartOffsetAndLength()
  {
      var original = BuildFrame(Mp3Mpeg1Layer3128At44100Stereo);
      var frame = ReadFrame(original);

      Assert.NotNull(frame);

      // After the byte-passthrough retrofit, MpaFrame no longer encodes itself.
      // It captures (StartOffset, Length); the round-trip identity guarantee
      // moves to MpaStreamTests.WriteTo_RoundTripsBytesIdentically.
      Assert.Equal(0L, frame!.StartOffset);
      Assert.Equal((long)original.Length, frame.Length);
      Assert.Equal(original.Length, frame.FrameLength);
  }
  ```

- [ ] **Step 3: Run the file's tests.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaFrameHeaderTests" -v normal
  ```

  Expected: all 26+ tests green. The renamed test asserts the new contract (offset + length) against the same fixture.

- [ ] **Step 4: Commit.**

  ```bash
  git add AudioVideoLib.Tests/MpaFrameHeaderTests.cs
  git commit -m "test(mpa): rewrite ToByteArray round-trip as offset/length assertion"
  ```

---

### Task 14: Audit other test files for `MpaFrame` / `MpaFrameHeader` / `MpaFrameData` mutation or `ToByteArray` calls

**Files:**
- Read: `AudioVideoLib.Tests/VbrHeaderTests.cs`
- Read: any other test file that imports `AudioVideoLib.Formats` and constructs an `MpaFrame`.

- [ ] **Step 1: Grep the test project for `MpaFrame.ToByteArray`, `frame.ToByteArray()`, or property assignments to MPA frame types.**

  ```bash
  grep -rn "\.ToByteArray()" AudioVideoLib.Tests | grep -i "mpa\|frame"
  grep -rn "MpaFrame\|MpaFrameHeader\|MpaFrameData" AudioVideoLib.Tests
  ```

  Expected matches:
  - `MpaFrameHeaderTests.cs` — already handled in Task 13.
  - `VbrHeaderTests.cs` — uses `MpaFrame.ReadFrame(...)` only (no mutation, no `ToByteArray`). No edit.

  If the grep surfaces any other mutation site, rewrite it to use the inspection-only surface (read property values, do not write them).

- [ ] **Step 2: Run the full VBR test suite to confirm `XingHeader.FindHeader` and `VbriHeader.FindHeader` still work against the retrofitted `MpaFrame`** (they read `firstFrame.AudioData`, which we kept).

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~VbrHeaderTests" -v normal
  ```

  Expected: all green. Confirms the spec §5.2 "VBR/Xing/VBRI/LAME unchanged" rule survived the retrofit on the consumer side as well.

---

### Task 15: Add the detached-source error test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MpaStreamTests.cs`

- [ ] **Step 1: Append a `[Fact]` covering `WriteTo` after `Dispose`.**

  ```csharp
  [Fact]
  public void WriteTo_AfterDispose_ThrowsInvalidOperation()
  {
      var original = BuildThreeFrameStream();

      using var input = new MemoryStream(original);
      var walker = new MpaStream();
      Assert.True(walker.ReadStream(input));
      walker.Dispose();

      var ex = Assert.Throws<InvalidOperationException>(
          () => walker.WriteTo(new MemoryStream()));
      Assert.Equal(DetachedSourceMessage, ex.Message);
  }
  ```

- [ ] **Step 2: Append a `[Fact]` covering `WriteTo` before `ReadStream`.**

  ```csharp
  [Fact]
  public void WriteTo_BeforeReadStream_ThrowsInvalidOperation()
  {
      using var walker = new MpaStream();
      var ex = Assert.Throws<InvalidOperationException>(
          () => walker.WriteTo(new MemoryStream()));
      Assert.Equal(DetachedSourceMessage, ex.Message);
  }
  ```

- [ ] **Step 3: Run.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests" -v normal
  ```

  Expected: 3 green tests (round-trip + two error cases).

---

### Task 16: Add an offset-list inspection test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MpaStreamTests.cs`

- [ ] **Step 1: Append a `[Fact]` that asserts each frame's `StartOffset` and `Length` line up with the synthetic stream's frame layout.**

  ```csharp
  [Fact]
  public void ReadStream_PopulatesFrameStartOffsetsAndLengths()
  {
      var original = BuildThreeFrameStream();

      using var input = new MemoryStream(original);
      using var walker = new MpaStream();
      Assert.True(walker.ReadStream(input));

      var frames = walker.Frames.ToList();
      Assert.Equal(3, frames.Count);
      for (var i = 0; i < 3; i++)
      {
          Assert.Equal(i * CanonicalFrameLength, frames[i].StartOffset);
          Assert.Equal(CanonicalFrameLength, frames[i].Length);
          Assert.Equal((i + 1) * CanonicalFrameLength, frames[i].EndOffset);
      }
  }
  ```

  Add the import `using System.Linq;` at the top of the test file if not already present.

- [ ] **Step 2: Run.** Expected: green.

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests" -v normal
  ```

---

### Task 17: Add a "WriteTo with nonzero start position in source" test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MpaStreamTests.cs`

- [ ] **Step 1: Verify offset semantics when the source `Stream`'s `Position` is non-zero at parse time.**

  This catches the common bug where `StartOffset` is recorded as an absolute file offset but `_source.CopyTo` expects an offset relative to the `_baseOffset` captured by `StreamSourceReader` (or vice-versa).

  ```csharp
  [Fact]
  public void WriteTo_RoundTripsWhenSourceHasLeadingPaddingBytes()
  {
      var audio = BuildThreeFrameStream();
      var leading = new byte[64]; // simulate an ID3v2 tag the outer scanner already skipped past

      var combined = new byte[leading.Length + audio.Length];
      Buffer.BlockCopy(leading, 0, combined, 0, leading.Length);
      Buffer.BlockCopy(audio, 0, combined, leading.Length, audio.Length);

      using var input = new MemoryStream(combined);
      input.Position = leading.Length;

      using var walker = new MpaStream();
      Assert.True(walker.ReadStream(input));

      using var output = new MemoryStream();
      walker.WriteTo(output);

      Assert.Equal(audio, output.ToArray());
  }
  ```

- [ ] **Step 2: Run.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests.WriteTo_RoundTripsWhenSourceHasLeadingPaddingBytes" -v normal
  ```

  Expected: green. If it fails with off-by-`leading.Length` bytes, the bug is in how `MpaFrame.StartOffset` is set during parse vs. how `StreamSourceReader._baseOffset` is captured. Reconcile by verifying that both are consistently relative to the same origin (either both file-absolute or both relative to the source-reader's base).

  Per `StreamSourceReader.cs` line 41-42, `_baseOffset` is captured from `stream.Position` at construction time and `Length = stream.Length - _baseOffset`. `Read(offset, ...)` adds `_baseOffset` internally (verify in `StreamSourceReader.Read`). So `_source.CopyTo(frame.StartOffset, ...)` works correctly **only if** `frame.StartOffset` is relative to the source-reader's base, which equals `stream.Position` at the moment `_source = new StreamSourceReader(stream, ...)` runs. `MpaFrame.StartOffset` is set from `stream.Position` at parse time (per `MpaFrame.ReadFrame(StreamBuffer)` line 239), which is also a file-absolute offset (post-leading-bytes). The two diverge: `frame.StartOffset` is absolute (e.g., 64 + frame_index * 417), but `_source.CopyTo` expects a value relative to base (frame_index * 417).

  **Fix if the test fails:** in `MpaStream.ReadStream`, capture `var sourceStart = stream.Position;` before constructing `_source`, then either (a) shift each `MpaFrame.StartOffset` by `-sourceStart` after reading, or (b) compute `_source.CopyTo(frame.StartOffset - sourceStart, frame.Length, destination)` in `WriteTo`. Option (b) is simpler and doesn't mutate the parsed model. Apply option (b):

  AFTER (in `MpaStream`):

  ```csharp
  private long _sourceBase;

  public bool ReadStream(Stream stream)
  {
      ArgumentNullException.ThrowIfNull(stream);

      _source?.Dispose();
      _sourceBase = stream.Position;
      _source = new StreamSourceReader(stream, leaveOpen: true);

      // … rest unchanged …
  }

  public void WriteTo(Stream destination)
  {
      ArgumentNullException.ThrowIfNull(destination);
      if (_source is null)
      {
          throw new InvalidOperationException(
              "Source stream was detached or never read. WriteTo requires a live source.");
      }

      foreach (var frame in _frames)
      {
          _source.CopyTo(frame.StartOffset - _sourceBase, frame.Length, destination);
      }
  }
  ```

- [ ] **Step 3: Re-run all `MpaStreamTests`.**

  ```bash
  dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpaStreamTests" -v normal
  ```

  Expected: all four tests green.

- [ ] **Step 4: Commit.**

  ```bash
  git add AudioVideoLib.Tests/IO/MpaStreamTests.cs AudioVideoLib/IO/MpaStream.cs
  git commit -m "test(mpa): cover offset/length, detached-source, and leading-padding cases"
  ```

---

### Task 18: Note on "tag-edit round-trip" test — intentionally simpler for MPA

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MpaStreamTests.cs`

- [ ] **Step 1: Add a documentation comment at the top of the test class** explaining why we don't have a "modify a tag, save, re-parse, audio still byte-identical" test (which the FLAC retrofit does have):

  ```csharp
  // ----------------------------------------------------------------------------
  // Tag-edit round-trip note.
  //
  // FLAC has Vorbis comments embedded inside the audio stream's metadata-block
  // chain, so its retrofit ships with a "modify-a-tag-save-reparse-audio-still-
  // identical" test. MPA is different: ID3v1 and ID3v2 are appended/prepended
  // to the audio stream, not embedded inside it. Tag mutation goes through the
  // AudioTags scanner (outside MpaStream's surface), so the relevant retrofit
  // assertion for MPA is just "WriteTo is byte-identical for unedited input"
  // (see WriteTo_RoundTripsBytesIdentically and ..WhenSourceHasLeadingPaddingBytes).
  // ----------------------------------------------------------------------------
  ```

  No new test needed. The leading-padding test in Task 17 already simulates the "ID3v2 was prepended, scanner skipped past it" case from a write-side perspective.

---

### Task 19: Run the full suite and verify zero regressions

- [ ] **Step 1: Full build.**

  ```bash
  dotnet build AudioVideoLib.slnx -c Release
  ```

  Expected: clean.

- [ ] **Step 2: Full test run.**

  ```bash
  dotnet test AudioVideoLib.slnx -c Release
  ```

  Expected: all green. Particular tests to confirm in the output:

  - `MpaStreamTests.*` — 4 tests, green.
  - `MpaFrameHeaderTests.*` — ~26 tests, green (with `ToByteArrayRoundTripsHeaderAndAudioData` renamed to `ReadFrameCapturesStartOffsetAndLength`).
  - `VbrHeaderTests.*` — green (validates the unchanged Xing/VBRI/LAME path against the retrofitted `MpaFrame`).
  - `Phase0ContractTests.*` — green (the no-op `Dispose` test for `MpaStream` continues to pass even though `Dispose` is no longer a no-op; calling `Dispose()` twice on a freshly-constructed `MpaStream` is still safe because `_source` starts null).

- [ ] **Step 3: If anything fails, stop and triage** before moving on. Do not paper over a regression with a test edit.

---

### Task 20: Final commit and worktree exit

- [ ] **Step 1: Confirm working tree is clean except for the planned commits from Tasks 12, 13, 17.**

  ```bash
  git status
  git log --oneline feat/mpa-retrofit ^master
  ```

  Expected log (3 commits):
  ```
  test(mpa): cover offset/length, detached-source, and leading-padding cases
  test(mpa): rewrite ToByteArray round-trip as offset/length assertion
  refactor(mpa): retrofit MpaStream onto byte-passthrough source-reader model
  ```

  Plus the test-first commit from Task 1:
  ```
  test(mpa): add round-trip identity test for MpaStream.WriteTo
  ```

  Total: 4 commits on the worktree branch, all passing.

- [ ] **Step 2: Hand back to orchestrator.** The orchestrator will merge `feat/mpa-retrofit` into the integration branch alongside the other Phase-1 worktrees.

---

## Acceptance criteria for the MPA retrofit

- `MpaFrame` no longer exposes `ToByteArray()`. Public properties remain read-only on the public surface (`{ get; }` or `{ get; private set; }`). `Length` is exposed as a public property equal to `EndOffset - StartOffset`.
- `MpaFrameHeader` and `MpaFrameData` have no public setters; no `Write*` / `Encode*` / `ToByteArray` methods. `MpaFrameData.AudioData` remains exposed (read-only on the public surface) so `XingHeader.FindHeader` and `VbriHeader.FindHeader` continue to work.
- `MpaSubbandQuantization` and `MpaBitAllocation` inner classes use `init`-only setters; their lookup-table population in `MpaFrameTables.cs` still compiles via object-initializer syntax. No encoder construction paths.
- `MpaAudioVersion`, `MpaChannelMode`, `MpaFrameEmphasis`, `MpaFrameLayerVersion` — untouched.
- `VbrHeader`, `VbrHeaderType`, `VbriHeader`, `XingHeader`, `XingHeaderFlags`, `LameTag` — **untouched** per spec §5.2. `LameTag.ToByteArray()` is preserved.
- `MpaStream` holds a `private ISourceReader? _source`. `ReadStream` populates it; `Dispose` releases it (replacing the Phase-0 no-op stub); `WriteTo` throws `InvalidOperationException` with the canonical message when `_source` is null and otherwise streams each frame's byte range from `_source` using `_source.CopyTo`.
- `AudioVideoLib.Tests/IO/MpaStreamTests.cs` exists and contains four tests: round-trip identity, round-trip with leading padding, write-after-dispose error, write-before-read error.
- `AudioVideoLib.Tests/MpaFrameHeaderTests.cs` keeps all its parser tests green; the one `ToByteArray`-based test is rewritten as an offset/length assertion.
- `dotnet build -c Release` and `dotnet test -c Release` both clean.

---

## Self-review

- **Placeholder check.** No `TODO`, `FIXME`, `[fill in]`, or `<placeholder>` markers remain in this plan. Every code block is complete and copy-pasteable.
- **Test name / impl method consistency.** `WriteTo_RoundTripsBytesIdentically`, `WriteTo_RoundTripsWhenSourceHasLeadingPaddingBytes`, `WriteTo_AfterDispose_ThrowsInvalidOperation`, `WriteTo_BeforeReadStream_ThrowsInvalidOperation`, `ReadStream_PopulatesFrameStartOffsetsAndLengths`, `ReadFrameCapturesStartOffsetAndLength` — all describe the actual method and assertion they cover. The renamed test in Task 13 was deliberately not left as `ToByteArrayRoundTripsHeaderAndAudioData` because that name no longer matches what it asserts.
- **Spec §5.2 facts covered.** Every row in the §5.2 table maps to a task in this plan: `MpaFrame`/`MpaFrameHeader`/`MpaFrameData` (Tasks 2-6); lookup tables (Task 7); VBR/Xing/VBRI/LAME unchanged (Task 9, with explicit "do not touch" instruction); `MpaStream.WriteTo` (Task 12); `MpaStream` lifecycle / `Dispose` (Tasks 10-11). The "VBR/Xing/VBRI/LAME unchanged" rule is reinforced in Task 14 (test audit) and called out in the file-list summary.
- **Phase-boundary discipline.** Task 9 explicitly forbids touching the six VBR/LAME files. Task 14 verifies the unchanged consumer code still works. Phase 1 does not touch `MediaContainers.cs`, `_doc_snippets/Program.cs`, or any docs — that's Phase 2. The plan's "Files this plan modifies" list is the authoritative scope.
- **TDD shape.** Task 1 writes the failing (or already-passing) round-trip test before any implementation change. Tasks 2-9 are audits + small property-tightening edits. Tasks 10-12 land the real implementation, with a build-and-test gate after each commit. Tasks 13-18 fold in the test rewrites and add coverage for the offset/error/leading-padding cases. Each task is sized at 2-5 minutes of work.
- **One pre-existing-state risk identified and resolved inline:** Task 17 calls out the offset-base mismatch between `MpaFrame.StartOffset` (absolute) and `StreamSourceReader._baseOffset` (relative). The fix (subtract `_sourceBase` in `WriteTo`) is shown as a contingent edit, gated on the test failing — this is the right TDD shape: don't pre-emptively introduce the field if the test happens to pass without it.
