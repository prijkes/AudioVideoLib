# Format pack — Phase 2 (integration) + Phase 3 (validation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the four new walkers (`MpcStream`, `WavPackStream`, `TtaStream`, `MacStream`) into `MediaContainers` dispatch, add their docs, add their snippets, append to the test-files manifest, and run the final validation gates from the spec §8 Phase 3.

**Architecture:** This plan runs once after all six Phase-1 plans (`format-pack-mpc.md`, `format-pack-wavpack.md`, `format-pack-tta.md`, `format-pack-mac.md`, `format-pack-flac-retrofit.md`, `format-pack-mpa-retrofit.md`) have merged into the integration branch. Every change here touches files that Phase 1 deliberately avoided to prevent merge conflicts: `MediaContainers.cs`, `_doc_snippets/Program.cs`, the doc index pages, and `TestFiles.txt`.

**Tech Stack:** C# 13 / .NET 10, xUnit, DocFX.

**Reference:** Spec §6 (shared changes & integration), §8 Phase 2 + Phase 3.

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib/IO/MediaContainers.cs` | Add 4 dispatch-dictionary entries; add 6 magic-byte probes with ID3v2-aware fast path. |
| `AudioVideoLib.Tests/IO/MediaContainersTests.cs` | Add cross-format dispatch tests including ID3v2-prefixed inputs. |
| `_doc_snippets/Program.cs` | Add S33 (MPC), S34 (WavPack), S35 (TTA), S36 (MAC). |
| `src/docs/getting-started.md` | Append four rows to the container list. |
| `src/docs/container-formats.md` | Add four index entries. |
| `src/docs/release-notes.md` | Add one entry covering the format pack + retrofit. |
| `src/TestFiles.txt` | Append four lines pointing at the per-format `PROVENANCE.md` fragments dropped by A1–A4. |

Creates NO new files (the four new format docs at `docs/container-formats/{mpc,wavpack,tta,mac}.md` were already created by their respective Phase-1 plans).

---

## Tasks

### Task 1: Verify Phase 1 deliverables are in place

- [ ] **Step 1: Confirm all Phase-1 outputs landed**

Run: `ls AudioVideoLib/IO/MpcStream.cs AudioVideoLib/IO/WavPackStream.cs AudioVideoLib/IO/TtaStream.cs AudioVideoLib/IO/MacStream.cs docs/container-formats/mpc.md docs/container-formats/wavpack.md docs/container-formats/tta.md docs/container-formats/mac.md AudioVideoLib.Tests/TestFiles/mpc/PROVENANCE.md AudioVideoLib.Tests/TestFiles/wavpack/PROVENANCE.md AudioVideoLib.Tests/TestFiles/tta/PROVENANCE.md AudioVideoLib.Tests/TestFiles/mac/PROVENANCE.md`

Expected: every listed path exists. If any are missing, stop and finish the corresponding Phase-1 plan first.

- [ ] **Step 2: Confirm `FlacStream` / `MpaStream` retrofit landed**

Run: `grep -c "_source.CopyTo" AudioVideoLib/IO/FlacStream.cs AudioVideoLib/IO/MpaStream.cs`

Expected: each file has at least one match (the byte-passthrough call in `WriteTo`).

- [ ] **Step 3: Confirm Phase 0 contract is in place**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Phase0ContractTests"`

Expected: 5 tests, all pass.

---

### Task 2: Register the four new walkers in the factory dictionary

**Files:**
- Modify: `AudioVideoLib/IO/MediaContainers.cs:18-30` (the `_factories` dictionary)

- [ ] **Step 1: Add four entries**

Locate the factory dictionary at the top of `MediaContainers.cs` (around lines 18-30, beginning with `{ typeof(MpaStream), () => new MpaStream() },`). Append the four new entries in the same style, ordered alphabetically by class name:

```csharp
{ typeof(MacStream), () => new MacStream() },
{ typeof(MpcStream), () => new MpcStream() },
{ typeof(TtaStream), () => new TtaStream() },
{ typeof(WavPackStream), () => new WavPackStream() },
```

- [ ] **Step 2: Add the corresponding `using AudioVideoLib.IO;` types if needed**

If the four new walker classes are in `AudioVideoLib.IO` (they should be, per the spec §4 file layout), no using statement is needed. Verify with: `grep -n "namespace AudioVideoLib.IO" AudioVideoLib/IO/MpcStream.cs AudioVideoLib/IO/WavPackStream.cs AudioVideoLib/IO/TtaStream.cs AudioVideoLib/IO/MacStream.cs`. Expected: four matches, all the same namespace as `MediaContainers.cs`.

