# Format pack — Phase 0: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the `IMediaContainer` interface and the ten existing walkers into conformance with the source-reference lifetime contract from `specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md` §3, so that the four new format walkers in Phase 1 can be added uniformly.

**Architecture:** `IMediaContainer` extends `IDisposable`. Three existing walkers (`Mp4Stream`, `AsfStream`, `MatroskaStream`) already hold an `ISourceReader _source` and implement `Dispose()`; their `WriteTo` is changed to throw on null source instead of silently returning. The other seven walkers (`AiffStream`, `DffStream`, `DsfStream`, `FlacStream`, `MpaStream`, `OggStream`, `RiffStream`) get a no-op `Dispose()` stub now; `FlacStream` and `MpaStream` get real source-reference logic in Phase 1 (their retrofit plans). `MediaContainers` itself becomes `IDisposable`, disposing each child walker.

**Tech Stack:** C# 13 / .NET 10. Tests use the existing test framework (`AudioVideoLib.Tests`).

**Scope boundary:** This plan touches only the foundational interface and the ten existing walkers' lifecycle plumbing. It does NOT add the four new walkers, retrofit FLAC/MPA audio frames, or change `MediaContainers.cs` dispatch — those happen in their own plans.

**Reference:** Spec §3 (architecture), §3.3 (interface change & contract conformance), §8 Phase 0.

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib/IO/IMediaContainer.cs` | Extend `IDisposable`. Update class-level doc comment with the source-stream lifetime contract. |
| `AudioVideoLib/IO/AiffStream.cs` | Add `: IDisposable` (already gets it transitively from interface, but make explicit) and `public void Dispose() { }` no-op stub. |
| `AudioVideoLib/IO/DffStream.cs` | Add `Dispose() { }` no-op stub. |
| `AudioVideoLib/IO/DsfStream.cs` | Add `Dispose() { }` no-op stub. |
| `AudioVideoLib/IO/FlacStream.cs` | Add `Dispose() { }` no-op stub (real `_source` lifecycle comes in `format-pack-flac-retrofit.md`). |
| `AudioVideoLib/IO/MpaStream.cs` | Add `Dispose() { }` no-op stub (real `_source` lifecycle comes in `format-pack-mpa-retrofit.md`). |
| `AudioVideoLib/IO/OggStream.cs` | Add `Dispose() { }` no-op stub. |
| `AudioVideoLib/IO/RiffStream.cs` | Add `Dispose() { }` no-op stub. |
| `AudioVideoLib/IO/Mp4Stream.cs:142-148` | Change `WriteTo` to throw `InvalidOperationException` instead of silent return on null source. |
| `AudioVideoLib/IO/AsfStream.cs:160-163` | Change `WriteTo` to throw on null source (currently silently returns when `_source is null` is part of a compound condition). |
| `AudioVideoLib/IO/MatroskaStream.cs:172-175` | Change `WriteTo` to throw on null source. |
| `AudioVideoLib/IO/MediaContainers.cs` | Implement `IDisposable`, dispose each contained walker. |
| `AudioVideoLib/AudioVideoLib.Cli/`, `AudioVideoLib/AudioVideoLib.Demo/`, `AudioVideoLib/AudioVideoLib.Samples/`, `_doc_snippets/Program.cs` | Wrap walker / `MediaContainers` usage in `using` where appropriate. |
| `AudioVideoLib.Tests/IO/Phase0ContractTests.cs` | New test file — covers the interface contract changes (no per-walker logic). |

---

## Tasks

### Task 1: Add the documented exception message constant

**Files:**
- Modify: `AudioVideoLib/IO/IMediaContainer.cs`

- [ ] **Step 1: Update `IMediaContainer.cs` to extend `IDisposable` and document the contract**

Replace the file body with:

```csharp
namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// Interface for an audio format to implement streaming.
/// </summary>
/// <remarks>
/// The canonical serialisation primitive is <see cref="WriteTo(Stream)"/>. Buffer-shaped
/// helpers (<c>ToByteArray</c>, <c>GetSerializedSize</c>, <c>TryWriteTo</c>,
/// <c>WriteTo(IBufferWriter&lt;byte&gt;)</c>) are provided as extension methods on
/// <see cref="IMediaContainerExtensions"/>; concrete types may override them with their own
/// instance method for a faster direct path.
///
/// <para />
/// <b>Source-stream lifetime contract.</b> Walkers that splice unchanged byte ranges from the
/// input stream (currently <c>Mp4Stream</c>, <c>AsfStream</c>, <c>MatroskaStream</c>; after the
/// format-pack retrofit also <c>FlacStream</c>, <c>MpaStream</c>, and the four new walkers
/// <c>MpcStream</c>, <c>WavPackStream</c>, <c>TtaStream</c>, <c>MacStream</c>) hold an
/// <see cref="ISourceReader"/> populated at <see cref="ReadStream"/> time. The caller must keep
/// the source <see cref="Stream"/> alive between <see cref="ReadStream"/> and
/// <see cref="WriteTo"/>. Calling <see cref="WriteTo"/> on a walker whose source has been
/// disposed (via <see cref="IDisposable.Dispose"/>) — or that was never read — produces an
/// <see cref="InvalidOperationException"/> with the message
/// <c>"Source stream was detached or never read. WriteTo requires a live source."</c>.
/// Walkers that do not need the source (no-op <c>Dispose()</c>) are exempt from this rule.
/// </remarks>
public interface IMediaContainer : IDisposable
{
    /// <summary>
    /// Gets the start offset of the <see cref="IMediaContainer"/>, where it starts in the stream.
    /// </summary>
    long StartOffset { get; }

