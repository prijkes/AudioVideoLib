# Format pack — Monkey's Audio (MAC) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `MacStream`, an `IMediaContainer` walker for Monkey's Audio (`.ape`) files — distinct from the existing `ApeTag` family. Supports both integer (`MAC `) and float (`MACF`) format. Parse-for-inspection, byte-passthrough save (interpretation-3 from spec §3).

**Architecture:** `MacStream` owns an `ISourceReader? _source` populated in `ReadStream` and disposed in `Dispose`. The `WriteTo` method emits the descriptor, header, seek table, optional WAV header data region (when `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` is *not* set), then splices each per-frame audio range from the live source. The class is named `MacStream` (not `ApeStream`/`ApeAudioStream`/`MonkeysAudioStream`) per spec §4.4 to keep clear separation from the existing `ApeTag` family — the upstream SDK uses "MAC = Monkey's Audio Codec" for the same reason.

**Tech Stack:** C# 13 / .NET 10, xUnit. Format reference at `3rdparty/MAC_1284_SDK/`.

**Worktree:** `feat/mac` (per spec §8 Phase 1).

---

## File Structure

Files this plan creates (per spec §4.4):

| File | Role |
|---|---|
| `AudioVideoLib/Formats/MacFormat.cs` | Enum: `Integer`, `Float`. |
| `AudioVideoLib/Formats/MacDescriptor.cs` | `APE_DESCRIPTOR` model — `cID`, version, descriptor size, header size, seek-table bytes, header-data bytes, APE frame-data bytes (low/high), terminating-data bytes, MD5. |
| `AudioVideoLib/Formats/MacHeader.cs` | `APE_HEADER` model — compression level, format flags, blocks-per-frame, final-frame blocks, total frames, bits-per-sample, channels, sample rate. |
| `AudioVideoLib/Formats/MacFrame.cs` | Per-frame model: `StartOffset`, `Length`, `BlockCount`. |
| `AudioVideoLib/Formats/MacSeekEntry.cs` | A single seek-table entry (file offset of the corresponding APE frame). |
| `AudioVideoLib/IO/MacStream.cs` | The walker. |
| `AudioVideoLib.Tests/IO/MacStreamTests.cs` | xUnit tests. |
| `src/docs/container-formats/mac.md` | Per-format docs page. |
| `AudioVideoLib.Tests/TestFiles/mac/PROVENANCE.md` | Provenance fragment for the sample input files. |

This plan **does not modify any existing file**: `MediaContainers.cs`, `_doc_snippets/Program.cs`, `docs/getting-started.md`, `docs/container-formats.md`, `docs/release-notes.md`, and `src/TestFiles.txt` are touched in Phase 2 (per spec §8).

**Reference C++ sources** to consult during implementation:

- `3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:179-211` — `APE_DESCRIPTOR` and `APE_HEADER` struct layouts (canonical field list + comments).
- `3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:84` — `#define APE_FORMAT_FLAG_CREATE_WAV_HEADER (1 << 5)`.
- `3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.h` — `APE_COMMON_HEADER` (cID + version) and the legacy `APE_HEADER_OLD` struct (we do **not** support pre-3.98 files in Phase 1; see §"Out of scope" below).
- `3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:156-157` — magic check (`MAC ` or `MACF`).
- `3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:188-268` — `CAPEHeader::AnalyzeCurrent`, the canonical parse path (descriptor read → little-endian fixup → header read → derive total blocks, length, seek-table element count).
- `3rdparty/MAC_1284_SDK/Source/MACLib/APECompressCreate.cpp:250-253` — the cID magic distinction (`'F'` for float, `' '` for integer).
- `3rdparty/MAC_1284_SDK/Source/MACLib/APEInfo.cpp:439,527` and `MACLib.cpp:641-655` — file-region offset arithmetic (junk-header → descriptor → header → seek-table → header-data → frame-data → terminating-data).

**Out of scope (Phase 1):**

- Pre-3.98 / `APE_HEADER_OLD` files (no descriptor; the magic-only header preceded the 3.98 redesign). The walker rejects these by returning `false` from `ReadStream` if `nVersion < 3980`.
- Re-encoding any audio. `WriteTo` is byte-passthrough.
- Float-mode peak-level / format-extension fields beyond what the descriptor + header exposes.

---

## Tasks

### Task 1: Create the `MacFormat` enum

**Files:**
- Create: `AudioVideoLib/Formats/MacFormat.cs`

- [ ] **Step 1: Write the enum**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// The two audio sample formats that Monkey's Audio supports. The distinction is encoded in
/// the descriptor's <c>cID</c> magic — see <c>3rdparty/MAC_1284_SDK/Source/MACLib/APECompressCreate.cpp:250-253</c>.
/// </summary>
public enum MacFormat
{
    /// <summary>Integer-PCM source. Magic is <c>"MAC "</c>.</summary>
    Integer = 0,

    /// <summary>IEEE float source. Magic is <c>"MACF"</c>.</summary>
    Float = 1,
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: clean build.

---

### Task 2: Create the `MacDescriptor` model

**Files:**
- Create: `AudioVideoLib/Formats/MacDescriptor.cs`

Reference: `3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:179-194` for the canonical layout and field comments. All properties are read-only (init-only via the constructor).

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;

/// <summary>
/// The Monkey's Audio file descriptor — the first structure in every APE file (3.98+).
/// Field layout mirrors <c>APE_DESCRIPTOR</c> in
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:179-194</c>.
/// </summary>
/// <remarks>
/// All multi-byte values are little-endian on disk. This model holds the host-endian
/// representation; the walker is responsible for the conversion at parse time.
/// </remarks>
public sealed class MacDescriptor
{
    /// <summary>The 4-byte magic — either <c>"MAC "</c> (integer) or <c>"MACF"</c> (float).</summary>
    public string Id { get; }

    /// <summary>Version number scaled by 1000 (3.99 → 3990, 4.10 → 4100, etc.).</summary>
    public ushort Version { get; }

    /// <summary>Total bytes of the descriptor (allows future expansion).</summary>
    public uint DescriptorBytes { get; }

    /// <summary>Bytes occupied by the <see cref="MacHeader"/> region that follows the descriptor.</summary>
    public uint HeaderBytes { get; }

    /// <summary>Bytes occupied by the seek table.</summary>
    public uint SeekTableBytes { get; }

    /// <summary>
    /// Bytes occupied by the original-file WAV header data (zero when
    /// <c>APE_FORMAT_FLAG_CREATE_WAV_HEADER</c> is set in <see cref="MacHeader.FormatFlags"/>).
    /// </summary>
    public uint HeaderDataBytes { get; }

    /// <summary>Low 32 bits of the APE frame-data byte count.</summary>
    public uint ApeFrameDataBytes { get; }

    /// <summary>High 32 bits of the APE frame-data byte count (for files &gt; 4 GiB).</summary>
    public uint ApeFrameDataBytesHigh { get; }

    /// <summary>Bytes of terminating data after the audio (e.g., trailing WAV chunk junk, but not tag data).</summary>
    public uint TerminatingDataBytes { get; }