- [ ] **Step 3: Build to confirm**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: clean build.

---

### Task 3: Add the six magic-byte probe branches with ID3v2-aware fast path

**Files:**
- Modify: `AudioVideoLib/IO/MediaContainers.cs` (the `TryProbeWalkerAtOffset` style method around lines 282-349)

- [ ] **Step 1: Identify the probe block**

Locate the cascading `if` chain that starts with the FLAC `fLaC` check at line 284 and ends with the MP4 `ftyp` check at line 344, returning `false` at line 349. This is the offset-0 magic dispatch.

- [ ] **Step 2: Increase the read buffer size if needed**

Confirm the buffer at the top of the probe method is at least 4 bytes (it currently is). The four new probes only need 4 bytes at offset 0, same as existing probes. No buffer change required.

- [ ] **Step 3: Insert the six new probe branches**

Insert the following six branches BEFORE the `return false` at the bottom of the probe block (i.e., after the MP4 `ftyp` check around line 344). Order: alphabetical by class name to match the factory dictionary, with both MPC versions adjacent.

```csharp
        // MPC SV7: 4-byte magic — first 3 bytes are 'M','P','+', 4th byte's low nibble = 7.
        if (buf[0] == 0x4D && buf[1] == 0x50 && buf[2] == 0x2B && (buf[3] & 0x0F) == 0x07)
        {
            walker = typeof(MpcStream);
            return true;
        }

        // MPC SV8: "MPCK" at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x50 && buf[2] == 0x43 && buf[3] == 0x4B)
        {
            walker = typeof(MpcStream);
            return true;
        }

        // WavPack: "wvpk" at offset 0.
        if (buf[0] == 0x77 && buf[1] == 0x76 && buf[2] == 0x70 && buf[3] == 0x6B)
        {
            walker = typeof(WavPackStream);
            return true;
        }

        // TrueAudio: "TTA1" at offset 0.
        if (buf[0] == 0x54 && buf[1] == 0x54 && buf[2] == 0x41 && buf[3] == 0x31)
        {
            walker = typeof(TtaStream);
            return true;
        }

        // Monkey's Audio (MAC): "MAC " (integer) at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x41 && buf[2] == 0x43 && buf[3] == 0x20)
        {
            walker = typeof(MacStream);
            return true;
        }

        // Monkey's Audio (MAC): "MACF" (float) at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x41 && buf[2] == 0x43 && buf[3] == 0x46)
        {
            walker = typeof(MacStream);
            return true;
        }
```

- [ ] **Step 4: Build to confirm**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: clean build.

---

### Task 4: Add ID3v2-aware probe path

**Files:**
- Modify: `AudioVideoLib/IO/MediaContainers.cs` (probe method)

- [ ] **Step 1: Locate the probe entry point**

The method that calls `TryProbeWalkerAtOffset` (or equivalent) is the public dispatch path. Currently a typical `<id3v2><wvpk audio>` file relies on the brute-force scan to find `wvpk` past the ID3v2 — slow.

- [ ] **Step 2: Add a fast-path check that skips ID3v2 before re-probing**

Before falling back to the brute-force scan, peek at offset 0 for an ID3v2 header (`ID3` at 0..2, version at 3..4, flags at 5, syncsafe size at 6..9). If present, compute the size = `((b6 & 0x7F) << 21) | ((b7 & 0x7F) << 14) | ((b8 & 0x7F) << 7) | (b9 & 0x7F)`, plus 10 bytes for the header, plus 10 more if the footer flag (bit 4 of the flags byte) is set. Re-run `TryProbeWalkerAtOffset` at that offset.

Add a helper near the existing probe method:

```csharp
private static bool TryProbeAfterId3v2(Stream stream, long startPosition, out Type? walker)
{
    walker = null;
    Span<byte> id3 = stackalloc byte[10];
    stream.Position = startPosition;
    var n = stream.Read(id3);
    stream.Position = startPosition;
    if (n < 10) return false;
    if (id3[0] != 0x49 || id3[1] != 0x44 || id3[2] != 0x33) return false; // "ID3"

    var size = ((id3[6] & 0x7F) << 21)
             | ((id3[7] & 0x7F) << 14)
             | ((id3[8] & 0x7F) << 7)
             | (id3[9] & 0x7F);
    var footer = (id3[5] & 0x10) != 0 ? 10 : 0;
    var afterTagOffset = startPosition + 10 + size + footer;
    return TryProbeWalkerAtOffset(stream, afterTagOffset, out walker);
}
```

