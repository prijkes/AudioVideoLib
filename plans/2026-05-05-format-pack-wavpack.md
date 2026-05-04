# Format pack — WavPack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `WavPackStream`, an `IMediaContainer` walker for WavPack (`.wv`) files with parse-for-inspection and byte-passthrough save (interpretation-3 from the spec §3).

**Architecture:** `WavPackStream` enumerates fixed-size 32-byte WavPack block headers (`wvpk` magic at every block start), then walks each block's payload as a sequence of variable-length sub-block records (id-prefixed metadata). All audio bytes splice from the source via `ISourceReader.CopyTo` on save; sub-block payloads are summarised by `(Id, Size, Offset)` and read on demand. Mirrors `Mp4Stream`'s lifecycle: `_source` is populated in `ReadStream` and disposed in `Dispose`.

**Tech Stack:** C# 13 / .NET 10, xUnit. Format reference at `3rdparty/WavPack/`.

**Worktree:** `feat/wavpack` (per spec §8 Phase 1).

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib/Formats/WavPackBlockHeader.cs` | Create — 32-byte block header model (read-only). |
| `AudioVideoLib/Formats/WavPackSubBlock.cs` | Create — sub-block summary (ID, size, offset) + payload accessor. |
| `AudioVideoLib/Formats/WavPackBlock.cs` | Create — combined block model: `Header`, `StartOffset`, `Length`, `IReadOnlyList<WavPackSubBlock> SubBlocks`. |
| `AudioVideoLib/IO/WavPackStream.cs` | Create — the walker. |
| `AudioVideoLib.Tests/IO/WavPackStreamTests.cs` | Create — xUnit tests. |
| `src/docs/container-formats/wavpack.md` | Create — docs page. |
| `AudioVideoLib.Tests/TestFiles/wavpack/PROVENANCE.md` | Create — provenance fragment. |

This plan modifies **no existing files**. Registry / snippets / index updates land in Phase 2.

**Phase 0 assumed complete.** `IMediaContainer` already extends `IDisposable`; the canonical detached-source message is `"Source stream was detached or never read. WriteTo requires a live source."` — copy it verbatim.

---

## Tasks

### Task 1: Create the worktree and verify the baseline build

**Files:**
- None modified yet.

- [ ] **Step 1: Create the worktree**

```bash
git worktree add ../audiovideolib-wavpack feat/wavpack
cd ../audiovideolib-wavpack/src
```

- [ ] **Step 2: Confirm Phase 0 has landed on the base branch**

`grep -n "IDisposable" AudioVideoLib/IO/IMediaContainer.cs` should show `public interface IMediaContainer : IDisposable`. If it does not, stop and execute the Phase 0 plan first.

- [ ] **Step 3: Confirm a clean build**

Run: `dotnet build AudioVideoLib.slnx -c Debug`
Expected: build succeeds with no errors.

---

### Task 2: Add a TestFiles provenance fragment

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/wavpack/PROVENANCE.md`

- [ ] **Step 1: Create the fragment**

```markdown
# WavPack TestFiles provenance

| File | Encoder | Source | Notes |
|---|---|---|---|
| `sample-stereo-44100-16.wv` | wavpack 5.6.0 (`-h`, default) | self-generated 0.25 s 440 Hz sine, stereo, 44.1 kHz, 16-bit | hybrid mode off, no `.wvc` companion |
| `sample-mono-48000-24.wv` | wavpack 5.6.0 (`-h`) | self-generated 0.25 s 880 Hz sine, mono, 48 kHz, 24-bit | exercises `MONO_FLAG` and `BYTES_STORED == 3` |
| `sample-with-apev2.wv` | wavpack 5.6.0 + APEv2 footer | `sample-stereo-44100-16.wv` with an APEv2 tag added via `wvtag` | exercises post-audio APE footer round-trip |

Phase 2 will reference these fragments from `src/TestFiles.txt`. Do not edit `src/TestFiles.txt` from this plan.
```

- [ ] **Step 2: Commit**

```bash
git add AudioVideoLib.Tests/TestFiles/wavpack/PROVENANCE.md
git commit -m "docs(wavpack): add TestFiles provenance fragment"
```

The actual `.wv` sample bytes are added later, in Task 14.

---

### Task 3: Create `WavPackBlockHeader` skeleton (failing build)

**Files:**
- Create: `AudioVideoLib/Formats/WavPackBlockHeader.cs`

This is the read-only model for the 32-byte preamble at the start of every WavPack block. Field layout from `3rdparty/WavPack/include/wavpack.h:69-76` (`WavpackHeader` struct, format string `"4LS2LLLLL"` = 4 char + 4-byte LE uint + 2-byte LE int16 + 2 single bytes + 5 LE uint32 = 32 bytes total).

- [ ] **Step 1: Write the class skeleton**