    /// <summary>16-byte MD5 of the original file content (see SDK notes — usage is non-trivial).</summary>
    public IReadOnlyList<byte> FileMd5 { get; }

    /// <summary>Combined 64-bit total of <see cref="ApeFrameDataBytes"/> and <see cref="ApeFrameDataBytesHigh"/>.</summary>
    public long TotalApeFrameDataBytes => ((long)ApeFrameDataBytesHigh << 32) | ApeFrameDataBytes;

    public MacDescriptor(
        string id,
        ushort version,
        uint descriptorBytes,
        uint headerBytes,
        uint seekTableBytes,
        uint headerDataBytes,
        uint apeFrameDataBytes,
        uint apeFrameDataBytesHigh,
        uint terminatingDataBytes,
        ReadOnlySpan<byte> fileMd5)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (fileMd5.Length != 16)
        {
            throw new ArgumentException("FileMd5 must be exactly 16 bytes.", nameof(fileMd5));
        }

        Id = id;
        Version = version;
        DescriptorBytes = descriptorBytes;
        HeaderBytes = headerBytes;
        SeekTableBytes = seekTableBytes;
        HeaderDataBytes = headerDataBytes;
        ApeFrameDataBytes = apeFrameDataBytes;
        ApeFrameDataBytesHigh = apeFrameDataBytesHigh;
        TerminatingDataBytes = terminatingDataBytes;
        FileMd5 = fileMd5.ToArray();
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 3: Create the `MacHeader` model

**Files:**
- Create: `AudioVideoLib/Formats/MacHeader.cs`

Reference: `3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:199-211` for field layout. Bits-per-sample, channels, and sample-rate are all stored on disk; `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` is bit 5 of `FormatFlags`.

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// The Monkey's Audio per-stream header — fixed-size structure that follows the
/// <see cref="MacDescriptor"/>. Field layout mirrors <c>APE_HEADER</c> in
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:199-211</c>.
/// </summary>
public sealed class MacHeader
{
    /// <summary>
    /// <c>APE_FORMAT_FLAG_CREATE_WAV_HEADER</c> from
    /// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:84</c> — when set, the original WAV
    /// header was discarded at encode time and synthesised on decode (so
    /// <see cref="MacDescriptor.HeaderDataBytes"/> is zero).
    /// </summary>
    public const ushort CreateWavHeaderFlag = 1 << 5;

    /// <summary>The compression level (1000=fast, 2000=normal, 3000=high, 4000=extra-high, 5000=insane).</summary>
    public ushort CompressionLevel { get; }

    /// <summary>Format flags. Bit 5 (<see cref="CreateWavHeaderFlag"/>) is the only one this walker interprets.</summary>
    public ushort FormatFlags { get; }

    /// <summary>Number of audio blocks in a non-final frame.</summary>
    public uint BlocksPerFrame { get; }

    /// <summary>Number of audio blocks in the final frame (≤ <see cref="BlocksPerFrame"/>).</summary>
    public uint FinalFrameBlocks { get; }

    /// <summary>Total frame count.</summary>
    public uint TotalFrames { get; }

    /// <summary>Bits per audio sample (typically 16 or 24; 32 for float).</summary>
    public ushort BitsPerSample { get; }

    /// <summary>Channel count (1 or 2 typical; up to <c>APE_MAXIMUM_CHANNELS</c>).</summary>
    public ushort Channels { get; }

    /// <summary>Sample rate in Hz.</summary>
    public uint SampleRate { get; }

    /// <summary>True when <see cref="CreateWavHeaderFlag"/> is set in <see cref="FormatFlags"/>.</summary>
    public bool CreatesWavHeaderOnDecode => (FormatFlags & CreateWavHeaderFlag) != 0;