Wire it into the dispatcher: when the offset-0 probe fails, call `TryProbeAfterId3v2` before falling back to the brute-force scan.

- [ ] **Step 3: Build to confirm**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: clean build.

- [ ] **Step 4: Commit progress so far**

```bash
git add AudioVideoLib/IO/MediaContainers.cs
git commit -m "feat(io): register MPC/WavPack/TTA/MAC walkers; add ID3v2-aware dispatch

Per specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md §6.2.
Six new magic-byte probes (MPC SV7, MPC SV8, wvpk, TTA1, 'MAC ', MACF)
plus a probe-after-ID3v2 fast path so id3v2-prefixed inputs dispatch in O(1)
without a byte-by-byte rescan."
```

---

### Task 5: Add cross-format dispatch tests

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MediaContainersTests.cs`

- [ ] **Step 1: Locate the existing dispatch tests**

Open `AudioVideoLib.Tests/IO/MediaContainersTests.cs`. Find the existing `[Fact]` methods that assert `MediaContainers.ReadStream(fs)` returns a walker of the expected type for a given input.

- [ ] **Step 2: Add a `[Theory]` covering the four new formats and their ID3v2-prefixed variants**

Append:

```csharp
public static IEnumerable<object[]> NewFormatDispatchData =>
    [
        ["TestFiles/mpc/sv7.mpc",        typeof(MpcStream)],
        ["TestFiles/mpc/sv8.mpc",        typeof(MpcStream)],
        ["TestFiles/wavpack/sample.wv",  typeof(WavPackStream)],
        ["TestFiles/tta/sample.tta",     typeof(TtaStream)],
        ["TestFiles/mac/integer.ape",    typeof(MacStream)],
        ["TestFiles/mac/float.ape",      typeof(MacStream)],
    ];

[Theory]
[MemberData(nameof(NewFormatDispatchData))]
public void Dispatch_NewFormats_ReturnsExpectedWalker(string path, Type expectedWalker)
{
    using var fs = File.OpenRead(path);
    using var streams = MediaContainers.ReadStream(fs);
    var walker = streams.FirstOrDefault();
    Assert.NotNull(walker);
    Assert.IsType(expectedWalker, walker);
}

[Theory]
[MemberData(nameof(NewFormatDispatchData))]
public void Dispatch_NewFormatsWithId3v2Prefix_ReturnsExpectedWalker(string path, Type expectedWalker)
{
    // Splice a minimal ID3v2.4 tag (10-byte header, 0-byte body) onto the front of the
    // file in memory, then assert the dispatcher still finds the right walker.
    var original = File.ReadAllBytes(path);
    var id3 = new byte[10];
    id3[0] = 0x49; id3[1] = 0x44; id3[2] = 0x33; // "ID3"
    id3[3] = 0x04; id3[4] = 0x00;                // v2.4.0
    id3[5] = 0x00;                               // flags: none
    id3[6] = id3[7] = id3[8] = id3[9] = 0x00;    // syncsafe size = 0
    var prefixed = id3.Concat(original).ToArray();

    using var fs = new MemoryStream(prefixed);
    using var streams = MediaContainers.ReadStream(fs);
    var walker = streams.FirstOrDefault();
    Assert.NotNull(walker);
    Assert.IsType(expectedWalker, walker);
}
```

- [ ] **Step 3: Run the new dispatch tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~Dispatch_NewFormats" -v normal`
Expected: 12 tests (6 paths × 2 theories), all pass.

- [ ] **Step 4: Commit**

```bash
git add AudioVideoLib.Tests/IO/MediaContainersTests.cs
git commit -m "test(io): cross-format dispatch tests for MPC/WavPack/TTA/MAC including ID3v2-prefixed inputs"
```

---

### Task 6: Add doc snippets S33–S36

**Files:**
- Modify: `_doc_snippets/Program.cs`

- [ ] **Step 1: Locate the snippet runner**

Open `_doc_snippets/Program.cs`. Find the `Main` method and the `Run("S30_MpaVbrAndLame", S30_MpaVbrAndLame);` call (around line 55 per the file we read earlier). The new snippets fit numerically after S32 (`S32_OggPagesAndCodec`).

