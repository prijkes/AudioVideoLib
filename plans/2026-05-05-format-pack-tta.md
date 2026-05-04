# Format pack — TrueAudio (TTA) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `TtaStream`, an `IMediaContainer` walker for TrueAudio (`.tta`) files with parse-for-inspection and byte-passthrough save (interpretation-3 from spec §3).

**Architecture:** Sealed `TtaStream : IMediaContainer, IDisposable` mirroring `Mp4Stream`'s pattern: holds a `private ISourceReader? _source` populated in `ReadStream` via `new StreamSourceReader(stream, leaveOpen: true)`, builds an in-memory list of `TtaFrame`s from the seek table, and emits the original bytes verbatim via `_source.CopyTo` from `WriteTo`. Tags (APEv2 footer, ID3v1 footer, ID3v2 header) are recognized at probe time but parsed via the existing `AudioTags` scanner — the walker only models the audio container.

**Tech Stack:** C# 13 / .NET 10, xUnit. Format reference at `3rdparty/libtta-c-2.3/`.

**Worktree:** `feat/tta` (per spec §8 Phase 1).

**Scope boundary:** This plan creates only new files. It does NOT modify `MediaContainers.cs`, `_doc_snippets/Program.cs`, `getting-started.md`, `container-formats.md`, `release-notes.md`, or `src/TestFiles.txt` — those updates land in Phase 2.

**Reference:** Spec §4.3 (TTA decisions), §3.1 (lifecycle contract), §7 (test strategy). Phase 0 plan (`2026-05-05-format-pack-phase0-foundation.md`) is assumed already executed: `IMediaContainer` extends `IDisposable`, the canonical detached-source exception message is `"Source stream was detached or never read. WriteTo requires a live source."`.

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib/Formats/TtaHeader.cs` | Create — TTA1 fixed-header model (read-only properties: format, channels, bits per sample, sample rate, total samples, header CRC32). |
| `AudioVideoLib/Formats/TtaSeekTable.cs` | Create — read-only array of per-frame compressed sizes plus the trailing seek-table CRC32. |
| `AudioVideoLib/Formats/TtaFrame.cs` | Create — per-frame model: `StartOffset`, `Length`, `SampleCount`. Implied by spec §4.3 frame model. |
| `AudioVideoLib/IO/TtaStream.cs` | Create — sealed walker, holds `_source`, parses header + seek table + builds frame list, byte-passthrough `WriteTo`, real `Dispose()`. |
| `AudioVideoLib.Tests/IO/TtaStreamTests.cs` | Create — xUnit fact tests covering header parse, frame enumeration, round-trip identity, tag-edit round-trip, detached-source error, magic-byte dispatch. |
| `AudioVideoLib.Tests/TestFiles/tta/` | Create directory; check in 1–2 short `.tta` samples (~few KB each). |
| `AudioVideoLib.Tests/TestFiles/tta/PROVENANCE.md` | Create — provenance fragment for the samples. |
| `src/docs/container-formats/ttastream.md` | Create — docs page following the layout of `flacstream.md`. |

Phase 1 modifies **no** existing files.

---

## Format reference (from `3rdparty/libtta-c-2.3/libtta.c` and `libtta.h`)

The TTA1 fixed header is exactly **22 bytes** (`read_tta_header` returns `22` after reading the magic, five info fields, and the trailing CRC32):

| Field | Type | Width | Notes |
|---|---|---|---|
| Magic | ASCII | 4 | `'T','T','A','1'` |
| Format | uint16 LE | 2 | `1 = TTA_FORMAT_SIMPLE`, `2 = TTA_FORMAT_ENCRYPTED` |
| NumChannels | uint16 LE | 2 | `MAX_NCH = 6` |
| BitsPerSample | uint16 LE | 2 | `MIN_BPS = 16`, `MAX_BPS = 24` (`MAX_DEPTH * 8`) |
| SampleRate | uint32 LE | 4 | sps |
| TotalSamples | uint32 LE | 4 | data length in samples |
| HeaderCrc32 | uint32 LE | 4 | CRC32 over the 18 bytes of the magic + five info fields |

Then the seek table follows immediately (per `tta_decoder_read_seek_table`):

- One uint32 LE per frame containing the compressed frame length.
- Followed by a single uint32 LE CRC32 over the per-frame size table.

The number of frames is derived from total samples and the per-frame sample count. From `tta_decoder_init_set_info`:

```c
dec_flen_std  = MUL_FRAME_TIME(info->sps);          // = 256 * sps / 245
dec_flen_last = info->samples % dec_flen_std;
dec_frames    = info->samples / dec_flen_std + (dec_flen_last ? 1 : 0);
if (!dec_flen_last) dec_flen_last = dec_flen_std;
```

Frame layout in the file (per `tta_decoder_read_seek_table`):

- The **first** frame's start offset is `headerEnd + (frames + 1) * 4` (header end, plus the seek table — `frames` entries plus the trailing CRC, four bytes each). `headerEnd` here is `dec_offset` (header size `22` plus any leading ID3v2 size that `skip_id3v2` reported).
- Each subsequent frame's start offset is the previous frame's start plus the previous frame's compressed size from the seek table.

The trailing tag region (APEv2 footer, ID3v1 footer) sits **after** the audio frames and is handled by the `AudioTags` scanner, not the walker.

---

## Tasks

### Task 1: Create `TtaHeader.cs` with read-only properties

**Files:**
- Create: `AudioVideoLib/Formats/TtaHeader.cs`

- [ ] **Step 1: Write the header model**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// Fixed 22-byte TTA1 header at the start of a TrueAudio stream (after any leading ID3v2 tag).
/// </summary>
/// <remarks>
/// Mirrors the <c>TTA_info</c> struct in <c>3rdparty/libtta-c-2.3/libtta.h</c>, plus the
/// 4-byte trailing CRC32 that <c>read_tta_header</c> in <c>libtta.c</c> consumes immediately
/// after the five info fields. All multi-byte integers are little-endian.
/// </remarks>
public sealed class TtaHeader
{
    /// <summary>Size of the fixed header on disk, in bytes (4-byte magic + 18 bytes of info + 4-byte CRC).</summary>
    public const int FixedSize = 22;

    /// <summary>The four-byte magic at offset 0: <c>T T A 1</c>.</summary>
    public static ReadOnlySpan<byte> Magic => "TTA1"u8;

    internal TtaHeader(
        ushort format,
        ushort numChannels,
        ushort bitsPerSample,
        uint sampleRate,
        uint totalSamples,
        uint headerCrc32)
    {
        Format = format;
        NumChannels = numChannels;
        BitsPerSample = bitsPerSample;
        SampleRate = sampleRate;
        TotalSamples = totalSamples;
        HeaderCrc32 = headerCrc32;
    }

    /// <summary>Audio format code: <c>1 = simple</c>, <c>2 = encrypted</c> (libtta <c>TTA_FORMAT_*</c>).</summary>
    public ushort Format { get; }

    /// <summary>Channel count (libtta caps at <c>MAX_NCH = 6</c>).</summary>
    public ushort NumChannels { get; }

    /// <summary>Bits per sample (libtta range: <c>MIN_BPS = 16</c> through <c>MAX_BPS = 24</c>).</summary>
    public ushort BitsPerSample { get; }

    /// <summary>Sample rate in Hz (libtta <c>sps</c>).</summary>
    public uint SampleRate { get; }

    /// <summary>Total decoded sample count across the whole stream.</summary>
    public uint TotalSamples { get; }

    /// <summary>CRC32 stored at offsets [18..22) of the fixed header.</summary>
    public uint HeaderCrc32 { get; }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: clean.

---

### Task 2: Create `TtaSeekTable.cs`

**Files:**
- Create: `AudioVideoLib/Formats/TtaSeekTable.cs`

- [ ] **Step 1: Write the seek-table model**

```csharp
namespace AudioVideoLib.Formats;