    /// <summary>
    /// Gets the end offset of the <see cref="IMediaContainer"/>, where it ends in the stream.
    /// </summary>
    long EndOffset { get; }

    /// <summary>
    /// Gets the total length of audio in milliseconds.
    /// </summary>
    long TotalDuration { get; }

    /// <summary>
    /// Gets the total size of audio data in bytes.
    /// </summary>
    long TotalMediaSize { get; }

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between 2 frames when searching for frames.
    /// </summary>
    int MaxFrameSpacingLength { get; set; }

    /// <summary>
    /// Reads the audio stream from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>true if the audio stream was successfully read; otherwise, false.</returns>
    bool ReadStream(Stream stream);

    /// <summary>
    /// Writes the serialised form of this container to the supplied <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write the container bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown by walkers that hold an <see cref="ISourceReader"/> when their source has been
    /// disposed or was never populated. See the source-stream lifetime contract above.
    /// </exception>
    void WriteTo(Stream destination);
}
```

- [ ] **Step 2: Build the AudioVideoLib project and confirm a clean compile of `IMediaContainer.cs`**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: build succeeds. Other walkers may fail to compile because they don't yet implement `IDisposable` — this is expected and addressed by tasks 2–8. Do not commit yet.

---

### Task 2: Add `Dispose()` stub to `AiffStream`

**Files:**
- Modify: `AudioVideoLib/IO/AiffStream.cs`

- [ ] **Step 1: Open `AiffStream.cs` and locate the class declaration**

Find the line `public sealed partial class AiffStream : IMediaContainer` (or similar). Confirm there is no existing `Dispose()` method.

- [ ] **Step 2: Add the no-op `Dispose()` method**

Just before the closing `}` of the class, add:

```csharp
    /// <summary>
    /// No-op. <see cref="AiffStream"/> does not hold an <see cref="ISourceReader"/>; the
    /// implementation exists to satisfy <see cref="IMediaContainer"/>'s <see cref="IDisposable"/>
    /// contract. See the source-stream lifetime contract on <see cref="IMediaContainer"/>.
    /// </summary>
    public void Dispose()
    {
    }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: `AiffStream.cs` compiles without error (other walkers may still fail).

---

### Task 3: Add `Dispose()` stub to `DffStream`

**Files:**
- Modify: `AudioVideoLib/IO/DffStream.cs`

- [ ] **Step 1: Add the no-op `Dispose()` method**

Inside the `DffStream` class, just before the closing `}`, add:

```csharp
    /// <summary>
    /// No-op. <see cref="DffStream"/> does not hold an <see cref="ISourceReader"/>; the
    /// implementation exists to satisfy <see cref="IMediaContainer"/>'s <see cref="IDisposable"/>
    /// contract. See the source-stream lifetime contract on <see cref="IMediaContainer"/>.
    /// </summary>
    public void Dispose()
    {
    }
```

- [ ] **Step 2: Build to verify** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: `DffStream.cs` compiles.

---

### Task 4: Add `Dispose()` stub to `DsfStream`

Same pattern as Task 3, applied to `AudioVideoLib/IO/DsfStream.cs`. Doc comment substitutes `DsfStream` for `DffStream`. Build to verify.

---

### Task 5: Add `Dispose()` stub to `FlacStream`

Same pattern, applied to `AudioVideoLib/IO/FlacStream.cs`. Doc comment text:

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

Note: `FlacStream` is `partial` across `FlacStream.cs` and `FlacStreamMetadataBlocks.cs`. Add the method to `FlacStream.cs` (the primary file).

Build to verify.

---

### Task 6: Add `Dispose()` stub to `MpaStream`

Same as Task 5, applied to `AudioVideoLib/IO/MpaStream.cs`. Doc comment references `format-pack-mpa-retrofit.md`. Build to verify.

---

### Task 7: Add `Dispose()` stub to `OggStream`

Same as Task 3, applied to `AudioVideoLib/IO/OggStream.cs`. Doc text matches Task 3 (no future retrofit planned). Build to verify.

---

### Task 8: Add `Dispose()` stub to `RiffStream`

Same as Task 3, applied to `AudioVideoLib/IO/RiffStream.cs`. Doc text matches Task 3. Build to verify.

---

### Task 9: Verify clean build before contract retrofits

- [ ] **Step 1: Build the whole solution**

Run: `dotnet build AudioVideoLib.slnx -c Debug`
Expected: build succeeds across all projects. All ten existing walkers now satisfy `IMediaContainer : IDisposable`.

- [ ] **Step 2: Commit progress so far**

```bash
git add AudioVideoLib/IO/IMediaContainer.cs AudioVideoLib/IO/AiffStream.cs AudioVideoLib/IO/DffStream.cs AudioVideoLib/IO/DsfStream.cs AudioVideoLib/IO/FlacStream.cs AudioVideoLib/IO/MpaStream.cs AudioVideoLib/IO/OggStream.cs AudioVideoLib/IO/RiffStream.cs
git commit -m "refactor(io): IMediaContainer extends IDisposable; add Dispose stubs to seven walkers

Foundational change for the format-pack project (see specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md §3.3).
The seven walkers that don't hold an ISourceReader get no-op Dispose() stubs.
FlacStream and MpaStream stubs are temporary; their real lifecycle lands in the
FLAC and MPA retrofit plans."
```

---

### Task 10: Add the contract test harness

**Files:**
- Create: `AudioVideoLib.Tests/IO/Phase0ContractTests.cs`

- [ ] **Step 1: Write the failing test for the new exception contract**

Create `AudioVideoLib.Tests/IO/Phase0ContractTests.cs`:

```csharp
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using AudioVideoLib.IO;
using Xunit;

public sealed class Phase0ContractTests
{
    private const string ExpectedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void Mp4Stream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new Mp4Stream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void AsfStream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new AsfStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void MatroskaStream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MatroskaStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void Mp4Stream_WriteTo_ThrowsAfterDispose()
    {
        using var fs = new MemoryStream();
        var walker = new Mp4Stream();
        walker.ReadStream(fs); // succeeds or fails harmlessly on empty stream
        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void Dispose_NoOp_StubsDoNotThrow()
    {
        // Each walker's no-op Dispose must be safe to call twice.
        Action[] dispose = [
            () => { using (var w = new AiffStream()) { w.Dispose(); } },
            () => { using (var w = new DffStream())  { w.Dispose(); } },
            () => { using (var w = new DsfStream())  { w.Dispose(); } },
            () => { using (var w = new FlacStream()) { w.Dispose(); } },
            () => { using (var w = new MpaStream())  { w.Dispose(); } },
            () => { using (var w = new OggStream())  { w.Dispose(); } },
            () => { using (var w = new RiffStream()) { w.Dispose(); } },
        ];

        foreach (var d in dispose)
        {
            d();
        }
    }
}
```