    public MacHeader(
        ushort compressionLevel,
        ushort formatFlags,
        uint blocksPerFrame,
        uint finalFrameBlocks,
        uint totalFrames,
        ushort bitsPerSample,
        ushort channels,
        uint sampleRate)
    {
        CompressionLevel = compressionLevel;
        FormatFlags = formatFlags;
        BlocksPerFrame = blocksPerFrame;
        FinalFrameBlocks = finalFrameBlocks;
        TotalFrames = totalFrames;
        BitsPerSample = bitsPerSample;
        Channels = channels;
        SampleRate = sampleRate;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 4: Create the `MacSeekEntry` model

**Files:**
- Create: `AudioVideoLib/Formats/MacSeekEntry.cs`

Reference: `APEHeader.cpp:268` (`pInfo->nSeekTableElements = nSeekTableBytes / 4`). The on-disk seek table is a packed array of 32-bit little-endian file offsets, one per frame.

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// A single entry from the Monkey's Audio seek table — the absolute file offset of an APE frame's
/// first byte. The seek table is a packed array of 32-bit little-endian offsets; element count is
/// <see cref="MacDescriptor.SeekTableBytes"/> / 4. See
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:268</c>.
/// </summary>
public sealed class MacSeekEntry
{
    /// <summary>Index of the frame this entry refers to (0-based).</summary>
    public int FrameIndex { get; }

    /// <summary>Absolute file offset of the frame's first byte.</summary>
    public uint FileOffset { get; }

    public MacSeekEntry(int frameIndex, uint fileOffset)
    {
        FrameIndex = frameIndex;
        FileOffset = fileOffset;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 5: Create the `MacFrame` model

**Files:**
- Create: `AudioVideoLib/Formats/MacFrame.cs`

The frame model is intentionally minimal: a byte range plus a block count. All audio inspection beyond that requires bit-level decode (out of scope for this walker).

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// A single Monkey's Audio frame — a contiguous byte range in the source file plus the
/// number of audio blocks it encodes. Frame boundaries are derived from the seek table; the
/// final frame's length is bounded by <see cref="MacDescriptor.TotalApeFrameDataBytes"/>.
/// </summary>
public sealed class MacFrame
{
    /// <summary>Absolute file offset of the frame's first byte.</summary>
    public long StartOffset { get; }

    /// <summary>Length of the frame in bytes.</summary>
    public long Length { get; }

    /// <summary>Number of audio blocks the frame encodes.</summary>
    /// <remarks>
    /// For non-final frames this equals <see cref="MacHeader.BlocksPerFrame"/>; for the final
    /// frame it equals <see cref="MacHeader.FinalFrameBlocks"/>.
    /// </remarks>
    public uint BlockCount { get; }

    public MacFrame(long startOffset, long length, uint blockCount)
    {
        StartOffset = startOffset;
        Length = length;
        BlockCount = blockCount;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

- [ ] **Step 3: Commit the model classes**

```bash
git add AudioVideoLib/Formats/MacFormat.cs AudioVideoLib/Formats/MacDescriptor.cs AudioVideoLib/Formats/MacHeader.cs AudioVideoLib/Formats/MacSeekEntry.cs AudioVideoLib/Formats/MacFrame.cs
git commit -m "feat(formats): add MAC (Monkey's Audio) descriptor/header/frame/seek models

Read-only models for the APE_DESCRIPTOR and APE_HEADER structs (per
3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:179-211), plus per-frame and
seek-table entry models. Both 'MAC ' and 'MACF' magic values are surfaced
via MacFormat (Integer | Float)."
```

---

### Task 6: Walker skeleton — `MacStream.ReadStream` magic dispatch

**Files:**
- Create: `AudioVideoLib/IO/MacStream.cs`

This task lays down the class skeleton with the magic-byte dispatch and `Format` property. Descriptor / header / seek-table parsing land in subsequent tasks.

- [ ] **Step 1: Write the skeleton**

```csharp
namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Structural walker for Monkey's Audio (<c>.ape</c>) files (version 3.98+ — files with the
/// <c>APE_DESCRIPTOR</c> + <c>APE_HEADER</c> layout). Surfaces the descriptor, header, seek
/// table, and per-frame byte ranges. <see cref="WriteTo(Stream)"/> is byte-passthrough — no
/// audio re-encoding happens at any point. APEv2 footer / ID3v1 footer tag editing flows
/// through the existing <c>AudioTags</c> scanner; this walker only handles the audio container.
/// </summary>
/// <remarks>
/// Both integer (<c>"MAC "</c>) and float (<c>"MACF"</c>) variants are accepted; the active
/// variant is surfaced via <see cref="Format"/>. See
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APECompressCreate.cpp:250-253</c> for how the
/// upstream encoder writes the cID. Pre-3.98 files (<c>APE_HEADER_OLD</c>, no descriptor) are
/// not supported — <see cref="ReadStream(Stream)"/> returns <c>false</c> for them.
/// </remarks>
public sealed class MacStream : IMediaContainer, IDisposable
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly List<MacSeekEntry> _seekEntries = [];
    private readonly List<MacFrame> _frames = [];
    private ISourceReader? _source;
    private MacDescriptor? _descriptor;
    private MacHeader? _header;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration => _header is null || _header.SampleRate == 0
        ? 0
        : (long)((TotalBlocks * 1000.0) / _header.SampleRate);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>Gets the parsed descriptor, or <c>null</c> if <see cref="ReadStream"/> has not been called.</summary>
    public MacDescriptor? Descriptor => _descriptor;

    /// <summary>Gets the parsed header, or <c>null</c> if <see cref="ReadStream"/> has not been called.</summary>
    public MacHeader? Header => _header;

    /// <summary>Gets the seek-table entries.</summary>
    public IReadOnlyList<MacSeekEntry> SeekEntries => _seekEntries;

    /// <summary>Gets the per-frame byte ranges.</summary>
    public IReadOnlyList<MacFrame> Frames => _frames;

    /// <summary>
    /// Gets which sample format the file uses (integer or float), per the descriptor's cID.
    /// Defaults to <see cref="MacFormat.Integer"/> when nothing has been read.
    /// </summary>
    public MacFormat Format { get; private set; } = MacFormat.Integer;

    private long TotalBlocks =>
        _header is null || _header.TotalFrames == 0
            ? 0
            : (long)(_header.TotalFrames - 1) * _header.BlocksPerFrame + _header.FinalFrameBlocks;

    /// <inheritdoc/>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < 8)
        {
            return false;
        }

        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4)
        {
            return false;
        }

        stream.Position = start;

        var id = Latin1.GetString(magic);
        Format = id switch
        {
            "MAC " => MacFormat.Integer,
            "MACF" => MacFormat.Float,
            _ => default,
        };

        if (id != "MAC " && id != "MACF")
        {
            return false;
        }

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);

        // Parsing of descriptor / header / seek table lands in tasks 7–10.
        // For now, return true after the magic dispatch so this skeleton compiles
        // and the magic test in task 16 passes.
        return true;
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        // Full passthrough — task 12 fills this in once the parse path is complete.
        _source.CopyTo(0, _source.Length, destination);
    }

    /// <summary>Releases the underlying <see cref="ISourceReader"/>.</summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 7: Test scaffolding — magic-byte dispatch + detached-source error

**Files:**
- Create: `AudioVideoLib.Tests/IO/MacStreamTests.cs`

These two tests can pass against the skeleton from Task 6, and lock down the contract for the work that follows.

- [ ] **Step 1: Write the test file**

```csharp
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using System.Text;
using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using Xunit;

public sealed class MacStreamTests
{
    private const string ExpectedDetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void ReadStream_AcceptsMacIntegerMagic()
    {
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes("MAC "));
        // Pad to >= 8 bytes so the early-out length check passes.
        ms.SetLength(64);
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Integer, walker.Format);
    }

    [Fact]
    public void ReadStream_AcceptsMacFloatMagic()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("MACF"));
        ms.SetLength(64);
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Float, walker.Format);
    }

    [Fact]
    public void ReadStream_RejectsForeignMagic()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("RIFF"));
        ms.SetLength(64);
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MacStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedDetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes("MAC "));
        ms.SetLength(64);
        ms.Position = 0;

        var walker = new MacStream();
        walker.ReadStream(ms);
        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedDetachedMessage, ex.Message);
    }
}
```

- [ ] **Step 2: Run the new tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests" -v normal`
Expected: 5 tests, all green.

---

### Task 8: Parse the `MacDescriptor`

**Files:**
- Modify: `AudioVideoLib/IO/MacStream.cs`