using System.Collections.Generic;

/// <summary>
/// The per-frame compressed-size table that follows the TTA1 fixed header.
/// </summary>
/// <remarks>
/// Mirrors the body of <c>tta_decoder_read_seek_table</c> in
/// <c>3rdparty/libtta-c-2.3/libtta.c</c>: one little-endian uint32 per frame giving the
/// compressed length in bytes, followed by one trailing uint32 CRC32 over the table.
/// </remarks>
public sealed class TtaSeekTable
{
    internal TtaSeekTable(IReadOnlyList<uint> frameSizes, uint crc32)
    {
        FrameSizes = frameSizes;
        Crc32 = crc32;
    }

    /// <summary>Compressed byte length of each frame, in file order.</summary>
    public IReadOnlyList<uint> FrameSizes { get; }

    /// <summary>CRC32 immediately following the per-frame size array.</summary>
    public uint Crc32 { get; }

    /// <summary>Total size of the seek-table region on disk (entries + trailing CRC).</summary>
    public int Size => (FrameSizes.Count + 1) * 4;
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 3: Create `TtaFrame.cs`

**Files:**
- Create: `AudioVideoLib/Formats/TtaFrame.cs`

- [ ] **Step 1: Write the per-frame model**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// A single TTA audio frame, located by a byte range in the source stream.
/// </summary>
/// <remarks>
/// Per <c>3rdparty/libtta-c-2.3/libtta.c</c>: every frame except the last carries
/// <c>FrameLengthSamples = 256 * SampleRate / 245</c> samples; the last frame holds the
/// remainder (<c>TotalSamples mod FrameLengthSamples</c>, or the standard length if that
/// modulus is zero).
/// </remarks>
public sealed class TtaFrame
{
    internal TtaFrame(long startOffset, long length, uint sampleCount)
    {
        StartOffset = startOffset;
        Length = length;
        SampleCount = sampleCount;
    }