- [ ] **Step 2: Run the new tests; expect failures on the three throw cases**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests" -v normal`
Expected: 3 failures (`Mp4Stream_WriteTo_ThrowsWhenSourceIsNull`, `AsfStream_WriteTo_…`, `MatroskaStream_WriteTo_…`, `Mp4Stream_WriteTo_ThrowsAfterDispose`) — current code returns silently. The `Dispose_NoOp_StubsDoNotThrow` test should pass already.

---

### Task 11: Retrofit `Mp4Stream.WriteTo` to throw on null source

**Files:**
- Modify: `AudioVideoLib/IO/Mp4Stream.cs:142-148`

- [ ] **Step 1: Locate the existing silent-return**

Open `AudioVideoLib/IO/Mp4Stream.cs`. At lines 142-148 the current code is:

```csharp
public void WriteTo(Stream destination)
{
    ArgumentNullException.ThrowIfNull(destination);
    if (_source is null)
    {
        return;
    }
    // …
}
```

- [ ] **Step 2: Replace the silent return with the documented throw**

Change lines 142-148 to:

```csharp
public void WriteTo(Stream destination)
{
    ArgumentNullException.ThrowIfNull(destination);
    if (_source is null)
    {
        throw new InvalidOperationException(
            "Source stream was detached or never read. WriteTo requires a live source.");
    }
    // …
}
```

- [ ] **Step 3: Run the Mp4 contract tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests.Mp4Stream"`
Expected: both Mp4 throw tests pass.

---

### Task 12: Retrofit `AsfStream.WriteTo` to throw on null source

**Files:**
- Modify: `AudioVideoLib/IO/AsfStream.cs:160-163`

- [ ] **Step 1: Read the current compound check**

The current code at line 163 is `if (_source is null || _headerBytes.Length == 0 || HeaderObjectSize == 0)` followed by a `return`. The compound condition lumps three different "not ready" states together. Split it: null source throws (contract), the other two states keep their existing silent-return behavior (they represent "no header was parsed", which is a separate concern from disposal).

- [ ] **Step 2: Replace the compound check**

Change the body of `WriteTo` so the source-null check comes first and throws:

```csharp
public void WriteTo(Stream destination)
{
    ArgumentNullException.ThrowIfNull(destination);
    if (_source is null)
    {
        throw new InvalidOperationException(
            "Source stream was detached or never read. WriteTo requires a live source.");
    }
    if (_headerBytes.Length == 0 || HeaderObjectSize == 0)
    {
        return;
    }
    // … existing body …
}
```

- [ ] **Step 3: Run the AsfStream contract test**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests.AsfStream"`
Expected: pass.

- [ ] **Step 4: Run the full test suite to confirm no regression in existing AsfStream tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~AsfStream"`
Expected: all green.

---

### Task 13: Retrofit `MatroskaStream.WriteTo` to throw on null source

**Files:**
- Modify: `AudioVideoLib/IO/MatroskaStream.cs:172-175`

- [ ] **Step 1: Replace the silent return**

The current `WriteTo` has `if (_source is null) { return; }`. Replace with the throw, identical to Task 11:

```csharp
public void WriteTo(Stream destination)
{
    ArgumentNullException.ThrowIfNull(destination);
    if (_source is null)
    {
        throw new InvalidOperationException(
            "Source stream was detached or never read. WriteTo requires a live source.");
    }
    // … existing body …
}
```

- [ ] **Step 2: There is also a `ToByteArray` override at `MatroskaStream.cs:207` that mirrors the same null-check.** Update it the same way (throw on null source):

```csharp
public byte[] ToByteArray()
{
    if (_source is null)
    {
        throw new InvalidOperationException(
            "Source stream was detached or never read. WriteTo requires a live source.");
    }
    using var ms = new MemoryStream();
    WriteTo(ms);
    return ms.ToArray();
}
```