```csharp
namespace AudioVideoLib.Formats;

using System;

/// <summary>
/// 32-byte WavPack block header preamble. Layout per
/// <c>3rdparty/WavPack/include/wavpack.h:69-76</c> (<c>WavpackHeader</c> struct,
/// format <c>"4LS2LLLLL"</c>).
/// </summary>
/// <remarks>
/// WavPack files are a flat concatenation of blocks; every block begins with this
/// header. The walker reads the header to discover block size (so it can seek to
/// the next block) and to interpret the flags bitfield (sample rate index, channel
/// count, bytes-per-sample). The 40-bit <see cref="BlockIndex"/> and
/// <see cref="TotalSamples"/> fields combine the low 32-bit field with the high
/// 8-bit "u8" extension byte, matching the <c>GET_BLOCK_INDEX</c> /
/// <c>GET_TOTAL_SAMPLES</c> macros in <c>wavpack.h:82-95</c>.
/// </remarks>
public sealed class WavPackBlockHeader
{
    /// <summary>Size of the on-disk header in bytes (always 32).</summary>
    public const int Size = 32;

    /// <summary>Four-byte chunk ID; always <c>"wvpk"</c> for valid blocks.</summary>
    public string CkId { get; init; } = "wvpk";

    /// <summary>
    /// Block payload size in bytes — i.e., <c>ckSize</c>. The total on-disk block size
    /// is <c>CkSize + 8</c> because <c>ckSize</c> excludes the leading 8 bytes of the
    /// header (per the WavPack file-format documentation referenced in
    /// <c>wavpack.h:62-67</c>).
    /// </summary>
    public uint CkSize { get; init; }

    /// <summary>Stream version. Library handles 0x402..0x410 per <c>wavpack.h:146-147</c>.</summary>
    public ushort Version { get; init; }

    /// <summary>High 8 bits of the 40-bit block index.</summary>
    public byte BlockIndexHigh { get; init; }

    /// <summary>High 8 bits of the 40-bit total-samples field.</summary>
    public byte TotalSamplesHigh { get; init; }

    /// <summary>
    /// Low 32 bits of total samples; combined with <see cref="TotalSamplesHigh"/> per
    /// <c>GET_TOTAL_SAMPLES</c> (<c>wavpack.h:94</c>). A value of <c>0xFFFFFFFF</c> in
    /// the low 32 bits signals "unknown" — surfaced as <see cref="TotalSamples"/> == -1.
    /// </summary>
    public uint TotalSamplesLow { get; init; }

    /// <summary>Low 32 bits of the block-index field.</summary>
    public uint BlockIndexLow { get; init; }

    /// <summary>Number of audio samples encoded in this block.</summary>
    public uint BlockSamples { get; init; }

    /// <summary>Flags bitfield. See <see cref="WavPackBlockFlags"/>.</summary>
    public uint Flags { get; init; }

    /// <summary>Block CRC.</summary>
    public uint Crc { get; init; }

    /// <summary>Combined 40-bit block index per <c>GET_BLOCK_INDEX</c>.</summary>
    public long BlockIndex => BlockIndexLow + ((long)BlockIndexHigh << 32);

    /// <summary>
    /// Combined 40-bit total-samples value, or <c>-1</c> if unknown
    /// (low 32 bits == <c>0xFFFFFFFF</c>) per <c>GET_TOTAL_SAMPLES</c>.
    /// </summary>
    public long TotalSamples => TotalSamplesLow == 0xFFFFFFFFu
        ? -1L
        : (long)TotalSamplesLow + ((long)TotalSamplesHigh << 32) - TotalSamplesHigh;

    /// <summary>Bytes per sample (1-4) decoded from the <c>BYTES_STORED</c> field — <c>(flags &amp; 3) + 1</c>.</summary>
    public int BytesPerSample => (int)(Flags & 0x3u) + 1;

    /// <summary>True when the block represents mono audio (<c>MONO_FLAG</c>, <c>wavpack.h:111</c>).</summary>
    public bool IsMono => (Flags & 0x4u) != 0;

    /// <summary>True when the block is from a hybrid-mode encoding (<c>HYBRID_FLAG</c>, <c>wavpack.h:112</c>).</summary>
    public bool IsHybrid => (Flags & 0x8u) != 0;

    /// <summary>True when the block stores IEEE 32-bit float samples (<c>FLOAT_DATA</c>, <c>wavpack.h:116</c>).</summary>
    public bool IsFloat => (Flags & 0x80u) != 0;

    /// <summary>True when this is the initial block of a multichannel segment (<c>INITIAL_BLOCK</c>).</summary>
    public bool IsInitialBlock => (Flags & 0x800u) != 0;

    /// <summary>True when this is the final block of a multichannel segment (<c>FINAL_BLOCK</c>).</summary>
    public bool IsFinalBlock => (Flags & 0x1000u) != 0;

    /// <summary>True when the block is encoded DSD (<c>DSD_FLAG</c>, <c>wavpack.h:141</c>).</summary>
    public bool IsDsd => (Flags & 0x80000000u) != 0;

    /// <summary>
    /// Sample rate in Hz, or <c>0</c> if the rate index is the reserved "non-standard" value (15).
    /// Decoded from the <c>SRATE_MASK</c> bits at <c>SRATE_LSB == 23</c> per
    /// <c>wavpack.h:131-132</c>; index table from <c>3rdparty/WavPack/src/common_utils.c:31-32</c>.
    /// </summary>
    public int SampleRate
    {
        get
        {
            var idx = (int)((Flags >> 23) & 0xFu);
            return idx switch
            {
                0 => 6000,
                1 => 8000,
                2 => 9600,
                3 => 11025,
                4 => 12000,
                5 => 16000,
                6 => 22050,
                7 => 24000,
                8 => 32000,
                9 => 44100,
                10 => 48000,
                11 => 64000,
                12 => 88200,
                13 => 96000,
                14 => 192000,
                _ => 0, // index 15 = "non-standard rate"; carried in an ID_SAMPLE_RATE sub-block.
            };
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj -c Debug`
Expected: succeeds.

---

### Task 4: Add `WavPackBlockHeader.Parse(ReadOnlySpan<byte>)`

**Files:**
- Modify: `AudioVideoLib/Formats/WavPackBlockHeader.cs`

Port the field-decoding portion of `3rdparty/WavPack/src/open_utils.c:read_next_header` (lines 951-984). That function reads 32 bytes, validates `ckID == 'wvpk'`, then runs `WavpackLittleEndianToNative` on the struct (i.e., LE decode of every multi-byte field). We reproduce the LE decode and the magic check; the brute-force resync loop is not needed because the walker controls block boundaries explicitly.

- [ ] **Step 1: Add the static parser**

Append inside the class:

```csharp
    /// <summary>
    /// Parses the 32-byte preamble. Returns <c>null</c> if <paramref name="span"/> is
    /// shorter than <see cref="Size"/> or its first four bytes are not the ASCII
    /// magic <c>"wvpk"</c>.
    /// </summary>
    /// <remarks>
    /// Ports the LE field-decoding portion of
    /// <c>3rdparty/WavPack/src/open_utils.c:read_next_header</c> (lines 951-984).
    /// The brute-force resync loop in that function is the caller's responsibility;
    /// this method only validates the four magic bytes and decodes the struct.
    /// </remarks>
    public static WavPackBlockHeader? Parse(ReadOnlySpan<byte> span)
    {
        if (span.Length < Size)
        {
            return null;
        }

        if (span[0] != (byte)'w' || span[1] != (byte)'v' ||
            span[2] != (byte)'p' || span[3] != (byte)'k')
        {
            return null;
        }

        return new WavPackBlockHeader
        {
            CkId = "wvpk",
            CkSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[4..8]),
            Version = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(span[8..10]),
            BlockIndexHigh = span[10],
            TotalSamplesHigh = span[11],
            TotalSamplesLow = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[12..16]),
            BlockIndexLow = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[16..20]),
            BlockSamples = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[20..24]),
            Flags = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[24..28]),
            Crc = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span[28..32]),
        };
    }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

---

### Task 5: Unit-test `WavPackBlockHeader` field decode and flag accessors

**Files:**
- Create: `AudioVideoLib.Tests/Formats/WavPackBlockHeaderTests.cs`

- [ ] **Step 1: Build the synthetic header bytes (TDD: write first)**

Construct a known-good header in test code and assert every accessor.

```csharp
namespace AudioVideoLib.Tests.Formats;

using System;
using System.Buffers.Binary;
using AudioVideoLib.Formats;
using Xunit;

public sealed class WavPackBlockHeaderTests
{
    [Fact]
    public void Parse_RejectsNonWvpkMagic()
    {
        var bad = new byte[WavPackBlockHeader.Size];
        bad[0] = (byte)'X';
        Assert.Null(WavPackBlockHeader.Parse(bad));
    }

    [Fact]
    public void Parse_RejectsTruncatedSpan()
    {
        Assert.Null(WavPackBlockHeader.Parse(new byte[31]));
    }