- [ ] **Step 2: Register S33–S36 in `Main`**

After the existing `Run("S32_OggPagesAndCodec", S32_OggPagesAndCodec);` line, add:

```csharp
        Run("S33_MpcReadHeader", S33_MpcReadHeader);
        Run("S34_WavPackReadBlocks", S34_WavPackReadBlocks);
        Run("S35_TtaReadFrames", S35_TtaReadFrames);
        Run("S36_MacReadDescriptor", S36_MacReadDescriptor);
```

- [ ] **Step 3: Implement S33_MpcReadHeader**

Add a method below the existing snippet methods. Mirror the shape of `S30_MpaVbrAndLame`:

```csharp
    // S33 — container-formats/mpc.md: read MPC header + report channels/sample rate/duration.
    private static void S33_MpcReadHeader()
    {
        var mpc = new MpcStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"MPC {mpc.Version}: {mpc.Channels} ch @ {mpc.SampleRate} Hz, {mpc.TotalDuration:N0} ms");
        foreach (var packet in mpc.Packets.Take(3))
        {
            Console.WriteLine($"  {packet.Key ?? "(SV7 frame)"} @ 0x{packet.StartOffset:X8}, {packet.Length} bytes");
        }
        // ===== SNIPPET END =====
    }
```

- [ ] **Step 4: Implement S34_WavPackReadBlocks**

```csharp
    // S34 — container-formats/wavpack.md: read WavPack blocks + sub-blocks.
    private static void S34_WavPackReadBlocks()
    {
        var wv = new WavPackStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"WavPack: {wv.Channels} ch @ {wv.SampleRate} Hz, {wv.Blocks.Count():N0} blocks");
        foreach (var block in wv.Blocks.Take(2))
        {
            Console.WriteLine($"  block @ 0x{block.StartOffset:X8}: {block.Length} bytes, {block.SubBlocks.Count()} sub-blocks");
            foreach (var sub in block.SubBlocks)
            {
                Console.WriteLine($"    sub {sub.Id}: {sub.Size} bytes");
            }
        }
        // ===== SNIPPET END =====
    }
```

- [ ] **Step 5: Implement S35_TtaReadFrames**

```csharp
    // S35 — container-formats/tta.md: read TTA header + report frame count and duration.
    private static void S35_TtaReadFrames()
    {
        var tta = new TtaStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"TTA: {tta.Channels} ch @ {tta.SampleRate} Hz, {tta.BitsPerSample}-bit");
        Console.WriteLine($"     {tta.Frames.Count():N0} frames, {tta.TotalDuration:N0} ms");
        // ===== SNIPPET END =====
    }
```

- [ ] **Step 6: Implement S36_MacReadDescriptor**

```csharp
    // S36 — container-formats/mac.md: read MAC descriptor + format flag.
    private static void S36_MacReadDescriptor()
    {
        var mac = new MacStream();

        // ===== SNIPPET START =====
        Console.WriteLine($"MAC ({mac.Format}): {mac.Channels} ch @ {mac.SampleRate} Hz, {mac.BitsPerSample}-bit");
        Console.WriteLine($"     compression level {mac.Header.CompressionLevel}, {mac.Frames.Count():N0} frames");
        // ===== SNIPPET END =====
    }
```

- [ ] **Step 7: Audit S20 and S30 for read-only-API breakage**

Per spec §6.4: re-read `S20_FlacEditVorbisComment` and `S30_MpaVbrAndLame` in the file. The retrofit made `MpaFrame.*` properties read-only. `S30` only reads `mpa.VbrHeader` (already read-only on the public surface), `vbr.HeaderType`, `vbr.FrameCount`, `vbr.FileSize`, `lame.EncoderVersion`, `lame.VbrMethodName` — all read-only. No change needed to S30.

For S20: it edits `vc.Comments` (a list on the Vorbis-comment metadata block, which stays mutable per spec §5.1). It calls `flac.ToByteArray()` (a `FlacStream` extension method) which after the retrofit splices unchanged audio frames from `_source`. No source-code changes to S20 expected.

Confirm both still compile; nothing further needed.

- [ ] **Step 8: Run `_doc_snippets`**

Run: `dotnet run --project _doc_snippets`
Expected: exit code 0. All snippets including S33–S36 PASS.

- [ ] **Step 9: Commit**

```bash
git add _doc_snippets/Program.cs
git commit -m "docs(snippets): add S33-S36 for MPC/WavPack/TTA/MAC containers"
```