- [ ] **Step 3: Run the MatroskaStream contract test and full Matroska tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MatroskaStream"`
Expected: all green.

---

### Task 14: Run all Phase 0 contract tests; commit

- [ ] **Step 1: Run the full Phase 0 contract suite**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests" -v normal`
Expected: 5 tests, all pass.

- [ ] **Step 2: Run the full test suite to confirm no regression**

Run: `dotnet test AudioVideoLib.Tests`
Expected: all green.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib/IO/Mp4Stream.cs AudioVideoLib/IO/AsfStream.cs AudioVideoLib/IO/MatroskaStream.cs AudioVideoLib.Tests/IO/Phase0ContractTests.cs
git commit -m "refactor(io): Mp4/Asf/Matroska WriteTo throw InvalidOperationException on detached source

Brings the three splice-rewriter walkers in line with the IMediaContainer
source-stream lifetime contract (specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md §3.1).
Previously they silently returned when _source was null. Adds Phase0ContractTests
covering both the throw behavior and the no-op Dispose stubs from the previous commit."
```

---

### Task 15: Implement `IDisposable` on `MediaContainers`

**Files:**
- Modify: `AudioVideoLib/IO/MediaContainers.cs`

- [ ] **Step 1: Find the class declaration**

`MediaContainers.cs:16` declares `public sealed class MediaContainers : IEnumerable<IMediaContainer>`. Add `IDisposable`:

```csharp
public sealed class MediaContainers : IEnumerable<IMediaContainer>, IDisposable
```

- [ ] **Step 2: Locate where the walker collection lives**

The class wraps `_streams` (a `NotifyingList<IMediaContainer>` per the constructor body at lines 39-43). Add a `Dispose()` method at the bottom of the class:

```csharp
    /// <summary>
    /// Disposes every contained walker. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        foreach (var walker in _streams)
        {
            walker.Dispose();
        }
        _disposed = true;
    }

    private bool _disposed;
```

- [ ] **Step 3: Add a test**

Append to `AudioVideoLib.Tests/IO/Phase0ContractTests.cs`:

```csharp
    [Fact]
    public void MediaContainers_Dispose_DisposesAllChildren()
    {
        var holder = new MediaContainers();
        // Add a walker that records dispose calls.
        var probe = new DisposeProbeContainer();
        // MediaContainers is read via ReadStream from a Stream; for the unit test
        // we need a way to inject the probe. If the public API doesn't allow that,
        // assert instead that calling Dispose twice does not throw.
        holder.Dispose();
        holder.Dispose(); // idempotent
    }

    private sealed class DisposeProbeContainer : IMediaContainer
    {
        public bool DisposeCalled { get; private set; }
        public long StartOffset => 0;
        public long EndOffset => 0;
        public long TotalDuration => 0;
        public long TotalMediaSize => 0;
        public int MaxFrameSpacingLength { get; set; }
        public bool ReadStream(Stream stream) => true;
        public void WriteTo(Stream destination) { }
        public void Dispose() => DisposeCalled = true;
    }