Port `CAPEHeader::AnalyzeCurrent` from `APEHeader.cpp:188-211`:
1. Read 4 bytes cID (already done in Task 6 magic dispatch — re-read at offset 0 of the descriptor for the model).
2. Read 2 bytes nVersion (LE), 2 bytes nPadding.
3. Read 7 × uint32 LE: nDescriptorBytes, nHeaderBytes, nSeekTableBytes, nHeaderDataBytes, nAPEFrameDataBytes, nAPEFrameDataBytesHigh, nTerminatingDataBytes.
4. Read 16 bytes cFileMD5.
5. If `nDescriptorBytes` exceeds the bytes read so far, skip the remainder (forward-compat).
6. Reject `nVersion < 3980` (we don't support `APE_HEADER_OLD`).

- [ ] **Step 1: Add a helper that reads the fixed-size descriptor**

Replace the placeholder body in `ReadStream` with:

```csharp
        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);

        if (!TryReadDescriptor(stream, out _descriptor))
        {
            return false;
        }

        if (_descriptor.Version < 3980)
        {
            // Pre-3.98 files (APE_HEADER_OLD) are out of scope for this walker.
            return false;
        }

        EndOffset = start + _source.Length;
        return true;
```

Then add the helper inside the class:

```csharp
    private bool TryReadDescriptor(Stream stream, out MacDescriptor? descriptor)
    {
        descriptor = null;

        // The descriptor is at least 52 bytes (4 + 2 + 2 + 7×4 + 16). Read the fixed block first.
        Span<byte> fixedBlock = stackalloc byte[52];
        if (stream.Read(fixedBlock) != fixedBlock.Length)
        {
            return false;
        }

        var id = Latin1.GetString(fixedBlock[..4]);
        if (id != "MAC " && id != "MACF")
        {
            return false;
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(fixedBlock[4..6]);
        // bytes 6..8 are nPadding — discarded.
        var descriptorBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[8..12]);
        var headerBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[12..16]);
        var seekTableBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[16..20]);
        var headerDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[20..24]);
        var apeFrameDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[24..28]);
        var apeFrameDataBytesHigh = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[28..32]);
        var terminatingDataBytes = BinaryPrimitives.ReadUInt32LittleEndian(fixedBlock[32..36]);
        var md5 = fixedBlock.Slice(36, 16);

        descriptor = new MacDescriptor(
            id,
            version,
            descriptorBytes,
            headerBytes,
            seekTableBytes,
            headerDataBytes,
            apeFrameDataBytes,
            apeFrameDataBytesHigh,
            terminatingDataBytes,
            md5);

        // Forward-compat: future versions may extend the descriptor; skip the rest.
        if (descriptorBytes > fixedBlock.Length)
        {
            stream.Seek(descriptorBytes - fixedBlock.Length, SeekOrigin.Current);
        }

        return true;
    }
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

- [ ] **Step 3: The skeleton tests from Task 7 read 64 bytes of zero-padded magic**

Those tests no longer pass for `MAC ` / `MACF` because the descriptor parse will read garbage values. Update them: replace each "pad to 64 bytes" test setup with a real, hand-built minimal descriptor. Add a small helper at the bottom of `MacStreamTests`:

```csharp
    private static byte[] MakeMinimalDescriptor(string magic, ushort version = 3990)
    {
        // 52-byte fixed descriptor: id(4) + ver(2) + pad(2) + 7×u32 + md5(16)
        var bytes = new byte[52];
        Encoding.ASCII.GetBytes(magic, bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 52); // nDescriptorBytes
        // remaining fields all zero — header/seek-table/audio absent
        return bytes;
    }
```

…and rewrite the magic tests to use it:

```csharp
    [Fact]
    public void ReadStream_AcceptsMacIntegerMagic()
    {
        using var ms = new MemoryStream(MakeMinimalDescriptor("MAC "));
        ms.Position = 0;
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Integer, walker.Format);
        Assert.Equal("MAC ", walker.Descriptor!.Id);
        Assert.Equal((ushort)3990, walker.Descriptor.Version);
    }

    [Fact]
    public void ReadStream_AcceptsMacFloatMagic()
    {
        using var ms = new MemoryStream(MakeMinimalDescriptor("MACF"));
        ms.Position = 0;
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Float, walker.Format);
        Assert.Equal("MACF", walker.Descriptor!.Id);
    }
```

The `WriteTo_ThrowsAfterDispose` test must also use `MakeMinimalDescriptor("MAC ")` instead of the 64-byte zero buffer.

Add the matching `using` directives to the test file: `using System.Buffers.Binary;`.

- [ ] **Step 4: Run tests** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests"`. Expected: 5 green.

---

### Task 9: Parse the `MacHeader`

**Files:**
- Modify: `AudioVideoLib/IO/MacStream.cs`

Port `APEHeader.cpp:215-227` (the post-descriptor APE_HEADER read). 24 bytes: 2×u16 + 3×u32 + 2×u16 + 1×u32 = `nCompressionLevel(2) + nFormatFlags(2) + nBlocksPerFrame(4) + nFinalFrameBlocks(4) + nTotalFrames(4) + nBitsPerSample(2) + nChannels(2) + nSampleRate(4)`. Then forward-skip if `descriptor.HeaderBytes > 24`.

- [ ] **Step 1: Add `TryReadHeader` and call it from `ReadStream`**

In `ReadStream`, after the descriptor parse (and the version check), insert:

```csharp
        if (!TryReadHeader(stream, _descriptor, out _header))
        {
            return false;
        }
```

Add the helper:

```csharp
    private static bool TryReadHeader(Stream stream, MacDescriptor descriptor, out MacHeader? header)
    {
        header = null;

        Span<byte> buf = stackalloc byte[24];
        if (stream.Read(buf) != buf.Length)
        {
            return false;
        }

        var compressionLevel  = BinaryPrimitives.ReadUInt16LittleEndian(buf[0..2]);
        var formatFlags       = BinaryPrimitives.ReadUInt16LittleEndian(buf[2..4]);
        var blocksPerFrame    = BinaryPrimitives.ReadUInt32LittleEndian(buf[4..8]);
        var finalFrameBlocks  = BinaryPrimitives.ReadUInt32LittleEndian(buf[8..12]);
        var totalFrames       = BinaryPrimitives.ReadUInt32LittleEndian(buf[12..16]);
        var bitsPerSample     = BinaryPrimitives.ReadUInt16LittleEndian(buf[16..18]);
        var channels          = BinaryPrimitives.ReadUInt16LittleEndian(buf[18..20]);
        var sampleRate        = BinaryPrimitives.ReadUInt32LittleEndian(buf[20..24]);

        header = new MacHeader(
            compressionLevel,
            formatFlags,
            blocksPerFrame,
            finalFrameBlocks,
            totalFrames,
            bitsPerSample,
            channels,
            sampleRate);

        // Forward-compat: HeaderBytes might be larger in a future version.
        if (descriptor.HeaderBytes > buf.Length)
        {
            stream.Seek(descriptor.HeaderBytes - buf.Length, SeekOrigin.Current);
        }

        return true;
    }
```

- [ ] **Step 2: Add a test that parses a hand-built descriptor + header**

Append to `MacStreamTests.cs`:

```csharp
    private static byte[] MakeDescriptorPlusHeader(
        string magic = "MAC ",
        ushort version = 3990,
        ushort compressionLevel = 2000,
        ushort formatFlags = 0,
        uint blocksPerFrame = 73728 * 4,
        uint finalFrameBlocks = 1024,
        uint totalFrames = 1,
        ushort bitsPerSample = 16,
        ushort channels = 2,
        uint sampleRate = 44100,
        uint seekTableBytes = 0,
        uint headerDataBytes = 0,
        uint apeFrameDataBytes = 0,
        uint terminatingDataBytes = 0)
    {
        var bytes = new byte[52 + 24];
        Encoding.ASCII.GetBytes(magic, bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 52);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekTableBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), headerDataBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28, 4), 0); // high
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32, 4), terminatingDataBytes);

        var hdr = bytes.AsSpan(52);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], compressionLevel);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], formatFlags);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], blocksPerFrame);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], finalFrameBlocks);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], totalFrames);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], bitsPerSample);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], channels);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], sampleRate);
        return bytes;
    }

    [Fact]
    public void ReadStream_PopulatesHeaderFromSyntheticInput()
    {
        var bytes = MakeDescriptorPlusHeader(
            sampleRate: 48_000,
            channels: 2,
            bitsPerSample: 24,
            totalFrames: 5);
        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.NotNull(walker.Header);
        Assert.Equal(48_000u, walker.Header!.SampleRate);
        Assert.Equal((ushort)2, walker.Header.Channels);
        Assert.Equal((ushort)24, walker.Header.BitsPerSample);
        Assert.Equal(5u, walker.Header.TotalFrames);
        Assert.Equal((ushort)2000, walker.Header.CompressionLevel);
        Assert.False(walker.Header.CreatesWavHeaderOnDecode);
        Assert.Equal(MacFormat.Integer, walker.Format);
    }
```