    [Fact]
    public void Parse_DecodesAllFields()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w'; b[1] = (byte)'v'; b[2] = (byte)'p'; b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4, 4),  0x1234u);   // ckSize
        BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(8, 2),  (ushort)0x0410); // version
        b[10] = 0x01;                                                          // block_index_u8
        b[11] = 0x02;                                                          // total_samples_u8
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12, 4), 0x1000u);   // total_samples
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(16, 4), 0x2000u);   // block_index
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(20, 4), 0x0400u);   // block_samples
        // flags: BYTES_STORED=1 (=> 2 bytes/sample), SRATE index 9 (=> 44100), HYBRID set.
        var flags = 0x1u | 0x8u | (9u << 23);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), flags);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(28, 4), 0xDEADBEEFu); // crc

        var h = WavPackBlockHeader.Parse(b);

        Assert.NotNull(h);
        Assert.Equal("wvpk", h!.CkId);
        Assert.Equal(0x1234u, h.CkSize);
        Assert.Equal(0x0410, h.Version);
        Assert.Equal((byte)0x01, h.BlockIndexHigh);
        Assert.Equal((byte)0x02, h.TotalSamplesHigh);
        Assert.Equal(0x1000u, h.TotalSamplesLow);
        Assert.Equal(0x2000u, h.BlockIndexLow);
        Assert.Equal(0x0400u, h.BlockSamples);
        Assert.Equal(flags, h.Flags);
        Assert.Equal(0xDEADBEEFu, h.Crc);
        Assert.Equal(2, h.BytesPerSample);
        Assert.False(h.IsMono);
        Assert.True(h.IsHybrid);
        Assert.False(h.IsFloat);
        Assert.Equal(44100, h.SampleRate);
        Assert.Equal(0x2000L + (0x01L << 32), h.BlockIndex);
        // GET_TOTAL_SAMPLES: 0x1000 + (0x02 << 32) - 0x02
        Assert.Equal(0x1000L + (0x02L << 32) - 0x02L, h.TotalSamples);
    }

    [Fact]
    public void TotalSamples_UnknownIsMinusOne()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w'; b[1] = (byte)'v'; b[2] = (byte)'p'; b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12, 4), 0xFFFFFFFFu);
        Assert.Equal(-1L, WavPackBlockHeader.Parse(b)!.TotalSamples);
    }

    [Fact]
    public void SampleRate_NonStandardIndexIsZero()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w'; b[1] = (byte)'v'; b[2] = (byte)'p'; b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), 15u << 23);
        Assert.Equal(0, WavPackBlockHeader.Parse(b)!.SampleRate);
    }
}
```

- [ ] **Step 2: Run the new test class**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPackBlockHeaderTests"`
Expected: 5 tests, all green.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib/Formats/WavPackBlockHeader.cs AudioVideoLib.Tests/Formats/WavPackBlockHeaderTests.cs
git commit -m "feat(wavpack): WavPackBlockHeader read-only model with LE field decode"
```

---

### Task 6: Create `WavPackSubBlock` skeleton

**Files:**
- Create: `AudioVideoLib/Formats/WavPackSubBlock.cs`

A sub-block is the WavPack metadata-chunk record that lives inside a block's payload area (post-header). One block can contain many sub-blocks; their IDs come from `wavpack.h:165-193`. The encoding is described in `3rdparty/WavPack/src/open_utils.c:read_metadata_buff` (lines 713-754):

- byte 0: `id` byte — low 5 bits are the unique ID (`ID_UNIQUE == 0x3f` mask is wide), bit 5 (`ID_OPTIONAL_DATA == 0x20`) marks the optional-data band, bit 6 (`ID_ODD_SIZE == 0x40`) flags an odd-byte payload, bit 7 (`ID_LARGE == 0x80`) signals a 24-bit size instead of 8.
- bytes 1..n: size (in 16-bit words), 1 byte by default, 3 bytes when `ID_LARGE` is set.
- payload: `byte_length` bytes (decoded from words), padded to even by an extra byte when `ID_ODD_SIZE` is set.

We expose the *raw* id byte (so callers can match against `wavpack.h` constants directly) plus a payload accessor that reads the bytes from the source on demand.

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Summary of one WavPack metadata sub-block — the id-prefixed records embedded
/// inside a block's post-header payload. Layout per
/// <c>3rdparty/WavPack/src/open_utils.c:read_metadata_buff</c> (lines 713-754).
/// </summary>
/// <remarks>
/// Sub-block IDs are documented in <c>3rdparty/WavPack/include/wavpack.h:158-193</c>.
/// The unique ID is in the low bits of <see cref="RawId"/> after masking out
/// <c>ID_LARGE (0x80)</c>, <c>ID_ODD_SIZE (0x40)</c>, and <c>ID_OPTIONAL_DATA (0x20)</c>;
/// callers can also compare <see cref="RawId"/> directly against the named
/// <c>ID_OPTIONAL_DATA | n</c> constants.
/// </remarks>
public sealed class WavPackSubBlock
{
    private readonly ISourceReader _source;

    internal WavPackSubBlock(ISourceReader source, byte rawId, long payloadOffset, int payloadLength)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        RawId = rawId;
        PayloadOffset = payloadOffset;
        PayloadLength = payloadLength;
    }

    /// <summary>Raw id byte (including the <c>ID_LARGE</c>, <c>ID_ODD_SIZE</c>, <c>ID_OPTIONAL_DATA</c> flag bits).</summary>
    public byte RawId { get; }

    /// <summary>Unique id with the three flag bits cleared (bit 7, bit 6, bit 5).</summary>
    public byte UniqueId => (byte)(RawId & 0x1F);

    /// <summary>True if the <c>ID_OPTIONAL_DATA</c> band is set.</summary>
    public bool IsOptional => (RawId & 0x20) != 0;

    /// <summary>True if the <c>ID_LARGE</c> bit is set (i.e., the on-disk size header was 24-bit).</summary>
    public bool IsLargeSize => (RawId & 0x80) != 0;

    /// <summary>File offset (within the source stream) of the first payload byte.</summary>
    public long PayloadOffset { get; }

    /// <summary>Logical payload length in bytes — already accounts for <c>ID_ODD_SIZE</c>.</summary>
    public int PayloadLength { get; }

    /// <summary>
    /// Reads the sub-block payload bytes from the source. The source must be alive —
    /// after <see cref="WavPackStream.Dispose"/>, this method throws.
    /// </summary>
    public byte[] ReadPayload()
    {
        var buf = new byte[PayloadLength];
        _source.Read(PayloadOffset, buf);
        return buf;
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

---

### Task 7: Create `WavPackBlock` (combined block model)

**Files:**
- Create: `AudioVideoLib/Formats/WavPackBlock.cs`

Per the spec §4.2 frame-model row: `WavPackBlock { Header, StartOffset, Length, SubBlocks }`.

- [ ] **Step 1: Write the class**

```csharp
namespace AudioVideoLib.Formats;

using System.Collections.Generic;