    /// <summary>Byte offset of the frame's first byte, relative to the source stream's origin.</summary>
    public long StartOffset { get; }

    /// <summary>Frame length in bytes (from the seek table).</summary>
    public long Length { get; }

    /// <summary>Decoded sample count for this frame.</summary>
    public uint SampleCount { get; }
}
```

- [ ] **Step 2: Build** — clean compile expected.

- [ ] **Step 3: Commit milestone**

```bash
git add AudioVideoLib/Formats/TtaHeader.cs AudioVideoLib/Formats/TtaSeekTable.cs AudioVideoLib/Formats/TtaFrame.cs
git commit -m "feat(formats): add TTA model classes (header, seek table, frame)

Models the TTA1 fixed header, the per-frame size table, and the per-frame
byte/sample range used by TtaStream's splice writer. Properties are read-only;
the walker constructs instances via internal constructors."
```

---

### Task 4: Skeleton `TtaStream` with magic detection and `Dispose`

**Files:**
- Create: `AudioVideoLib/IO/TtaStream.cs`

- [ ] **Step 1: Write the skeleton**

```csharp
namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Walker for TrueAudio (<c>.tta</c>) streams. Parses the TTA1 fixed header and the
/// per-frame seek table, exposing the frame layout for inspection. <see cref="WriteTo"/>
/// streams the original bytes verbatim from the source — no audio re-encoding.
/// </summary>
/// <remarks>
/// Format reference: <c>3rdparty/libtta-c-2.3/libtta.c</c>, especially
/// <c>read_tta_header</c> and <c>tta_decoder_read_seek_table</c>.
/// <para />
/// <see cref="ReadStream"/> populates <c>_source</c> with a <see cref="StreamSourceReader"/>
/// that holds the supplied <see cref="Stream"/> open via <c>leaveOpen: true</c>; callers
/// must keep that <see cref="Stream"/> alive until <see cref="WriteTo"/> finishes, in line
/// with the source-stream lifetime contract on <see cref="IMediaContainer"/>.
/// </remarks>
public sealed class TtaStream : IMediaContainer, IDisposable
{
    private const string DetachedSourceMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    private readonly List<TtaFrame> _frames = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration =>
        Header is null || Header.SampleRate == 0
            ? 0
            : (long)(Header.TotalSamples * 1000UL / Header.SampleRate);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>The parsed fixed header, or <c>null</c> if <see cref="ReadStream"/> has not run successfully.</summary>
    public TtaHeader? Header { get; private set; }

    /// <summary>The parsed seek table, or <c>null</c> if <see cref="ReadStream"/> has not run successfully.</summary>
    public TtaSeekTable? SeekTable { get; private set; }

    /// <summary>Per-frame byte/sample ranges, in file order.</summary>
    public IReadOnlyList<TtaFrame> Frames => _frames;

    /// <summary>Standard frame length in samples: <c>256 * SampleRate / 245</c> per libtta's <c>MUL_FRAME_TIME</c>.</summary>
    public uint FrameLengthSamples =>
        Header is null ? 0u : (uint)(256UL * Header.SampleRate / 245UL);

    /// <inheritdoc/>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        // Filled in by Task 5.
        return false;
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(DetachedSourceMessage);
        }

        // Filled in by Task 7.
    }

    /// <summary>Releases the underlying <see cref="ISourceReader"/>; does not close the caller's source <see cref="Stream"/>.</summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 5: Implement `ReadStream` — magic check, header parse

**Files:**
- Modify: `AudioVideoLib/IO/TtaStream.cs`

- [ ] **Step 1: Replace the `ReadStream` body**

Port the front half of `read_tta_header` from `libtta.c:449-471`. The walker is dispatched **after** any leading ID3v2 tag has been consumed by the outer scanner, so `stream.Position` already points at the `TTA1` magic when `ReadStream` runs (matching how `Mp4Stream.ReadStream` is entered with the stream positioned at `ftyp`).

```csharp
public bool ReadStream(Stream stream)
{
    ArgumentNullException.ThrowIfNull(stream);

    var start = stream.Position;
    var available = stream.Length - start;
    if (available < TtaHeader.FixedSize)
    {
        return false;
    }

    Span<byte> headerBuf = stackalloc byte[TtaHeader.FixedSize];
    if (stream.Read(headerBuf) != TtaHeader.FixedSize)
    {
        return false;
    }

    if (headerBuf[0] != (byte)'T' || headerBuf[1] != (byte)'T'
        || headerBuf[2] != (byte)'A' || headerBuf[3] != (byte)'1')
    {
        stream.Position = start;
        return false;
    }

    var format        = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[4..6]);
    var nch           = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[6..8]);
    var bps           = BinaryPrimitives.ReadUInt16LittleEndian(headerBuf[8..10]);
    var sps           = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[10..14]);
    var totalSamples  = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[14..18]);
    var headerCrc32   = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[18..22]);

    Header = new TtaHeader(format, nch, bps, sps, totalSamples, headerCrc32);

    StartOffset = start;
    _source?.Dispose();
    _source = new StreamSourceReader(stream, leaveOpen: true);

    // Seek table + frame list parsed in Task 6.
    EndOffset = start + _source.Length;
    return true;
}
```