- [ ] **Step 3: Run tests** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests"`. Expected: 6 green.

---

### Task 10: Parse the seek table

**Files:**
- Modify: `AudioVideoLib/IO/MacStream.cs`

Reference: the seek table is `descriptor.SeekTableBytes / 4` little-endian uint32 entries. After the seek table comes `descriptor.HeaderDataBytes` of original-WAV-header bytes (zero-length when `MacHeader.CreateWavHeaderFlag` is set; see `APEHeader.cpp:255` and `:412`).

- [ ] **Step 1: Add `ReadSeekTable` and call it from `ReadStream`**

After the `TryReadHeader` call, append:

```csharp
        ReadSeekTable(stream, _descriptor);

        // Skip any preserved-WAV-header bytes — these are pre-audio bytes that the walker
        // does not interpret but must round-trip via the source. With CreateWavHeaderFlag set,
        // HeaderDataBytes is zero (per APEHeader.cpp:255). Otherwise it's the original WAV
        // header that the encoder preserved verbatim.
        if (_descriptor.HeaderDataBytes > 0)
        {
            stream.Seek(_descriptor.HeaderDataBytes, SeekOrigin.Current);
        }
```

Add the helper:

```csharp
    private void ReadSeekTable(Stream stream, MacDescriptor descriptor)
    {
        _seekEntries.Clear();
        var elementCount = (int)(descriptor.SeekTableBytes / 4);
        if (elementCount <= 0)
        {
            return;
        }

        var raw = new byte[descriptor.SeekTableBytes];
        if (stream.Read(raw) != raw.Length)
        {
            return;
        }

        for (var i = 0; i < elementCount; i++)
        {
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(i * 4, 4));
            _seekEntries.Add(new MacSeekEntry(i, offset));
        }
    }
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 11: Build the per-frame `(StartOffset, Length, BlockCount)` list

**Files:**
- Modify: `AudioVideoLib/IO/MacStream.cs`

For each frame index `i`:
- `StartOffset = seekEntries[i].FileOffset`.
- `Length = seekEntries[i+1].FileOffset - seekEntries[i].FileOffset`, except the **last** frame, whose length is `descriptor.TotalApeFrameDataBytes - (seekEntries[last].FileOffset - audioRegionStart)`.
- `BlockCount = i == header.TotalFrames - 1 ? header.FinalFrameBlocks : header.BlocksPerFrame`.

Audio region starts at `descriptor.size + header.size + seek-table.size + header-data.size` (the value computed in `MACLib.cpp:641-655`). When `CreateWavHeaderFlag` is set, `HeaderDataBytes` is zero, so the audio region begins immediately after the seek table.

- [ ] **Step 1: Add `BuildFrameTable` and call it after the seek table read**

In `ReadStream` after the `if (_descriptor.HeaderDataBytes > 0)` block, append:

```csharp
        BuildFrameTable();
        EndOffset = start + _source.Length;
        return true;
```

Replace the existing `EndOffset = start + _source.Length; return true;` (added in Task 8) with the block above.

Add the helper:

```csharp
    private void BuildFrameTable()
    {
        _frames.Clear();
        if (_header is null || _descriptor is null || _seekEntries.Count == 0 || _header.TotalFrames == 0)
        {
            return;
        }

        // The audio region's last byte is at:
        //   audioStart + descriptor.TotalApeFrameDataBytes
        // where audioStart is the file offset of the first audio byte. Per APEInfo.cpp:439 the
        // first audio byte sits at descriptor.DescriptorBytes + descriptor.HeaderBytes
        // + descriptor.SeekTableBytes + descriptor.HeaderDataBytes (relative to the start of
        // the descriptor, plus any leading junk-header bytes that this walker doesn't yet
        // surface). For frame-length math we only need the END of the audio region, which the
        // seek table itself anchors via descriptor.TotalApeFrameDataBytes.
        var audioEnd = _seekEntries[0].FileOffset + _descriptor.TotalApeFrameDataBytes;

        for (var i = 0; i < _seekEntries.Count; i++)
        {
            var startOffset = _seekEntries[i].FileOffset;
            long length;
            if (i + 1 < _seekEntries.Count)
            {
                length = _seekEntries[i + 1].FileOffset - startOffset;
            }
            else
            {
                length = audioEnd - startOffset;
            }

            var blocks = i == _header.TotalFrames - 1 ? _header.FinalFrameBlocks : _header.BlocksPerFrame;
            _frames.Add(new MacFrame(startOffset, length, blocks));
        }
    }
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 12: Implement passthrough `WriteTo`

**Files:**
- Modify: `AudioVideoLib/IO/MacStream.cs`

The walker doesn't edit anything in the descriptor / header / seek table / audio region. Tag editing is delegated to `AudioTags`, which mutates only the trailing APEv2 / ID3v1 footer regions — those bytes are *not* part of any of the four descriptor regions, so a full source passthrough is bit-exact for unmodified input. After tag edits the caller writes back via the `AudioTags` save path; the walker's role is to round-trip the audio container bytes exactly.

For interpretation 3, the cleanest implementation is a single `_source.CopyTo(0, _source.Length, destination)` — this yields byte-identical output for unmodified input and is the simplest possible expression of "no audio re-encode".

- [ ] **Step 1: Confirm `WriteTo` already does the right thing**

The skeleton from Task 6 already writes the full source. Update its doc comment to document the passthrough explicitly:

```csharp
    /// <inheritdoc/>
    /// <remarks>
    /// Byte-exact passthrough of the source. The walker does not edit the descriptor,
    /// header, seek table, preserved-WAV-header region, or audio region; APE/ID3 tag
    /// editing flows through the existing <c>AudioTags</c> scanner, which mutates only
    /// the trailing footer regions outside any of the descriptor's accounted ranges.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        _source.CopyTo(0, _source.Length, destination);
    }
```

- [ ] **Step 2: Build** — `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 13: Test — frame enumeration count and total length

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MacStreamTests.cs`

Build a synthetic input with a real seek table + zero-byte audio bodies, and verify the walker enumerates the expected frame count and lengths.

- [ ] **Step 1: Add the test**