/// <summary>
/// One WavPack block: 32-byte <see cref="WavPackBlockHeader"/> followed by a
/// concatenation of <see cref="WavPackSubBlock"/> records.
/// </summary>
public sealed class WavPackBlock
{
    internal WavPackBlock(
        WavPackBlockHeader header,
        long startOffset,
        long length,
        IReadOnlyList<WavPackSubBlock> subBlocks)
    {
        Header = header;
        StartOffset = startOffset;
        Length = length;
        SubBlocks = subBlocks;
    }

    /// <summary>The decoded 32-byte preamble.</summary>
    public WavPackBlockHeader Header { get; }

    /// <summary>File offset of the first byte of this block (the <c>'w'</c> of <c>wvpk</c>).</summary>
    public long StartOffset { get; }

    /// <summary>
    /// Total on-disk block length in bytes — equals <c>Header.CkSize + 8</c> per the
    /// <c>ckSize</c>-excludes-leading-8 convention noted at <c>wavpack.h:62-67</c>.
    /// </summary>
    public long Length { get; }

    /// <summary>Sub-block summaries in file order.</summary>
    public IReadOnlyList<WavPackSubBlock> SubBlocks { get; }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

- [ ] **Step 3: Commit progress**

```bash
git add AudioVideoLib/Formats/WavPackSubBlock.cs AudioVideoLib/Formats/WavPackBlock.cs
git commit -m "feat(wavpack): WavPackSubBlock and WavPackBlock model"
```

---

### Task 8: `WavPackStream` skeleton — class shell, properties, Dispose

**Files:**
- Create: `AudioVideoLib/IO/WavPackStream.cs`

Mirror `Mp4Stream`'s lifecycle precisely (`AudioVideoLib/IO/Mp4Stream.cs:29-46,112-114,165-169`).

- [ ] **Step 1: Write the skeleton**

```csharp
namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Structural walker for WavPack (<c>.wv</c>) files. Enumerates blocks and the
/// metadata sub-blocks within each block; <see cref="WriteTo"/> streams the
/// original bytes through unchanged (no audio re-encode).
/// </summary>
/// <remarks>
/// Reference: <c>3rdparty/WavPack/include/wavpack.h</c> for layout constants;
/// <c>3rdparty/WavPack/src/open_utils.c:read_next_header</c> (lines 951-984) for the
/// header validation rule; <c>read_metadata_buff</c> (lines 713-754) for sub-block parsing.
/// <para />
/// Hybrid mode: a <c>.wv</c> file may interleave correction-stream blocks if the file
/// was muxed that way; the walker treats each <c>wvpk</c> block uniformly, regardless
/// of whether its <c>ID_WVC_BITSTREAM</c> sub-block carries lossless residuals.
/// Multi-file hybrid (<c>.wvc</c> companion file) is out of scope per spec §4.2.
/// </remarks>
public sealed class WavPackStream : IMediaContainer, IDisposable
{
    private readonly List<WavPackBlock> _blocks = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration
    {
        get
        {
            if (_blocks.Count == 0)
            {
                return 0;
            }

            var first = _blocks[0].Header;
            var rate = first.SampleRate;
            if (rate <= 0)
            {
                return 0;
            }

            var total = first.TotalSamples;
            return total < 0 ? 0 : total * 1000L / rate;
        }
    }

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>Gets the WavPack blocks discovered in the input, in file order.</summary>
    public IReadOnlyList<WavPackBlock> Blocks => _blocks;

    /// <inheritdoc/>
    public bool ReadStream(Stream stream)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }
}
```

- [ ] **Step 2: Build to verify the skeleton compiles**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

---

### Task 9: Implement `WavPackStream.ReadStream` — header check + block enumeration

**Files:**
- Modify: `AudioVideoLib/IO/WavPackStream.cs`

Port the high-level structure of `3rdparty/WavPack/src/open_utils.c:WavpackOpenFileInputEx64` (block enumeration loop) plus the header validator from `read_next_header` (lines 951-984). For each block, advance by `header.CkSize + 8`.

- [ ] **Step 1: Replace the `NotImplementedException` body**

```csharp
    /// <inheritdoc/>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        var totalLen = stream.Length;
        if (totalLen - start < WavPackBlockHeader.Size)
        {
            return false;
        }

        // Magic check at offset 0 (relative to the caller's stream position).
        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4 ||
            magic[0] != (byte)'w' || magic[1] != (byte)'v' ||
            magic[2] != (byte)'p' || magic[3] != (byte)'k')
        {
            stream.Position = start;
            return false;
        }

        stream.Position = start;

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        _blocks.Clear();

        var pos = 0L; // file-relative-to-StartOffset
        var sourceLength = _source.Length;
        while (pos + WavPackBlockHeader.Size <= sourceLength)
        {
            Span<byte> hdrBytes = stackalloc byte[WavPackBlockHeader.Size];
            var read = _source.Read(pos, hdrBytes);
            if (read != WavPackBlockHeader.Size)
            {
                break;
            }

            var header = WavPackBlockHeader.Parse(hdrBytes);
            if (header is null)
            {
                // No valid wvpk magic at the expected offset — stop. The library does
                // not currently support resync-on-garbage; tags / trailing data live
                // outside the wvpk block stream and are handled by AudioTags upstream.
                break;
            }

            var blockLength = (long)header.CkSize + 8L;
            if (blockLength < WavPackBlockHeader.Size || pos + blockLength > sourceLength)
            {
                break;
            }

            var subBlocks = ParseSubBlocks(pos + WavPackBlockHeader.Size, (int)(blockLength - WavPackBlockHeader.Size));
            _blocks.Add(new WavPackBlock(header, StartOffset + pos, blockLength, subBlocks));

            pos += blockLength;
        }

        // Position the caller's stream just past the last decoded block so an outer scanner can continue.
        var consumed = _blocks.Count > 0
            ? _blocks[^1].StartOffset + _blocks[^1].Length
            : start;
        stream.Position = consumed;
        EndOffset = consumed;

        return _blocks.Count > 0;
    }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: fails because `ParseSubBlocks` is unresolved — that's the next task.

---

### Task 10: Implement `WavPackStream.ParseSubBlocks` — port `read_metadata_buff`

**Files:**
- Modify: `AudioVideoLib/IO/WavPackStream.cs`

Port `3rdparty/WavPack/src/open_utils.c:read_metadata_buff` (lines 713-754). The walker only records the (id, payload-offset, payload-length) triple; payload bytes are read from `_source` on demand by `WavPackSubBlock.ReadPayload`.

- [ ] **Step 1: Add the helper**

```csharp
    private List<WavPackSubBlock> ParseSubBlocks(long payloadStart, int payloadLength)
    {
        var subs = new List<WavPackSubBlock>();
        if (_source is null)
        {
            return subs;
        }

        var span = new byte[payloadLength];
        var got = _source.Read(payloadStart, span);
        if (got != payloadLength)
        {
            return subs;
        }

        var i = 0;
        while (i + 2 <= span.Length)
        {
            var rawId = span[i];
            i++;

            // Size header: 8-bit by default; 24-bit when ID_LARGE is set.
            int wordLen = span[i];
            i++;

            int byteLen;
            if ((rawId & 0x80) != 0)
            {
                if (i + 2 > span.Length)
                {
                    break;
                }

                wordLen += span[i] << 8;
                wordLen += span[i + 1] << 16;
                i += 2;
            }

            byteLen = wordLen << 1;

            // ID_ODD_SIZE: subtract 1 byte from the logical length but keep the on-disk
            // padding (a stored even-aligned length).
            var hadOddSize = (rawId & 0x40) != 0;
            if (hadOddSize)
            {
                if (byteLen == 0)
                {
                    break;
                }

                byteLen--;
            }

            var onDiskLen = byteLen + (byteLen & 1);
            if (i + onDiskLen > span.Length)
            {
                break;
            }

            subs.Add(new WavPackSubBlock(_source, rawId, payloadStart + i, byteLen));
            i += onDiskLen;
        }

        return subs;
    }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

---

### Task 11: Implement `WavPackStream.WriteTo` — passthrough

**Files:**
- Modify: `AudioVideoLib/IO/WavPackStream.cs`

`WriteTo` is a single source-to-destination splice from `StartOffset` to `EndOffset`. There is no metadata block to re-emit at the WavPack level — APEv2 / ID3v1 footers live *outside* the wvpk block stream and are handled by the tag scanner.

- [ ] **Step 1: Replace the `NotImplementedException` body**

```csharp
    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the source has been disposed or was never populated.
    /// </exception>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        if (_blocks.Count == 0)
        {
            return;
        }

        var first = _blocks[0];
        var last = _blocks[^1];
        var length = last.StartOffset + last.Length - first.StartOffset;
        _source.CopyTo(first.StartOffset - StartOffset, length, destination);
    }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build AudioVideoLib/AudioVideoLib.csproj`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib/IO/WavPackStream.cs
git commit -m "feat(wavpack): WavPackStream walker — block + sub-block enumeration, passthrough WriteTo"
```

---

### Task 12: Helper — synthesise a minimal valid `.wv` block in-memory

**Files:**
- Create: `AudioVideoLib.Tests/IO/WavPackStreamTests.cs` (empty file with namespace + helpers; tests added in subsequent tasks).

The test corpus file is added in Task 14, but several tests (block-header parse, sub-block enumeration boundary cases, detached-source error) work better against a synthesised block. Define a `BuildSyntheticBlock` helper in the test file that produces a single valid block with two sub-blocks: one ID_DECORR_TERMS (id=0x02, 4-byte payload) and one ID_DUMMY (id=0x00, 0-byte payload).

- [ ] **Step 1: Create the test file with the synth helper**

```csharp
namespace AudioVideoLib.Tests.IO;

using System;
using System.Buffers.Binary;
using System.IO;
using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using Xunit;

public sealed class WavPackStreamTests
{
    private static byte[] BuildSyntheticBlock()
    {
        // Two sub-blocks back-to-back inside a single wvpk block.
        // Sub-block 1: id=ID_DECORR_TERMS (0x02), 4 bytes -> 2 words, no flag bits.
        //   bytes: 0x02 0x02 <4 bytes payload>
        // Sub-block 2: id=ID_DUMMY (0x00), 0 bytes.
        //   bytes: 0x00 0x00
        var sub1 = new byte[] { 0x02, 0x02, 0xAA, 0xBB, 0xCC, 0xDD };
        var sub2 = new byte[] { 0x00, 0x00 };
        var subAll = new byte[sub1.Length + sub2.Length];
        Buffer.BlockCopy(sub1, 0, subAll, 0, sub1.Length);
        Buffer.BlockCopy(sub2, 0, subAll, sub1.Length, sub2.Length);

        var block = new byte[WavPackBlockHeader.Size + subAll.Length];
        block[0] = (byte)'w'; block[1] = (byte)'v'; block[2] = (byte)'p'; block[3] = (byte)'k';
        // ckSize = block size minus 8.
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4, 4),  (uint)(block.Length - 8));
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8, 2),  (ushort)0x0410);
        // block_index_u8, total_samples_u8 zero.
        // total_samples = 1024.
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(12, 4), 1024u);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(16, 4), 0u);    // block_index
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(20, 4), 1024u); // block_samples
        // flags: SRATE index 9 (44100), 16-bit (BYTES_STORED=1), stereo.
        var flags = 0x1u | (9u << 23);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(24, 4), flags);
        // crc: leave zero — walker doesn't verify.
        Buffer.BlockCopy(subAll, 0, block, WavPackBlockHeader.Size, subAll.Length);
        return block;
    }
}
```

- [ ] **Step 2: Build to verify the test file compiles** (no tests yet)

Run: `dotnet build AudioVideoLib.Tests`
Expected: succeeds.

---

### Task 13: Synthetic-block tests — read, sub-block enumeration, passthrough, detached source

**Files:**
- Modify: `AudioVideoLib.Tests/IO/WavPackStreamTests.cs`

- [ ] **Step 1: Append the synthetic-block tests**

```csharp
    [Fact]
    public void ReadStream_RejectsNonWvPkInput()
    {
        var notWvpk = new byte[64];
        notWvpk[0] = (byte)'X';
        using var ms = new MemoryStream(notWvpk);
        using var walker = new WavPackStream();
        Assert.False(walker.ReadStream(ms));
        Assert.Empty(walker.Blocks);
    }

    [Fact]
    public void ReadStream_DecodesSyntheticBlock()
    {
        var bytes = BuildSyntheticBlock();
        using var ms = new MemoryStream(bytes);
        using var walker = new WavPackStream();

        Assert.True(walker.ReadStream(ms));
        Assert.Single(walker.Blocks);

        var block = walker.Blocks[0];
        Assert.Equal(0L, block.StartOffset);
        Assert.Equal(bytes.Length, block.Length);
        Assert.Equal(44100, block.Header.SampleRate);
        Assert.Equal(2, block.Header.BytesPerSample);
        Assert.False(block.Header.IsMono);
    }

    [Fact]
    public void ReadStream_EnumeratesSubBlocks()
    {
        var bytes = BuildSyntheticBlock();
        using var ms = new MemoryStream(bytes);
        using var walker = new WavPackStream();

        walker.ReadStream(ms);

        var subs = walker.Blocks[0].SubBlocks;
        Assert.Equal(2, subs.Count);

        Assert.Equal(0x02, subs[0].RawId);
        Assert.Equal(4, subs[0].PayloadLength);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, subs[0].ReadPayload());

        Assert.Equal(0x00, subs[1].RawId);
        Assert.Equal(0, subs[1].PayloadLength);
    }

    [Fact]
    public void WriteTo_RoundTripsBytesIdentically()
    {
        var bytes = BuildSyntheticBlock();
        using var input = new MemoryStream(bytes);
        using var walker = new WavPackStream();
        walker.ReadStream(input);

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(bytes, output.ToArray());
    }

    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        var bytes = BuildSyntheticBlock();
        using var input = new MemoryStream(bytes);
        var walker = new WavPackStream();
        walker.ReadStream(input);
        walker.Dispose();

        var ex = Assert.Throws<InvalidOperationException>(() => walker.WriteTo(new MemoryStream()));
        Assert.Equal(
            "Source stream was detached or never read. WriteTo requires a live source.",
            ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsBeforeRead()
    {
        using var walker = new WavPackStream();
        var ex = Assert.Throws<InvalidOperationException>(() => walker.WriteTo(new MemoryStream()));
        Assert.Equal(
            "Source stream was detached or never read. WriteTo requires a live source.",
            ex.Message);
    }
```

- [ ] **Step 2: Run the synthetic suite**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPackStreamTests"`
Expected: 6 tests, all green.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib.Tests/IO/WavPackStreamTests.cs
git commit -m "test(wavpack): synthetic-block round-trip and lifecycle tests"
```

---

### Task 14: Generate and check in real WavPack sample files

**Files:**
- Create: `AudioVideoLib.Tests/TestFiles/wavpack/sample-stereo-44100-16.wv`
- Create: `AudioVideoLib.Tests/TestFiles/wavpack/sample-mono-48000-24.wv`
- Create: `AudioVideoLib.Tests/TestFiles/wavpack/sample-with-apev2.wv`

The samples must match the descriptions in the provenance fragment from Task 2.

- [ ] **Step 1: Generate the stereo sample**

If `wavpack` (the CLI from `3rdparty/WavPack`) is not on `PATH`, build it from the in-tree source first per the project's normal third-party build instructions, or use a system package (`wavpack` on most distros). Then:

```bash
# Generate a 0.25 s 440 Hz stereo sine WAV via the project's existing test-asset script,
# or via ffmpeg if available locally:
ffmpeg -y -f lavfi -i "sine=frequency=440:duration=0.25:sample_rate=44100" \
       -ac 2 -sample_fmt s16 /tmp/sine.wav
wavpack -h /tmp/sine.wav -o AudioVideoLib.Tests/TestFiles/wavpack/sample-stereo-44100-16.wv
```

- [ ] **Step 2: Generate the mono 24-bit sample**

```bash
ffmpeg -y -f lavfi -i "sine=frequency=880:duration=0.25:sample_rate=48000" \
       -ac 1 -sample_fmt s32 /tmp/sine-mono.wav
wavpack -h /tmp/sine-mono.wav -o AudioVideoLib.Tests/TestFiles/wavpack/sample-mono-48000-24.wv
```

(The 24-bit width is the WavPack output — the input is 32-bit but `wavpack` packs to the minimum sufficient width. If the resulting file's first block reports `BytesPerSample == 4`, regenerate with explicit `--bits-per-sample` controls or use a true 24-bit input WAV.)

- [ ] **Step 3: Generate the APEv2-tagged sample**

```bash
cp AudioVideoLib.Tests/TestFiles/wavpack/sample-stereo-44100-16.wv \
   AudioVideoLib.Tests/TestFiles/wavpack/sample-with-apev2.wv
wvtag -w "Title=WavPack walker test" -w "Artist=AudioVideoLib" \
   AudioVideoLib.Tests/TestFiles/wavpack/sample-with-apev2.wv
```

- [ ] **Step 4: Verify file sizes are reasonable (< 100 KB each)**

```bash
ls -la AudioVideoLib.Tests/TestFiles/wavpack/
```

Expected: each file is between 4 KB and 100 KB. Larger means the duration regenerated wrong.

- [ ] **Step 5: Commit**

```bash
git add AudioVideoLib.Tests/TestFiles/wavpack/sample-stereo-44100-16.wv \
        AudioVideoLib.Tests/TestFiles/wavpack/sample-mono-48000-24.wv \
        AudioVideoLib.Tests/TestFiles/wavpack/sample-with-apev2.wv
git commit -m "test(wavpack): add sample .wv files (stereo 44.1/16, mono 48/24, APEv2-tagged)"
```

If neither `ffmpeg` nor `wavpack` are available, fall back to including pre-built fixtures that an outside contributor produced; the provenance fragment from Task 2 must be updated to reflect the actual tool that produced them.

---

### Task 15: Real-file tests — stereo header parse, block enumeration, round-trip

**Files:**
- Modify: `AudioVideoLib.Tests/IO/WavPackStreamTests.cs`

- [ ] **Step 1: Append the real-file tests**

```csharp
    private const string StereoFixture = "TestFiles/wavpack/sample-stereo-44100-16.wv";
    private const string MonoFixture   = "TestFiles/wavpack/sample-mono-48000-24.wv";
    private const string ApeFixture    = "TestFiles/wavpack/sample-with-apev2.wv";

    [Fact]
    public void Stereo_HeaderReportsExpectedRateChannelsAndDepth()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var first = walker.Blocks[0].Header;
        Assert.Equal(44100, first.SampleRate);
        Assert.Equal(2, first.BytesPerSample);
        Assert.False(first.IsMono);
        Assert.False(first.IsFloat);
        Assert.True(first.TotalSamples > 0);
    }

    [Fact]
    public void Mono_HeaderReportsMonoAnd24BitDepth()
    {
        using var fs = File.OpenRead(MonoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var first = walker.Blocks[0].Header;
        Assert.Equal(48000, first.SampleRate);
        Assert.Equal(3, first.BytesPerSample);
        Assert.True(first.IsMono);
    }

    [Fact]
    public void Stereo_BlockLengthsSumToAudioSpan()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        walker.ReadStream(fs);

        var sumLen = 0L;
        foreach (var b in walker.Blocks)
        {
            sumLen += b.Length;
        }

        // The block stream is contiguous, starting at the file's first wvpk byte.
        var firstStart = walker.Blocks[0].StartOffset;
        var lastEnd = walker.Blocks[^1].StartOffset + walker.Blocks[^1].Length;
        Assert.Equal(lastEnd - firstStart, sumLen);
    }

    [Fact]
    public void Stereo_FirstBlockExposesKnownSubBlockIds()
    {
        // ID_DECORR_TERMS (0x02), ID_DECORR_WEIGHTS (0x03), and ID_WV_BITSTREAM (0x0a)
        // are present in essentially every wavpack-encoded stereo block per
        // 3rdparty/WavPack/include/wavpack.h:167-175.
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        walker.ReadStream(fs);

        var ids = new HashSet<byte>();
        foreach (var sb in walker.Blocks[0].SubBlocks)
        {
            ids.Add(sb.UniqueId);
        }

        Assert.Contains((byte)0x02, ids);
        Assert.Contains((byte)0x03, ids);
        Assert.Contains((byte)0x0a, ids);
    }

    [Fact]
    public void Stereo_RoundTripIsByteIdentical()
    {
        var original = File.ReadAllBytes(StereoFixture);

        using var fs = new MemoryStream(original);
        using var walker = new WavPackStream();
        walker.ReadStream(fs);

        using var output = new MemoryStream();
        walker.WriteTo(output);

        // The walker covers only the wvpk block stream. If the file has any
        // pre-block bytes (it shouldn't, per `wvpk`-at-offset-0) or post-block
        // bytes (e.g., an APEv2/ID3v1 footer), they fall outside the walker's
        // span. For sample-stereo-44100-16.wv there is no tag footer; the
        // round-trip is exact. The APEv2 fixture has its own dedicated test.
        Assert.Equal(original, output.ToArray());
    }