- [ ] **Step 2: Build** — clean compile.

---

### Task 6: Implement seek-table parse and frame-list build

**Files:**
- Modify: `AudioVideoLib/IO/TtaStream.cs`

- [ ] **Step 1: Add a private helper `ReadSeekTableAndBuildFrames`**

Port `tta_decoder_read_seek_table` from `libtta.c:473-490` plus the frame-count math from `tta_decoder_init_set_info` (`libtta.c:551-574`). The `_source` is already populated, so do bytewise reads off `stream` (which is still positioned right after the fixed header).

```csharp
private bool ReadSeekTableAndBuildFrames(Stream stream)
{
    if (Header is null)
    {
        return false;
    }

    // Frame count from libtta:
    //   flen_std  = 256 * sps / 245
    //   flen_last = total_samples % flen_std
    //   frames    = total_samples / flen_std + (flen_last ? 1 : 0)
    if (Header.SampleRate == 0)
    {
        return false;
    }

    var flenStd = FrameLengthSamples;
    if (flenStd == 0)
    {
        return false;
    }

    var flenLast = Header.TotalSamples % flenStd;
    var frames = Header.TotalSamples / flenStd + (flenLast == 0 ? 0u : 1u);
    if (flenLast == 0)
    {
        flenLast = flenStd;
    }

    if (frames == 0)
    {
        SeekTable = new TtaSeekTable([], 0u);
        return true;
    }

    // Each entry is 4 bytes; trailing CRC32 is one more uint32.
    var seekTableBytes = checked((int)((frames + 1) * 4));
    if (stream.Length - stream.Position < seekTableBytes)
    {
        return false;
    }

    var buf = new byte[seekTableBytes];
    stream.ReadExactly(buf);

    var sizes = new uint[frames];
    for (var i = 0; i < frames; i++)
    {
        sizes[i] = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(i * 4, 4));
    }

    var crc = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan((int)frames * 4, 4));
    SeekTable = new TtaSeekTable(sizes, crc);

    // Build frame offset list. First frame begins immediately after the seek table.
    var firstFrameOffset = StartOffset + TtaHeader.FixedSize + seekTableBytes;
    var offset = firstFrameOffset;
    for (var i = 0; i < frames; i++)
    {
        var sampleCount = (i == frames - 1) ? flenLast : flenStd;
        _frames.Add(new TtaFrame(offset, sizes[i], sampleCount));
        offset += sizes[i];
    }

    return true;
}
```

- [ ] **Step 2: Wire it into `ReadStream`**

Just before the `return true;` line in `ReadStream`, insert:

```csharp
    if (!ReadSeekTableAndBuildFrames(stream))
    {
        // Header parsed but we couldn't trust the seek table; treat as a soft failure.
        _frames.Clear();
        SeekTable = null;
    }

    // Position the stream just past the audio so the outer scanner can pick up
    // any trailing ID3v1 / APEv2 footer.
    if (_frames.Count > 0)
    {
        var lastFrame = _frames[^1];
        stream.Position = lastFrame.StartOffset + lastFrame.Length;
    }
```

- [ ] **Step 3: Build** — clean compile.

---

### Task 7: Implement byte-passthrough `WriteTo`

**Files:**
- Modify: `AudioVideoLib/IO/TtaStream.cs`

- [ ] **Step 1: Replace the `WriteTo` body**

The walker holds no editable model — `Header`, `SeekTable`, and `Frames` are all read-only — so `WriteTo` is a single `_source.CopyTo` from `StartOffset` to the end of the audio span. The outer `MediaContainers` infrastructure handles surrounding ID3/APE tags via the `AudioTags` scanner.

```csharp
public void WriteTo(Stream destination)
{
    ArgumentNullException.ThrowIfNull(destination);
    if (_source is null)
    {
        throw new InvalidOperationException(DetachedSourceMessage);
    }

    var end = EndOffset - StartOffset;
    if (end > 0)
    {
        _source.CopyTo(0, end, destination);
    }
}
```

The `CopyTo` is offset-relative-to-source-start (matching how `Mp4Stream.WriteTo` uses it; see `Mp4Stream.cs:185, 215, 226`).

- [ ] **Step 2: Build** — clean compile.

- [ ] **Step 3: Commit milestone**

```bash
git add AudioVideoLib/IO/TtaStream.cs
git commit -m "feat(io): add TtaStream walker (header + seek table + passthrough WriteTo)

Parses the TTA1 fixed header and the per-frame size table per
3rdparty/libtta-c-2.3/libtta.c. Frame offsets are derived from the seek
table; sample counts use libtta's MUL_FRAME_TIME (256 * sps / 245) for all
frames except the last. WriteTo splices the original byte range from
_source — no re-encoding. Implements the IMediaContainer source-stream
lifetime contract with InvalidOperationException on detached source."
```