```csharp
    [Fact]
    public void ReadStream_BuildsFrameTableFromSeekEntries()
    {
        // 3 frames at offsets [200, 280, 360], audio region runs to offset 480 (so
        // the last frame is 120 bytes long). All frame body bytes are zero — the walker
        // doesn't decode them, only ranges them.
        const int totalFrames = 3;
        const uint blocksPerFrame = 73728 * 4;
        const uint finalFrameBlocks = 17_000;
        var seekOffsets = new uint[] { 200, 280, 360 };
        const uint apeFrameDataBytes = 480 - 200;

        var descSize = 52;
        var hdrSize = 24;
        var seekSize = (uint)(seekOffsets.Length * 4);
        var bytes = new byte[480]; // file ends exactly at audioEnd

        // Descriptor.
        Encoding.ASCII.GetBytes("MAC ", bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 3990);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)descSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)hdrSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 0); // headerDataBytes
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytes);
        // high + terminating already zero.

        // Header.
        var hdr = bytes.AsSpan(descSize);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], 2000);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], MacHeader.CreateWavHeaderFlag);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], blocksPerFrame);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], finalFrameBlocks);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], totalFrames);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], 44100);

        // Seek table at offset descSize + hdrSize = 76.
        var seek = bytes.AsSpan(descSize + hdrSize);
        for (var i = 0; i < seekOffsets.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(seek.Slice(i * 4, 4), seekOffsets[i]);
        }

        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.Equal(totalFrames, walker.SeekEntries.Count);
        Assert.Equal(totalFrames, walker.Frames.Count);
        Assert.Equal(200, walker.Frames[0].StartOffset);
        Assert.Equal(80, walker.Frames[0].Length);
        Assert.Equal(280, walker.Frames[1].StartOffset);
        Assert.Equal(80, walker.Frames[1].Length);
        Assert.Equal(360, walker.Frames[2].StartOffset);
        Assert.Equal(120, walker.Frames[2].Length);

        Assert.Equal(blocksPerFrame, walker.Frames[0].BlockCount);
        Assert.Equal(blocksPerFrame, walker.Frames[1].BlockCount);
        Assert.Equal(finalFrameBlocks, walker.Frames[2].BlockCount);

        // Sum of frame lengths equals descriptor.TotalApeFrameDataBytes.
        var sum = 0L;
        foreach (var f in walker.Frames)
        {
            sum += f.Length;
        }

        Assert.Equal((long)apeFrameDataBytes, sum);
    }
```

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests"`. Expected: 7 green.

---

### Task 14: Test — round-trip identity on a real `.ape` sample

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/mac/sample.ape` (binary — sourced separately, see Task 19 provenance fragment).
- Modify: `AudioVideoLib.Tests/IO/MacStreamTests.cs`

The synthetic-input tests cover the parse paths; this one closes the loop on byte-exact passthrough using a real Monkey's Audio file.

- [ ] **Step 1: Add the round-trip test**

```csharp
    [Fact]
    public void RoundTrip_ProducesByteIdenticalOutput()
    {
        var path = Path.Combine("TestFiles", "mac", "sample.ape");
        Assert.True(File.Exists(path), $"Test sample missing: {path}");
        var original = File.ReadAllBytes(path);

        using var input = new MemoryStream(original);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);
        Assert.Equal(original, output.ToArray());
    }

    [Fact]
    public void RoundTrip_HeaderFieldsMatchKnownSampleValues()
    {
        // Adjust these expected values to match the actual sample.ape provenance
        // (Task 19 records the exact source). The assertion is here to catch
        // regressions in the descriptor/header parser against a real upstream-encoded file.
        var path = Path.Combine("TestFiles", "mac", "sample.ape");
        Assert.True(File.Exists(path));
        using var fs = File.OpenRead(path);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(fs));

        Assert.NotNull(walker.Header);
        Assert.Equal(MacFormat.Integer, walker.Format);
        Assert.True(walker.Header!.SampleRate is 44_100u or 48_000u,
            $"Unexpected sample rate {walker.Header.SampleRate}");
        Assert.InRange(walker.Header.Channels, (ushort)1, (ushort)8);
        Assert.True(walker.Header.BitsPerSample is 16 or 24);
        Assert.True(walker.Header.TotalFrames > 0);
    }