```

Add `using System.Collections.Generic;` to the test file's `using` block.

- [ ] **Step 2: Run the new tests**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPackStreamTests"`
Expected: 11 tests total (5 synthetic + 5 real + 1 magic-rejection from Task 13), all green.

If `Stereo_RoundTripIsByteIdentical` fails because the sample file has trailing non-wvpk bytes (e.g., an ID3v1 trailer was added by the encoder), regenerate the stereo fixture without footer tagging — the *footer*-aware round-trip is exercised by the APEv2 fixture in Task 16, not this one.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib.Tests/IO/WavPackStreamTests.cs
git commit -m "test(wavpack): real-file header, block enumeration, sub-block ID, round-trip"
```

---

### Task 16: Tag-edit round-trip via `AudioTags`

**Files:**
- Modify: `AudioVideoLib.Tests/IO/WavPackStreamTests.cs`

The tag scanner is responsible for APEv2/ID3v1 mutation. The walker's job is only to leave the audio bytes untouched. The acceptance criterion in spec §7.2 task 4 is: edit a tag, save, re-parse the saved bytes, audio frame `(StartOffset, Length)` ranges still describe the *same byte contents* as the original.

- [ ] **Step 1: Append the tag-edit round-trip test**

```csharp
    [Fact]
    public void TagEdit_DoesNotPerturbWvpkBlockBytes()
    {
        var original = File.ReadAllBytes(ApeFixture);

        // Establish the original wvpk audio span and a hash of those bytes.
        byte[] originalAudioBytes;
        long originalAudioStart;
        long originalAudioLength;
        using (var ms = new MemoryStream(original))
        using (var walker = new WavPackStream())
        {
            Assert.True(walker.ReadStream(ms));
            originalAudioStart = walker.Blocks[0].StartOffset;
            var lastEnd = walker.Blocks[^1].StartOffset + walker.Blocks[^1].Length;
            originalAudioLength = lastEnd - originalAudioStart;
            originalAudioBytes = new byte[originalAudioLength];
            Array.Copy(original, originalAudioStart, originalAudioBytes, 0, originalAudioLength);
        }

        // Round-trip through AudioTags with a tag mutation.
        // (AudioTags is the canonical APEv2 editor; see Tags/AudioTags.cs and
        // the existing Mp3 / FLAC tag-edit tests for the pattern.)
        var edited = AudioVideoLib.Tags.AudioTags.EditApeTag(
            original,
            ape =>
            {
                ape.Title = "WavPack walker test (edited)";
            });

        // Re-parse the saved bytes: audio bytes must match exactly.
        using var ms2 = new MemoryStream(edited);
        using var walker2 = new WavPackStream();
        Assert.True(walker2.ReadStream(ms2));
        var editedAudioStart = walker2.Blocks[0].StartOffset;
        var editedLastEnd = walker2.Blocks[^1].StartOffset + walker2.Blocks[^1].Length;
        var editedAudioLength = editedLastEnd - editedAudioStart;

        Assert.Equal(originalAudioLength, editedAudioLength);

        var editedAudioBytes = new byte[editedAudioLength];
        Array.Copy(edited, editedAudioStart, editedAudioBytes, 0, editedAudioLength);
        Assert.Equal(originalAudioBytes, editedAudioBytes);
    }