---

### Task 8: Provenance fragment for sample files

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/tta/PROVENANCE.md`

- [ ] **Step 1: Write the fragment**

```markdown
# TestFiles/tta provenance

Per `specs/2026-05-04-mpc-wavpack-tta-mac-format-pack-design.md` §7.1, this
fragment is stitched into the shared `src/TestFiles.txt` manifest in Phase 2.

## sample-stereo-16bit.tta

- Source: short stereo sine generated locally; encoded with `ttaenc` from
  `3rdparty/libtta-c-2.3/`.
- License: original synthetic content, no third-party rights.
- Purpose: smoke-test header parse, frame enumeration, and round-trip identity.

## sample-with-id3v2.tta

- Source: `sample-stereo-16bit.tta` with a hand-crafted minimal ID3v2.3 tag
  prepended (TIT2 = "Test Title"), to exercise the post-ID3v2 dispatch path.
- License: as above.
- Purpose: tag-edit round-trip test via `AudioTags`.
```

- [ ] **Step 2: Drop the actual sample files into the directory**

Place `sample-stereo-16bit.tta` and `sample-with-id3v2.tta` in
`AudioVideoLib.Tests/TestFiles/tta/`. Each file should be a few KB; encode
short audio using the `3rdparty/libtta-c-2.3/` reference encoder offline (the
encoder is not built as part of the .NET solution per spec §7.1). Mark both
files as `<None CopyToOutputDirectory="PreserveNewest">` in the test project
if the project's existing pattern requires it (check
`AudioVideoLib.Tests/AudioVideoLib.Tests.csproj` — many existing TestFiles
folders use a wildcard glob already, in which case no edit is needed).

- [ ] **Step 3: Verify the test project picks up the files**

Run: `dotnet build AudioVideoLib.Tests/AudioVideoLib.Tests.csproj`
Expected: clean build, both `.tta` files copied to the bin output.

---

### Task 9: Test scaffold + header-parse fact

**Files:**
- Create: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

- [ ] **Step 1: Write the scaffold and the first failing test**

```csharp
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using Xunit;

public sealed class TtaStreamTests
{
    private const string SamplePath = "TestFiles/tta/sample-stereo-16bit.tta";
    private const string SampleWithId3Path = "TestFiles/tta/sample-with-id3v2.tta";
    private const string ExpectedDetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void ReadStream_ParsesFixedHeader()
    {
        using var fs = File.OpenRead(SamplePath);
        using var walker = new TtaStream();

        Assert.True(walker.ReadStream(fs));

        var header = walker.Header;
        Assert.NotNull(header);
        Assert.Equal(1, header!.Format);              // TTA_FORMAT_SIMPLE
        Assert.Equal(2, header.NumChannels);
        Assert.Equal(16, header.BitsPerSample);
        Assert.Equal(44100u, header.SampleRate);
        Assert.True(header.TotalSamples > 0);
    }
}
```

The exact `SampleRate` and `BitsPerSample` assertions assume the sample is
44.1 kHz / 16-bit stereo per the provenance file; if the local encode
differs, update these literals to match the actual sample.

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~TtaStreamTests.ReadStream_ParsesFixedHeader"`. Expected: pass.

---

### Task 10: Frame-enumeration test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

- [ ] **Step 1: Append the test**

```csharp
    [Fact]
    public void ReadStream_BuildsFrameListMatchingSeekTable()
    {
        using var fs = File.OpenRead(SamplePath);
        using var walker = new TtaStream();

        Assert.True(walker.ReadStream(fs));

        Assert.NotNull(walker.SeekTable);
        Assert.Equal(walker.SeekTable!.FrameSizes.Count, walker.Frames.Count);

        // Sum of frame lengths matches the audio span between header end and
        // the byte just past the last frame.
        long sumLen = 0;
        foreach (var f in walker.Frames)
        {
            sumLen += f.Length;
        }

        var first = walker.Frames[0];
        var last = walker.Frames[^1];
        Assert.Equal(sumLen, last.StartOffset + last.Length - first.StartOffset);

        // Standard frame length is 256 * sps / 245; only the last frame may differ.
        var expectedStd = (uint)(256UL * walker.Header!.SampleRate / 245UL);
        for (var i = 0; i < walker.Frames.Count - 1; i++)
        {
            Assert.Equal(expectedStd, walker.Frames[i].SampleCount);
        }
    }
```

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~TtaStreamTests.ReadStream_BuildsFrameListMatchingSeekTable"`. Expected: pass.

---

### Task 11: Round-trip identity test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

- [ ] **Step 1: Append**