---

### Task 7: Update getting-started.md

**Files:**
- Modify: `src/docs/getting-started.md` (around lines 44-52, the container list under "MediaContainers — container probe")

- [ ] **Step 1: Find the container list**

The list currently reads:

```markdown
- MPEG audio (Layer I/II/III, v1/v2/2.5)
- FLAC
- RIFF / WAV, RIFX
- AIFF, AIFF-C
- OGG
- MP4 / M4A (ISO/IEC 14496-12 with iTunes-style metadata)
- ASF / WMA / WMV
- Matroska / WebM
- DSF (Sony), DFF (Philips)
```

- [ ] **Step 2: Append four rows**

After the `DSF / DFF` line, add:

```markdown
- Musepack (`.mpc`, SV7 + SV8)
- WavPack (`.wv`, including hybrid `.wvc` correction stream when present)
- TrueAudio (`.tta`)
- Monkey's Audio (`.ape`, integer + float)
```

- [ ] **Step 3: Verify DocFX builds the page**

Run: `docfx docfx.json`
Expected: zero new warnings beyond the six pre-existing ones from the spec self-review.

---

### Task 8: Update container-formats.md

**Files:**
- Modify: `src/docs/container-formats.md`

- [ ] **Step 1: Find the index entries**

The file lists each format with a short blurb and a link to its per-format page. Verify the existing structure (e.g., `- [FLAC](container-formats/flac.md) — short description.`).

- [ ] **Step 2: Append four index entries**

After the existing entries, add:

```markdown
- [Musepack](container-formats/mpc.md) — `.mpc` files, SV7 and SV8 stream versions.
- [WavPack](container-formats/wavpack.md) — `.wv` lossless / hybrid lossy.
- [TrueAudio](container-formats/tta.md) — `.tta` lossless.
- [Monkey's Audio](container-formats/mac.md) — `.ape` integer or float.
```

- [ ] **Step 3: Update the toc**

Open `src/docs/container-formats/toc.yml`. Add four entries pointing at the new per-format pages, in the same shape as the existing entries.

- [ ] **Step 4: DocFX build**

Run: `docfx docfx.json`
Expected: zero new warnings; the four new pages link from both `container-formats.md` and the table of contents.

---

### Task 9: Update release-notes.md

**Files:**
- Modify: `src/docs/release-notes.md`

- [ ] **Step 1: Add a top-of-file entry**

Append at the top (most recent first):

```markdown
## (next release)

### Added
- `MpcStream` — Musepack walker (SV7 + SV8).
- `WavPackStream` — WavPack walker (`.wv` + optional `.wvc` correction stream in same file).
- `TtaStream` — TrueAudio walker.
- `MacStream` — Monkey's Audio walker (integer + float). Note: separate from the existing `ApeTag` family (which handles APE *tags*, not the Monkey's Audio audio container).

### Changed
- `IMediaContainer` now extends `IDisposable`. Walkers that hold an `ISourceReader` throw `InvalidOperationException` from `WriteTo` if the source has been disposed or was never populated. See the "source-stream lifetime contract" docstring on `IMediaContainer`.
- `FlacStream` and `MpaStream` now use offset-based byte-passthrough on save: per-frame audio bytes are spliced from the source stream rather than re-emitted from the parsed model. Audio is byte-identical on round-trip; no audio re-encoding occurs anywhere in the library.

### Breaking
- All `FlacFrame`, `FlacSubFrame`, `FlacResidual`, `FlacRicePartition`, `MpaFrame`, `MpaFrameHeader`, `MpaFrameData` properties are now read-only. Callers cannot mutate audio-frame data. (Tag editing — Vorbis comments, ID3 frames, etc. — remains fully supported via the metadata-block path.)
- `IMediaContainer` consumers must dispose the walker (or the parent `MediaContainers`) when done. Wrap usage in `using`.
```

- [ ] **Step 2: DocFX build**

Run: `docfx docfx.json`
Expected: zero new warnings.

---

### Task 10: Update TestFiles.txt

**Files:**
- Modify: `src/TestFiles.txt`

- [ ] **Step 1: Append references to the four per-format provenance fragments**

Append to `src/TestFiles.txt`:

```
mpc/PROVENANCE.md      — Musepack samples; see fragment for source + license.
wavpack/PROVENANCE.md  — WavPack samples; see fragment for source + license.
tta/PROVENANCE.md      — TrueAudio samples; see fragment for source + license.
mac/PROVENANCE.md      — Monkey's Audio samples; see fragment for source + license.
```