```

`AudioTags.EditApeTag` is illustrative — it stands in for whatever entry point the existing scanner exposes for APEv2 mutation. Before running, verify the actual API by reading `AudioVideoLib/Tags/AudioTags.cs` and adjust the call site accordingly. If the only entry point is via `MediaContainers`, route through that instead; the assertion (audio bytes preserved) is the load-bearing part.

- [ ] **Step 2: Run the tag-edit test**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPackStreamTests.TagEdit_DoesNotPerturbWvpkBlockBytes"`
Expected: pass.

- [ ] **Step 3: Commit**

```bash
git add AudioVideoLib.Tests/IO/WavPackStreamTests.cs
git commit -m "test(wavpack): tag-edit round-trip preserves wvpk block bytes"
```

---

### Task 17: Magic-byte dispatch test

**Files:**
- Modify: `AudioVideoLib.Tests/IO/WavPackStreamTests.cs`

Spec §7.2 task 6: `MediaContainers.ReadStream` returns a `WavPackStream` for a fresh `.wv` file. **However**, the registry update for WavPack lives in Phase 2, not this plan. The Phase-1 dispatch test instead asserts that `WavPackStream` itself is the right walker for the format — i.e., construction-time magic detection works on a blob beginning with `wvpk`. Phase 2 will add the cross-walker dispatch test.