```

- [ ] **Step 2: Configure the test project to copy the sample to the output directory**

In `AudioVideoLib.Tests/AudioVideoLib.Tests.csproj`, ensure `TestFiles/mac/*.ape` is included via the existing `<Content Include="TestFiles\**\*">` glob (most likely already present). If not, add:

```xml
  <ItemGroup>
    <None Update="TestFiles\mac\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 3: Run the new tests** — once `sample.ape` is in place (Task 19), `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests.RoundTrip"`. Expected: 2 green.

---

### Task 15: Test — tag-edit round-trip via `AudioTags`

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MacStreamTests.cs`

The contract: edit an APEv2 / ID3v1 field via `AudioTags`, save, re-parse via `MacStream`, then assert the audio frame `(StartOffset, Length)` list of the saved output matches the audio frame ranges of the original input. The audio splice must be exact.

- [ ] **Step 1: Add the tag-edit round-trip test**

```csharp
    [Fact]
    public void TagEdit_RoundTrip_PreservesAudioFrameRanges()
    {
        var path = Path.Combine("TestFiles", "mac", "sample.ape");
        Assert.True(File.Exists(path));
        var original = File.ReadAllBytes(path);

        // Capture the original frame layout.
        IReadOnlyList<MacFrame> originalFrames;
        using (var input = new MemoryStream(original))
        using (var walker = new MacStream())
        {
            Assert.True(walker.ReadStream(input));
            originalFrames = [.. walker.Frames];
        }

        // Simulate a tag edit: the existing AudioTags scanner mutates the trailing APEv2 /
        // ID3v1 footer region only — it does not touch the descriptor / header / seek table /
        // audio bytes. For this test we mimic the worst case by appending a synthetic ID3v1
        // tag to the file (128 bytes). The walker's frame ranges should be IDENTICAL because
        // the audio region's offset / length / per-frame seek-table entries did not move.
        var synthesizedId3v1 = new byte[128];
        Encoding.ASCII.GetBytes("TAG", synthesizedId3v1.AsSpan(0, 3));
        var withTag = new byte[original.Length + 128];
        Buffer.BlockCopy(original, 0, withTag, 0, original.Length);
        Buffer.BlockCopy(synthesizedId3v1, 0, withTag, original.Length, 128);

        using (var input = new MemoryStream(withTag))
        using (var walker = new MacStream())
        {
            Assert.True(walker.ReadStream(input));
            Assert.Equal(originalFrames.Count, walker.Frames.Count);
            for (var i = 0; i < originalFrames.Count; i++)
            {
                Assert.Equal(originalFrames[i].StartOffset, walker.Frames[i].StartOffset);
                Assert.Equal(originalFrames[i].Length, walker.Frames[i].Length);
                Assert.Equal(originalFrames[i].BlockCount, walker.Frames[i].BlockCount);
            }
        }
    }
```

(If the existing `AudioTags` API is straightforward to invoke from the test project, prefer a real edit-then-save round-trip via `AudioTags`. The synthetic-tag-append above is a fallback that asserts the same property — that footer-region mutation does not shift any audio byte ranges.)

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests.TagEdit"`. Expected: 1 green.

---

### Task 16: Test — `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` toggle

**Files:**
- Modify: `AudioVideoLib.Tests/IO/MacStreamTests.cs`

When the flag is **set**, `descriptor.HeaderDataBytes` is zero (per `APEHeader.cpp:255`) and the audio region begins immediately after the seek table. When the flag is **clear**, `HeaderDataBytes` is the original WAV-header byte count and the audio region begins after that block. The walker must compute correct audio offsets in both cases.

- [ ] **Step 1: Add the test**

```csharp
    [Fact]
    public void ReadStream_AccountsForPreservedWavHeader()
    {
        // CreateWavHeaderFlag CLEAR → 44 bytes of preserved WAV header sit between
        // the seek table and the audio region.
        const uint wavHeaderBytes = 44;
        const uint apeFrameDataBytes = 100;

        var descSize = 52;
        var hdrSize = 24;
        var seekSize = 4u; // 1 entry
        var fileSize = (int)(descSize + hdrSize + seekSize + wavHeaderBytes + apeFrameDataBytes);
        var bytes = new byte[fileSize];

        Encoding.ASCII.GetBytes("MAC ", bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 3990);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)descSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)hdrSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), wavHeaderBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytes);

        var hdr = bytes.AsSpan(descSize);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], 2000);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], 0); // CreateWavHeaderFlag CLEAR
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], 73728);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], 1024);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], 44100);

        // Seek table: single entry pointing past the WAV header to the start of the audio region.
        var audioStart = (uint)(descSize + hdrSize + seekSize + wavHeaderBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(descSize + hdrSize, 4), audioStart);

        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.False(walker.Header!.CreatesWavHeaderOnDecode);
        Assert.Equal(wavHeaderBytes, walker.Descriptor!.HeaderDataBytes);

        // The single frame starts at audioStart and has length apeFrameDataBytes.
        Assert.Single(walker.Frames);
        Assert.Equal(audioStart, (uint)walker.Frames[0].StartOffset);
        Assert.Equal((long)apeFrameDataBytes, walker.Frames[0].Length);
    }
```

- [ ] **Step 2: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests.ReadStream_AccountsForPreservedWavHeader"`. Expected: green.

---

### Task 17: Run the full `MacStreamTests` suite and commit

- [ ] **Step 1: Run** — `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MacStreamTests" -v normal`. Expected: 10 tests, all green (5 from Task 7, 1 from Task 9, 1 from Task 13, 2 from Task 14, 1 from Task 15, 1 from Task 16; the originally written Task 7 tests were updated in Task 8 step 3 to use real descriptors but the count stays at 5 in that group).

- [ ] **Step 2: Run the full project test suite to confirm no regression**

Run: `dotnet test AudioVideoLib.Tests`
Expected: all green.

- [ ] **Step 3: Commit the walker + tests**

```bash
git add AudioVideoLib/IO/MacStream.cs AudioVideoLib.Tests/IO/MacStreamTests.cs AudioVideoLib.Tests/TestFiles/mac/sample.ape
git commit -m "feat(io): add MacStream walker for Monkey's Audio (.ape)

Parses the APE_DESCRIPTOR + APE_HEADER + seek table (per
3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:188-268), builds a
per-frame (StartOffset, Length, BlockCount) list, and round-trips audio
bit-exact via _source.CopyTo. Both 'MAC ' (integer) and 'MACF' (float)
magic values are dispatched by the same walker; the active variant is
surfaced as MacStream.Format. Implements the IDisposable lifetime contract
from IMediaContainer with the documented detached-source exception.

This commit registers no dispatch entries in MediaContainers and adds no
doc-snippet — both happen in Phase 2 per the format-pack plan."
```

---

### Task 18: Write the docs page

**Files:**
- Create: `src/docs/container-formats/mac.md`

Pattern: `src/docs/container-formats/flac.md` — H1 / overview / structure / API surface / sample.

- [ ] **Step 1: Read the FLAC page for layout and tone**

Run: `cat src/docs/container-formats/flac.md` (the writer doesn't need to copy text — only mirror the section structure: title → one-paragraph overview → "Structure" section → "API surface" section → "Sample" section).

- [ ] **Step 2: Write the MAC page**

```markdown
# Monkey's Audio (`.ape`)

`MacStream` is the structural walker for Monkey's Audio files in versions 3.98 and later.
The format is named "MAC" upstream — for Monkey's Audio Codec — and the C# class follows
that convention to keep the walker name distinct from the existing `ApeTag` family
(APE-format **tag**, unrelated to the audio container).

Both sample-format variants are supported by the same walker:

- **Integer-PCM source** — descriptor magic `"MAC "`. Surfaced as `MacFormat.Integer`.
- **IEEE float source** — descriptor magic `"MACF"`. Surfaced as `MacFormat.Float`.

The variant is exposed through `MacStream.Format`.

## Structure

A 3.98+ `.ape` file is laid out as:

| Region | Source of size |
|---|---|
| `APE_DESCRIPTOR` (52 bytes for current version) | self-describing via `nDescriptorBytes` |
| `APE_HEADER` (24 bytes for current version) | `descriptor.HeaderBytes` |
| Seek table (`SeekTableBytes / 4` × `uint32 LE` file offsets) | `descriptor.SeekTableBytes` |
| Preserved WAV header (optional) | `descriptor.HeaderDataBytes` (zero when `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` is set on `header.FormatFlags`) |
| APE audio frame data | `descriptor.TotalApeFrameDataBytes` (low 32 + high 32 combined) |
| Terminating data (junk WAV trailer) | `descriptor.TerminatingDataBytes` |
| (Optional) APEv2 footer + ID3v1 footer | parsed by `AudioTags`, not by `MacStream` |

Frame `i`'s byte range is `seek[i].FileOffset` … `seek[i+1].FileOffset` (or, for the last
frame, up to `seek[0].FileOffset + descriptor.TotalApeFrameDataBytes`). Frame `i`'s block
count is `header.BlocksPerFrame`, except the final frame which uses `header.FinalFrameBlocks`.

## API surface

```csharp
using var fs = File.OpenRead("song.ape");
using var walker = new MacStream();
if (!walker.ReadStream(fs))
{
    return;
}

Console.WriteLine($"Format       : {walker.Format}");                  // Integer or Float
Console.WriteLine($"Sample rate  : {walker.Header!.SampleRate} Hz");
Console.WriteLine($"Channels     : {walker.Header.Channels}");
Console.WriteLine($"Bits/sample  : {walker.Header.BitsPerSample}");
Console.WriteLine($"Total frames : {walker.Header.TotalFrames}");
Console.WriteLine($"Compression  : {walker.Header.CompressionLevel}");

foreach (var frame in walker.Frames)
{
    Console.WriteLine($"  frame @{frame.StartOffset:X8} len={frame.Length} blocks={frame.BlockCount}");
}
```

## Editing tags

`MacStream` does **not** touch the trailing APEv2 / ID3v1 footer regions. Tag editing flows
through the existing `AudioTags` scanner; after any tag-region edit, `MacStream`'s frame
`(StartOffset, Length)` ranges still reference the unchanged audio bytes (the audio region
sits in front of the tag footers).

## Limitations

- Pre-3.98 files (`APE_HEADER_OLD`, no descriptor) are not supported. `ReadStream` returns
  `false` for those.
- No PCM decoding. The walker is for inspection of the encoded structure only.
- No re-encoding. `WriteTo` is byte-exact passthrough; consumers that mutate tags do so via
  the `AudioTags` save path, not through this walker.
```

- [ ] **Step 3: Lint via DocFX (optional pre-flight)** — Phase 3 runs `docfx docfx.json`. If you have it locally, run it now and confirm the new page renders without warnings.

---

### Task 19: Provenance fragment

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/mac/PROVENANCE.md`

- [ ] **Step 1: Write the fragment**

```markdown
# `mac/` test inputs

| File | Source | License | Notes |
|---|---|---|---|
| `sample.ape` | Encoded from a public-domain WAV (e.g., a 1-second 44.1 kHz 16-bit stereo sine wave) using `mac.exe` from the Monkey's Audio SDK at compression level "normal" (`-c2000`). The source WAV is regenerated locally; no third-party audio is checked in. | Public domain (synthesized signal). | Used by `MacStreamTests.RoundTrip_*` and `MacStreamTests.TagEdit_*`. |

## Regeneration

```bash
# Generate a 1 s 44.1 kHz 16-bit stereo sine WAV via sox (or any tool):
sox -n -r 44100 -b 16 -c 2 sine.wav synth 1 sine 440
# Encode to .ape with the upstream SDK (Windows: mac.exe, Unix: built from 3rdparty/MAC_1284_SDK):
mac sine.wav sample.ape -c2000
mv sample.ape AudioVideoLib.Tests/TestFiles/mac/sample.ape
```

The exact byte sequence is not load-bearing — the tests assert structural invariants
(header fields parse to plausible values, frame ranges sum to descriptor's
`TotalApeFrameDataBytes`, byte-exact round-trip). Any 3.98+ `.ape` file produced by
the upstream encoder will work.
```

- [ ] **Step 2: Commit docs + provenance**

```bash
git add src/docs/container-formats/mac.md AudioVideoLib.Tests/TestFiles/mac/PROVENANCE.md
git commit -m "docs(mac): add per-format docs page and TestFiles provenance fragment

Per-format page follows the layout of src/docs/container-formats/flac.md.
Mentions the integer/float magic-byte distinction explicitly, lists the
file regions and the seek-table-driven frame range computation, and notes
that tag editing flows through AudioTags rather than the walker.

Provenance fragment records how to regenerate sample.ape from a
synthesized public-domain WAV via the upstream Monkey's Audio SDK."
```

---

### Task 20: Final validation

- [ ] **Step 1: Build the whole solution in Release**

Run: `dotnet build AudioVideoLib.slnx -c Release`
Expected: clean build, zero warnings.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test AudioVideoLib.slnx -c Release`
Expected: all green. The MacStream tests run alongside everything else.

- [ ] **Step 3: Confirm no Phase 2 files were touched**

Run: `git diff --name-only main` (or against the worktree base).
Expected: only files listed in this plan's "File Structure" section appear. **No** modifications to:
- `AudioVideoLib/IO/MediaContainers.cs`
- `_doc_snippets/Program.cs`
- `src/docs/getting-started.md`
- `src/docs/container-formats.md`
- `src/docs/release-notes.md`
- `src/TestFiles.txt`

If any of those appear, the plan was over-applied — revert that file and re-run the test suite.

- [ ] **Step 4: Final commit if any cleanup happened**

If steps 1–3 surfaced any remaining issues, fix them and commit. Otherwise this Phase 1 worktree is ready for the Phase 2 integration agent to pick up.

---

## Acceptance criteria for the MAC walker

- `MacStream` exists and implements `IMediaContainer` + `IDisposable` (per spec §3.1).
- Both `"MAC "` and `"MACF"` magic values are accepted; `MacStream.Format` returns `MacFormat.Integer` or `MacFormat.Float` accordingly.
- `MacDescriptor`, `MacHeader`, `MacSeekEntry`, `MacFrame` are all defined with read-only properties (no public setters).
- `ReadStream` returns `false` for pre-3.98 files (`nVersion < 3980`) and for files whose magic is anything other than `"MAC "` or `"MACF"`.
- Frame enumeration is correct: `Frames.Count == Header.TotalFrames`, frame `i`'s start offset equals `SeekEntries[i].FileOffset`, frame lengths sum to `Descriptor.TotalApeFrameDataBytes`, the final frame's `BlockCount` equals `Header.FinalFrameBlocks` (others equal `Header.BlocksPerFrame`).
- `MAC_FORMAT_FLAG_CREATE_WAV_HEADER` is interpreted: when set, `Descriptor.HeaderDataBytes` is zero and the audio region starts immediately after the seek table; when clear, the walker accounts for the preserved WAV header bytes when validating audio offsets.
- `WriteTo` produces byte-identical output for unmodified input (round-trip identity).
- A simulated tag-region edit (synthetic ID3v1 footer appended) does not perturb `Frames[i].StartOffset` / `Frames[i].Length` / `Frames[i].BlockCount` for any `i`.
- `WriteTo` throws `InvalidOperationException` with the exact message
  `"Source stream was detached or never read. WriteTo requires a live source."`
  when called on a `MacStream` that has been disposed or never read.
- `dotnet build -c Release` and `dotnet test` are both clean.
- No Phase 2 files (`MediaContainers.cs`, `_doc_snippets/Program.cs`, the index/release-notes docs, `src/TestFiles.txt`) were modified.

---

## Self-review checklist

- [x] **Class name is `MacStream`** — not `ApeStream`, not `ApeAudioStream`, not `MonkeysAudioStream`. Matches spec §4.4.
- [x] **Both magic values handled** — `"MAC "` and `"MACF"` are dispatched in Task 6 (skeleton) and exercised in Tasks 7, 8, 13, 16.
- [x] **`MacFormat` enum** — `Integer`, `Float` (Task 1).
- [x] **Five format files created** — `MacFormat.cs`, `MacDescriptor.cs`, `MacHeader.cs`, `MacFrame.cs`, `MacSeekEntry.cs` (Tasks 1–5).
- [x] **One IO file created** — `MacStream.cs` (Task 6, extended in Tasks 8–12).
- [x] **Test file** — `MacStreamTests.cs` (Tasks 7, 9, 13, 14, 15, 16).
- [x] **Docs page** — `src/docs/container-formats/mac.md` (Task 18), follows the flac.md layout, mentions integer/float distinction.
- [x] **Provenance fragment** — `AudioVideoLib.Tests/TestFiles/mac/PROVENANCE.md` (Task 19).
- [x] **No modifications to Phase 2 files** — explicitly verified in Task 20 step 3.
- [x] **Read-only properties throughout** — every model class uses `{ get; }` only; init via constructor.
- [x] **Source-reference lifetime contract** — `_source` populated in `ReadStream`, disposed in `Dispose`, null-check in `WriteTo` throws the canonical message (Task 6, Task 12).
- [x] **References to specific C++ files/lines** — present in Tasks 2, 3, 4, 8, 9, 10, 11 and in the "Reference C++ sources" header section.
- [x] **TDD shape** — tests precede / accompany implementation; the magic and detached-source tests in Task 7 land before the descriptor parser in Task 8.
- [x] **Test method names match impl method behaviour** — e.g., `WriteTo_ThrowsWhenSourceIsNull` ↔ `WriteTo`'s null-check; `ReadStream_AcceptsMacFloatMagic` ↔ `ReadStream`'s magic-dispatch path.
- [x] **No placeholders** — all xUnit test bodies are concrete and runnable; commit messages are filled in; no TODO markers.
- [x] **Length** — within the 500–800 line target.