(Match the existing format of `TestFiles.txt`. The current file is short; if the format differs from what's shown above, follow the file's own convention.)

- [ ] **Step 2: Commit the docs and manifest updates**

```bash
git add src/docs/getting-started.md src/docs/container-formats.md src/docs/container-formats/toc.yml src/docs/release-notes.md src/TestFiles.txt
git commit -m "docs(site): add MPC/WavPack/TTA/MAC to format index, getting-started, release notes, test manifest"
```

---

## Phase 3: validation

### Task 11: Full Release build

- [ ] **Step 1: Clean Release build of the whole solution**

Run: `dotnet build AudioVideoLib.slnx -c Release`
Expected: zero errors, zero warnings (or only the pre-existing six XML cref warnings).

---

### Task 12: Full test suite

- [ ] **Step 1: Run every test**

Run: `dotnet test AudioVideoLib.slnx -c Release`
Expected: zero failures across all test projects.

---

### Task 13: Doc snippets compile + run gate

- [ ] **Step 1: Run `_doc_snippets`**

Run: `dotnet run --project _doc_snippets`
Expected: exit code 0. Console error stream shows `[PASS]` for every snippet S01–S70 plus the new S33–S36 (37 total currently registered, plus 4 new = 41).

---

### Task 14: DocFX site build

- [ ] **Step 1: DocFX build**

Run: `docfx docfx.json`
Expected: clean build. Six pre-existing `InvalidCref` warnings allowed; zero new warnings.

- [ ] **Step 2: Visual smoke-test the four new pages**

Open in a browser:
- `_site/docs/container-formats/mpc.html`
- `_site/docs/container-formats/wavpack.html`
- `_site/docs/container-formats/tta.html`
- `_site/docs/container-formats/mac.html`

Verify each renders with the H2/H3 hierarchy from the project's custom CSS (`templates/custom/public/main.css`), shows the new content from its Phase-1 plan, and links correctly back from `container-formats.md` and the sidebar.

- [ ] **Step 3: Visual smoke-test the updated index pages**

Open `_site/docs/getting-started.html` and `_site/docs/container-formats.html`. Confirm the four new entries appear in the lists.

---

### Task 15: Final integration commit

- [ ] **Step 1: If any cleanup happened during validation, commit it**

```bash
git status
# If anything is modified:
git add <files>
git commit -m "chore: format-pack final-validation cleanup"
```

- [ ] **Step 2: Squash-merge the integration branch to master**

The user's preference (per the brainstorming session) is one big commit on master covering Phases 0–3. Squash the entire integration branch:

```bash
git checkout master
git merge --squash <integration-branch>
git commit -m "feat: format pack — MPC, WavPack, TTA, MAC walkers + FLAC/MPA byte-passthrough retrofit

Adds four new IMediaContainer walkers (Musepack SV7+SV8, WavPack, TrueAudio, Monkey's Audio
integer+float) and retrofits FlacStream/MpaStream onto the same offset-based byte-passthrough
model used by Mp4Stream. IMediaContainer now extends IDisposable; walkers that hold an
ISourceReader throw InvalidOperationException from WriteTo if the source has been disposed.

See specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md for the full design.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 3: Push to trigger the docs publish workflow**

```bash
git push origin master
```

The `Publish docs` workflow rebuilds `audiovideolib.github.io` with the new content.

---

## Acceptance criteria for Phase 2 + Phase 3

- `MediaContainers` factory dictionary contains entries for all four new walker types.
- Six magic-byte probes (MPC SV7, MPC SV8, wvpk, TTA1, `MAC `, `MACF`) plus the ID3v2-aware fast-path dispatch are in `MediaContainers.cs`.
- Cross-format dispatch tests pass for all four formats, both raw and ID3v2-prefixed.
- `_doc_snippets` registers and passes S33–S36; S20 and S30 still pass without modification.
- `getting-started.md`, `container-formats.md`, `container-formats/toc.yml`, `release-notes.md`, `TestFiles.txt` all reference the four new formats.
- `dotnet build -c Release` clean.
- `dotnet test` all green.
- `docfx docfx.json` clean (six pre-existing warnings allowed; zero new).
- The four new container pages render correctly in `_site/docs/container-formats/`.
- Final commit squash-merged to master and pushed; `Publish docs` workflow runs successfully.