- [ ] **Step 1: Append the standalone-magic test**

```csharp
    [Fact]
    public void DirectInvocation_OnWavPackBytes_Succeeds()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));
    }

    [Fact]
    public void DirectInvocation_OnNonWavPackBytes_Fails()
    {
        var bogus = new byte[256];
        for (var i = 0; i < bogus.Length; i++)
        {
            bogus[i] = (byte)i;
        }

        using var ms = new MemoryStream(bogus);
        using var walker = new WavPackStream();
        Assert.False(walker.ReadStream(ms));
    }
```

- [ ] **Step 2: Run; commit**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPackStreamTests"`
Expected: all green.

```bash
git add AudioVideoLib.Tests/IO/WavPackStreamTests.cs
git commit -m "test(wavpack): magic-byte direct-invocation accept/reject"
```

---

### Task 18: Documentation page

**Files:**
- Create: `src/docs/container-formats/wavpack.md`

Mirror the layout of `src/docs/container-formats/flacstream.md` (intro, on-disk-layout section, code sample).

- [ ] **Step 1: Write the page**

```markdown
# WavPackStream

Walks a WavPack (`.wv`) file as a sequence of fixed-prefix blocks; each block
carries a 32-byte preamble plus a stream of metadata sub-blocks that describe
decorrelation terms, entropy variables, RIFF wrappers, optional sample rates,
and the audio bitstream itself. `WavPackStream.Blocks` surfaces every block
with its `Header`, `StartOffset`, `Length`, and `SubBlocks` summary.

## On-disk layout

A WavPack file is a flat concatenation of blocks. Every block begins with the
ASCII magic `wvpk`, followed by 28 little-endian bytes that decode into the
`WavpackHeader` struct (block size, stream version, 40-bit total samples,
40-bit block index, block-samples count, flags bitfield, CRC). The `flags`
field encodes sample-rate index, mono / hybrid / float / DSD bits, and
multichannel sequencing markers (`INITIAL_BLOCK`, `FINAL_BLOCK`).

Inside each block, after the 32-byte header, lives a sequence of metadata
sub-blocks. A sub-block opens with a single `id` byte; the high three bits
signal `ID_LARGE` (24-bit size header instead of 8-bit), `ID_ODD_SIZE`
(payload is one byte short of the on-disk word-aligned length), and
`ID_OPTIONAL_DATA` (id is from the 0x20 band, e.g., `ID_RIFF_HEADER`,
`ID_SAMPLE_RATE`). The remaining low bits identify the chunk: decorrelation
terms, entropy vars, the WV / WVC / WVX bitstream slices, channel info, MD5
checksum, and so on. The complete enumeration is in
`include/wavpack.h:158-193`.

`WavPackStream.WriteTo(destination)` streams the original block bytes from
the source `Stream` directly to the destination — there is no audio
re-encode. APEv2 and ID3v1 footers (carried *outside* the wvpk block stream)
are managed by the `AudioTags` scanner.

```csharp
var wv = streams.OfType<WavPackStream>().First();