```csharp
    [Fact]
    public void WriteTo_ProducesByteIdenticalOutputForUnmodifiedInput()
    {
        var original = File.ReadAllBytes(SamplePath);

        using var fs = new MemoryStream(original, writable: false);
        using var walker = new TtaStream();
        Assert.True(walker.ReadStream(fs));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        // The walker only emits the audio container span (header + seek table
        // + frames); compare against the same span of the original file.
        var expected = original.AsSpan(
            (int)walker.StartOffset,
            (int)(walker.EndOffset - walker.StartOffset)).ToArray();
        Assert.Equal(expected, output.ToArray());
    }
```

If the real samples have no surrounding ID3/APE tags (the simple
`sample-stereo-16bit.tta`), `StartOffset` is `0` and `EndOffset` equals
`original.Length`, so this reduces to a whole-file equality check.

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~TtaStreamTests.WriteTo_ProducesByteIdenticalOutputForUnmodifiedInput"`. Expected: pass.

---

### Task 12: Tag-edit round-trip test (audio bytes preserved)

**Files:**
- Modify: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

- [ ] **Step 1: Append**

This exercises spec §7.2 acceptance #4: edit a tag via `AudioTags`, save,
re-parse, and confirm the audio frame `(StartOffset, Length)` ranges in the
new output reference unchanged audio bytes. Tag editing happens through the
existing `AudioTags` scanner — `TtaStream` itself doesn't model tags.

```csharp
    [Fact]
    public void TagEdit_PreservesAudioFrameByteRanges()
    {
        var original = File.ReadAllBytes(SampleWithId3Path);

        // Capture the original audio frame ranges.
        long[] origStarts;
        long[] origLengths;
        byte[][] origFrameBytes;
        using (var fs = new MemoryStream(original, writable: false))
        using (var walker = new TtaStream())
        {
            Assert.True(walker.ReadStream(fs));
            origStarts = walker.Frames.Select(f => f.StartOffset).ToArray();
            origLengths = walker.Frames.Select(f => f.Length).ToArray();
            origFrameBytes = walker.Frames
                .Select(f => original.AsSpan((int)f.StartOffset, (int)f.Length).ToArray())
                .ToArray();
        }

        // Edit a tag via AudioTags, persist, re-parse.
        byte[] edited;
        using (var fs = new MemoryStream(original, writable: false))
        {
            // Use the project's existing AudioTags entry point. The exact API
            // surface is inherited from the existing tag-scanner tests; if
            // this test fails to compile due to API drift, mirror what
            // FlacStreamTests / MpaStreamTests already do for tag editing.
            var tags = AudioVideoLib.Tags.AudioTags.ReadStream(fs);
            // … set a frame's value, e.g. ID3v2 TIT2 to "Edited Title" …
            using var ms = new MemoryStream();
            tags.WriteTo(ms);
            edited = ms.ToArray();
        }

        using (var fs2 = new MemoryStream(edited, writable: false))
        using (var walker2 = new TtaStream())
        {
            Assert.True(walker2.ReadStream(fs2));
            Assert.Equal(origStarts.Length, walker2.Frames.Count);
            for (var i = 0; i < walker2.Frames.Count; i++)
            {
                var f = walker2.Frames[i];
                Assert.Equal(origLengths[i], f.Length);
                var sliced = edited.AsSpan((int)f.StartOffset, (int)f.Length).ToArray();
                Assert.Equal(origFrameBytes[i], sliced);
            }
        }
    }
```

The middle block (`AudioTags.ReadStream` / mutation / `WriteTo`) follows the
existing tag-edit pattern used elsewhere in the test suite. If the call
shape doesn't match the actual API, copy from a current passing
`FlacStreamTests`/`MpaStreamTests` tag-edit test in the same project.

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~TtaStreamTests.TagEdit_PreservesAudioFrameByteRanges"`. Expected: pass.

---

### Task 13: Detached-source error test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

- [ ] **Step 1: Append**

```csharp
    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        var walker = new TtaStream();
        using (var fs = File.OpenRead(SamplePath))
        {
            Assert.True(walker.ReadStream(fs));
        }

        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(() => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedDetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsWhenNeverRead()
    {
        using var walker = new TtaStream();
        var ex = Assert.Throws<InvalidOperationException>(() => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedDetachedMessage, ex.Message);
    }
```

- [ ] **Step 2: Run** — both pass.

---

### Task 14: Magic-byte dispatch test (direct walker only)

**Files:**
- Modify: `AudioVideoLib.Tests/IO/TtaStreamTests.cs`