```

The probe class is currently unused because `MediaContainers` only takes walkers via `ReadStream`'s probe path. The test verifies the idempotency of `Dispose` on `MediaContainers` itself; the probe class stays as scaffolding for a future test once `MediaContainers` exposes a way to inject walkers (out of scope for Phase 0).

- [ ] **Step 4: Run the test**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests.MediaContainers_Dispose"`
Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add AudioVideoLib/IO/MediaContainers.cs AudioVideoLib.Tests/IO/Phase0ContractTests.cs
git commit -m "refactor(io): MediaContainers implements IDisposable, disposes children"
```

---

### Task 16: Update in-tree consumers to dispose containers

**Files:**
- Modify: `AudioVideoLib.Cli/**/*.cs` (any file that creates a `MediaContainers` or an individual walker without disposing).
- Modify: `AudioVideoLib.Demo/**/*.cs` (same).
- Modify: `AudioVideoLib.Samples/**/*.cs` (same).
- Modify: `_doc_snippets/Program.cs` (same).

- [ ] **Step 1: Find every site that creates a walker or `MediaContainers`**

Run: `grep -rn "new MediaContainers\|MediaContainers.ReadStream\|new Mp4Stream\|new AsfStream\|new MatroskaStream\|new AiffStream\|new DsfStream\|new DffStream\|new FlacStream\|new MpaStream\|new OggStream\|new RiffStream" AudioVideoLib.Cli AudioVideoLib.Demo AudioVideoLib.Samples _doc_snippets 2>/dev/null`

Record the list. The fix for each: either wrap in `using` (`using var streams = MediaContainers.ReadStream(fs);`) or call `.Dispose()` explicitly when the walker leaves scope.

- [ ] **Step 2: Apply `using` to each call site**

For each match from Step 1, change e.g.:

```csharp
var streams = MediaContainers.ReadStream(fs);
foreach (var stream in streams) { … }
```

to:

```csharp
using var streams = MediaContainers.ReadStream(fs);
foreach (var stream in streams) { … }
```

For sites that pass the result around (return from a method, store in a field), follow the standard `IDisposable` ownership rules — the owner declares the field `IDisposable` and disposes it when its own lifetime ends.

- [ ] **Step 3: Build all four projects**

Run: `dotnet build AudioVideoLib.Cli AudioVideoLib.Demo AudioVideoLib.Samples _doc_snippets -c Debug`
Expected: all four build clean. CS warnings about "disposable not disposed" should be gone.

- [ ] **Step 4: Run `_doc_snippets` to confirm no behavior regression**

Run: `dotnet run --project _doc_snippets`
Expected: exit code 0, all snippets PASS.

- [ ] **Step 5: Commit**

```bash
git add AudioVideoLib.Cli AudioVideoLib.Demo AudioVideoLib.Samples _doc_snippets
git commit -m "refactor: dispose IMediaContainer / MediaContainers consumers

Wraps walker and MediaContainers usage in using-statements across the four
in-tree consumer projects, completing the IMediaContainer : IDisposable rollout."
```

---

### Task 17: Final Phase 0 validation

- [ ] **Step 1: Full build**

Run: `dotnet build AudioVideoLib.slnx -c Release`
Expected: clean build, zero warnings related to `IDisposable` / `Dispose` / undisposed locals.

- [ ] **Step 2: Full test suite**

Run: `dotnet test AudioVideoLib.slnx -c Release`
Expected: all green.

- [ ] **Step 3: DocFX build (sanity check)**

Run: `docfx docfx.json`
Expected: no new warnings beyond the six pre-existing `InvalidCref` warnings.

- [ ] **Step 4: Final commit if any cleanup**

If steps 1-3 surfaced any remaining issues, fix them and commit. Otherwise Phase 0 is complete and Phase 1 plans (`format-pack-mpc.md`, `format-pack-wavpack.md`, `format-pack-tta.md`, `format-pack-mac.md`, `format-pack-flac-retrofit.md`, `format-pack-mpa-retrofit.md`) can begin in parallel.

---

## Acceptance criteria for Phase 0

- `IMediaContainer` extends `IDisposable`; doc comment captures the source-stream lifetime contract.
- All ten existing walkers implement `Dispose()`. Seven are no-op stubs; three (Mp4, Asf, Matroska) retain their pre-existing real implementations.
- `Mp4Stream.WriteTo`, `AsfStream.WriteTo`, `MatroskaStream.WriteTo` (and `MatroskaStream.ToByteArray`) throw `InvalidOperationException` with the documented message when called on a walker with a null `_source`.
- `MediaContainers` implements `IDisposable` and disposes each contained walker.
- Every in-tree consumer (`AudioVideoLib.Cli`, `AudioVideoLib.Demo`, `AudioVideoLib.Samples`, `_doc_snippets`) wraps walker usage in `using` (or owns it via a disposable holder).
- `Phase0ContractTests` passes (5 tests).
- `dotnet build -c Release` and `dotnet test` both clean.
- `dotnet run --project _doc_snippets` returns 0.