foreach (var block in wv.Blocks)
{
    Console.WriteLine(
        $"block @ {block.StartOffset}: {block.Header.SampleRate} Hz, " +
        $"{block.Header.BytesPerSample}-byte samples, " +
        $"{(block.Header.IsMono ? "mono" : "stereo")}, " +
        $"{block.SubBlocks.Count} sub-blocks");

    foreach (var sub in block.SubBlocks)
    {
        Console.WriteLine($"  sub 0x{sub.RawId:x2} ({sub.PayloadLength} bytes)");
    }
}
```

Hybrid mode: when a `.wv` file contains correction-stream blocks interleaved
inline (rather than in a separate `.wvc` companion), the walker enumerates
those blocks too — no special-casing needed. Multi-file hybrid (audio in
`.wv`, residuals in `.wvc`) is out of scope and not currently supported.
```

- [ ] **Step 2: Commit**

```bash
git add src/docs/container-formats/wavpack.md
git commit -m "docs(wavpack): WavPackStream container-format page"
```

---

### Task 19: Final build + full-suite test

**Files:**
- None modified.

- [ ] **Step 1: Full clean build**

Run: `dotnet build AudioVideoLib.slnx -c Release`
Expected: zero errors, zero new warnings.

- [ ] **Step 2: Full test suite**

Run: `dotnet test AudioVideoLib.slnx -c Release`
Expected: all tests green (existing + new WavPack tests).

- [ ] **Step 3: Verify the test count adds up**

Run: `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~WavPack"`
Expected: at least 18 tests (5 `WavPackBlockHeaderTests` + 13 `WavPackStreamTests`).

---

### Task 20: Worktree wrap-up

**Files:**
- None modified.

- [ ] **Step 1: Confirm the worktree is clean**

Run: `git status`
Expected: working tree clean, all commits on `feat/wavpack`.

- [ ] **Step 2: Confirm the branch contains every expected file**

```bash
git diff --stat origin/main...feat/wavpack
```

Expected files:
- `AudioVideoLib/Formats/WavPackBlockHeader.cs` (new)
- `AudioVideoLib/Formats/WavPackSubBlock.cs` (new)
- `AudioVideoLib/Formats/WavPackBlock.cs` (new)
- `AudioVideoLib/IO/WavPackStream.cs` (new)
- `AudioVideoLib.Tests/IO/WavPackStreamTests.cs` (new)
- `AudioVideoLib.Tests/Formats/WavPackBlockHeaderTests.cs` (new)
- `AudioVideoLib.Tests/TestFiles/wavpack/sample-stereo-44100-16.wv` (new)
- `AudioVideoLib.Tests/TestFiles/wavpack/sample-mono-48000-24.wv` (new)
- `AudioVideoLib.Tests/TestFiles/wavpack/sample-with-apev2.wv` (new)
- `AudioVideoLib.Tests/TestFiles/wavpack/PROVENANCE.md` (new)
- `src/docs/container-formats/wavpack.md` (new)

No existing files modified. **Do not** edit `MediaContainers.cs`, `_doc_snippets/Program.cs`, `src/TestFiles.txt`, `src/docs/getting-started.md`, `src/docs/container-formats.md`, or `src/docs/release-notes.md` — those are Phase 2 work.

- [ ] **Step 3: Hand off**

The orchestrator merges `feat/wavpack` into the integration branch alongside the other Phase-1 worktrees. Phase 2 then wires `WavPackStream` into the dispatch and updates the docs index.

---

## Acceptance criteria

- `WavPackStream` parses a real `.wv` fixture and reports the correct sample rate (44100 / 48000), channel count (mono vs stereo), and bytes-per-sample (2 vs 3) on the first block.
- Block enumeration: total length sums match the contiguous wvpk-span length.
- Sub-block enumeration: every block with an audio bitstream exposes a sub-block with `UniqueId == 0x0a` (`ID_WV_BITSTREAM`).
- Round-trip identity: `ReadStream` then `WriteTo` produces byte-identical output for the unmodified stereo fixture.
- Tag-edit round-trip: editing the APEv2 footer of `sample-with-apev2.wv` does not change any byte inside the wvpk block span.
- `WriteTo` after `Dispose` (or before `ReadStream`) throws `InvalidOperationException` with the spec §3.1 message verbatim.
- Magic detection: `ReadStream` returns `false` (and leaves the input stream's position unchanged) when the first four bytes are not `wvpk`.
- No existing files modified: `git diff --name-only origin/main...feat/wavpack | xargs -I{} grep -l "{}"` shows only the new-file set above.
- Full `dotnet build -c Release` and `dotnet test` both green.

---

## Self-review notes

- No "TBD", "implement later", or "appropriate error handling" placeholders — every code block compiles as written, modulo the one note in Task 16 that the exact `AudioTags` API may differ from the illustrative `EditApeTag` name.
- Test method names match the implementation methods they exercise: `Parse_*` -> `WavPackBlockHeader.Parse`, `ReadStream_*` -> `WavPackStream.ReadStream`, `WriteTo_*` -> `WavPackStream.WriteTo`.
- Spec §4.2 facts covered: magic `wvpk` (Task 4 + Task 9 + Task 17), file list `WavPackBlockHeader.cs` / `WavPackSubBlock.cs` / `WavPackStream.cs` (Tasks 3-11), block model with `Header` / `StartOffset` / `Length` / `SubBlocks` (Task 7), APEv2 + ID3v1 tag carriage (Task 16 + provenance fragment), reference to `wavpack.h` and `pack_utils.c` / `open_utils.c` (every porting task cites the specific function).
- Hybrid-mode handling: noted in the docs page (Task 18) and acknowledged in the `WavPackStream` class doc comment (Task 8) — single-file inline correction stream is fine; multi-file is out of scope per spec §4.2.
- All model properties read-only: `WavPackBlockHeader` uses `init`-only setters; `WavPackSubBlock` and `WavPackBlock` use constructor-only initialisation with `get`-only properties.
- No `MediaContainers.cs` / `_doc_snippets/Program.cs` / index-doc / release-notes edits — Task 20 explicitly forbids them.