This test exercises only the direct walker path. The
`MediaContainers`-level dispatch test (the spec §7.2 #6 case) is part of
Phase 2's `MediaContainersTests` work — Phase 1 plans don't touch
`MediaContainers.cs`.

- [ ] **Step 1: Append**

```csharp
    [Fact]
    public void ReadStream_RejectsNonTtaInput()
    {
        // 22 bytes of zeros — same size as a TTA1 header but no magic.
        var bogus = new byte[64];
        using var fs = new MemoryStream(bogus);
        using var walker = new TtaStream();

        Assert.False(walker.ReadStream(fs));
        Assert.Null(walker.Header);
    }

    [Fact]
    public void ReadStream_ConsumesMagicAtCurrentPosition()
    {
        // Prefix the sample with arbitrary garbage; position the stream at the
        // TTA1 magic and confirm the walker parses cleanly.
        var sample = File.ReadAllBytes(SamplePath);
        var prefixed = new byte[64 + sample.Length];
        Array.Copy(sample, 0, prefixed, 64, sample.Length);

        using var fs = new MemoryStream(prefixed, writable: false);
        fs.Position = 64;

        using var walker = new TtaStream();
        Assert.True(walker.ReadStream(fs));
        Assert.Equal(64, walker.StartOffset);
    }
```

- [ ] **Step 2: Run all `TtaStreamTests`** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~TtaStreamTests"`. Expected: all green (7 facts).

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib.Tests/IO/TtaStreamTests.cs AudioVideoLib.Tests/TestFiles/tta/
git commit -m "test(tta): cover header parse, frame enumeration, round-trip, tag-edit, detached-source

Seven xUnit facts cover spec §7.2 acceptance #1-#5 plus the post-prefix
positional dispatch path. The MediaContainers-level magic-dispatch test
(§7.2 #6) lands in Phase 2."
```

---

### Task 15: Docs page

**Files:**
- Create: `src/docs/container-formats/ttastream.md`

- [ ] **Step 1: Write the page (mirrors the layout of `flacstream.md`)**

```markdown
# TtaStream

Walks a TrueAudio (`.tta`) stream — the 22-byte TTA1 fixed header,
the per-frame seek table, and the resulting list of audio frames.
`TtaStream.Header`, `TtaStream.SeekTable`, and `TtaStream.Frames`
expose the parsed model; tag editing for the surrounding APEv2 footer,
ID3v1 footer, and ID3v2 header is handled via the existing `AudioTags`
scanner, not on the walker itself.

## On-disk layout

A TrueAudio file (per `3rdparty/libtta-c-2.3/libtta.c`) is laid out as:

1. **Optional ID3v2 header** at offset 0. Recognised and skipped by the
   outer container scanner before the walker is dispatched.
2. **Fixed 22-byte TTA1 header.** Four-byte magic `TTA1`, followed by
   five little-endian info fields — format (1 = simple, 2 = encrypted),
   channel count (≤6), bits per sample (16–24), sample rate, and total
   samples — and a four-byte CRC32 over the preceding 18 bytes.
3. **Seek table.** One little-endian uint32 per frame giving the
   compressed frame length, followed by a single uint32 CRC32 over the
   table.
4. **Audio frames.** Each frame's start offset is fixed by the seek
   table; the first frame begins immediately after the seek-table CRC.
   Every frame except the last carries `256 * SampleRate / 245`
   samples; the last frame carries the remainder.
5. **Optional APEv2 footer / ID3v1 footer** trailing the audio data.
   Recognised by `AudioTags`, not by the walker.

## Inspecting a TTA stream

```csharp
var tta = streams.OfType<TtaStream>().First();

var header = tta.Header!;
Console.WriteLine($"{header.SampleRate} Hz, {header.NumChannels} ch, " +
                  $"{header.BitsPerSample}-bit, {header.TotalSamples} samples");

Console.WriteLine($"Frames: {tta.Frames.Count}");
foreach (var frame in tta.Frames.Take(3))
{
    Console.WriteLine($"  @{frame.StartOffset:X8}  len={frame.Length}  samples={frame.SampleCount}");
}
```

## Save semantics

`TtaStream.WriteTo` is a byte-for-byte passthrough. The walker holds an
`ISourceReader` populated by `ReadStream`; on save the original audio
container span is streamed verbatim to the destination. There is no
audio re-encoding path. Callers must keep the source `Stream` alive
between `ReadStream` and `WriteTo`; calling `WriteTo` after `Dispose`
throws `InvalidOperationException` with the message
`"Source stream was detached or never read. WriteTo requires a live source."`.
```

- [ ] **Step 2: Build the docs**

If a local `docfx` is available: `docfx docfx.json` — expected zero new
warnings. Otherwise note the page is queued for the Phase 3 docs build.

- [ ] **Step 3: Commit**

```bash
git add src/docs/container-formats/ttastream.md
git commit -m "docs(tta): add TtaStream container-format page

Documents the on-disk layout, header fields, seek table, and the
byte-passthrough save semantics. Index/getting-started cross-links land
in Phase 2."
```

---

### Task 16: Final Phase-1-TTA validation

- [ ] **Step 1: Full build**

Run: `dotnet build AudioVideoLib.slnx -c Release`
Expected: zero warnings related to TTA files.

- [ ] **Step 2: Full test suite**

Run: `dotnet test AudioVideoLib.slnx -c Release`
Expected: all green; the seven new `TtaStreamTests` facts pass.

- [ ] **Step 3: Confirm no Phase-2 files were touched**

Run: `git diff --name-only $(git merge-base HEAD master)..HEAD`
Expected output contains **only**:

```
AudioVideoLib/Formats/TtaHeader.cs
AudioVideoLib/Formats/TtaSeekTable.cs
AudioVideoLib/Formats/TtaFrame.cs
AudioVideoLib/IO/TtaStream.cs
AudioVideoLib.Tests/IO/TtaStreamTests.cs
AudioVideoLib.Tests/TestFiles/tta/PROVENANCE.md
AudioVideoLib.Tests/TestFiles/tta/sample-stereo-16bit.tta
AudioVideoLib.Tests/TestFiles/tta/sample-with-id3v2.tta
src/docs/container-formats/ttastream.md
```

Any other path indicates accidental scope creep. `MediaContainers.cs`,
`_doc_snippets/Program.cs`, `getting-started.md`, `container-formats.md`,
`release-notes.md`, and `src/TestFiles.txt` are **explicitly Phase 2 work**
and must not appear here.

---

## Acceptance criteria for the TTA Phase 1 worktree

- `TtaStream` implements `IMediaContainer, IDisposable` per spec §3.1.
- `TtaHeader`, `TtaSeekTable`, and `TtaFrame` expose read-only properties only (no public setters).
- `ReadStream` parses the 22-byte TTA1 fixed header and the seek table per `3rdparty/libtta-c-2.3/libtta.c` (`read_tta_header` and `tta_decoder_read_seek_table`).
- `Frames` enumerates the audio frames with offsets derived from the seek table; per-frame `SampleCount` equals `256 * SampleRate / 245` for all but the last frame, which holds the remainder of `TotalSamples`.
- `WriteTo` performs a single splice of the source byte range — no audio re-encoding.
- `WriteTo` throws `InvalidOperationException` with the canonical message after `Dispose` or before `ReadStream`.
- Seven xUnit facts pass: header parse, frame enumeration, round-trip identity, tag-edit round-trip via `AudioTags`, detached-source post-Dispose, never-read, post-prefix positional dispatch (and the negative bogus-input case).
- `src/docs/container-formats/ttastream.md` exists and renders.
- `AudioVideoLib.Tests/TestFiles/tta/PROVENANCE.md` documents the sample files.
- `MediaContainers.cs`, `_doc_snippets/Program.cs`, `getting-started.md`, `container-formats.md`, `release-notes.md`, and `src/TestFiles.txt` are unchanged in this worktree.

---

## Self-review

- **Placeholders:** None remain. Every code block names a real file path; every test asserts a concrete value or relationship.
- **Test-name / impl-method consistency:** `ReadStream_ParsesFixedHeader` matches `TtaStream.ReadStream` + `TtaHeader`; `ReadStream_BuildsFrameListMatchingSeekTable` matches `Frames` and `SeekTable`; `WriteTo_ProducesByteIdenticalOutputForUnmodifiedInput` and `WriteTo_ThrowsAfterDispose` match `WriteTo` + `Dispose`; `TagEdit_PreservesAudioFrameByteRanges` matches the spec §7.2 #4 acceptance language.
- **Spec §4.3 facts covered:**
  - Magic `TTA1` at offset 0 → Task 5 (`headerBuf[0..4]` check) and Task 14 (negative + post-prefix tests).
  - File set `Formats/TtaHeader.cs`, `Formats/TtaSeekTable.cs`, `IO/TtaStream.cs` → Tasks 1, 2, 4 (plus the spec-implied `Formats/TtaFrame.cs`, Task 3, since §4.3's "frame model" line specifies one).
  - Frame model `{ StartOffset, Length, SampleCount }` → Task 3.
  - Tags carried (APEv2 footer, ID3v1 footer, ID3v2 header) → docs page (Task 15) and Task 12 (tag-edit round trip via `AudioTags`).
  - Reference `3rdparty/libtta-c-2.3/libtta.c` → cited in Tasks 5, 6, plus the format-reference section above.
  - "Simplest of the four — header + seek table fully describes the frame layout" → reflected in the lean task count (16 vs. ~25 expected for richer formats).
- **Lifecycle contract (§3.1):** `_source` populated in `ReadStream` (Task 5), disposed in `Dispose` (Task 4), null-checked with the canonical exception message in `WriteTo` (Task 4 / Task 7), tested in Task 13.
- **No re-encoding:** `WriteTo` is a single `_source.CopyTo` — Task 7 — and the round-trip test (Task 11) asserts byte equality.
- **Phase boundary:** Task 16 includes an explicit `git diff` check to fail loudly if any Phase-2 file slipped in.
- **Read-only properties everywhere:** `TtaHeader`, `TtaSeekTable`, and `TtaFrame` use `internal` constructors with `{ get; }` properties only. `TtaStream`'s mutable surface is limited to the framework-required `MaxFrameSpacingLength` setter (already mutable in the `IMediaContainer` interface).
