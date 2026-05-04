# Format pack — MPC (Musepack) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `MpcStream`, an `IMediaContainer` walker for Musepack (`.mpc`) files supporting both stream version 7 (SV7) and stream version 8 (SV8), with parse-for-inspection and byte-passthrough save (interpretation-3 from the spec §3).

**Architecture:** `MpcStream` is a sealed `IMediaContainer : IDisposable` walker that holds an `ISourceReader? _source` populated at `ReadStream` time and uses `_source.CopyTo(offset, length, destination)` in `WriteTo` for bit-exact byte passthrough. The walker auto-detects SV7 vs SV8 via the leading magic and exposes a parsed top-of-file descriptor (`MpcStreamHeader`) plus a per-packet offset/length list (`MpcPacket`). Audio is never re-encoded; tag editing happens via `AudioTags`.

**Tech Stack:** C# 13 / .NET 10, xUnit. Format reference at `3rdparty/musepack_src_r475/`.

**Worktree:** `feat/mpc` (per spec §8 Phase 1).

**Scope boundary:** This plan creates files only. It does NOT touch `MediaContainers.cs`, `_doc_snippets/Program.cs`, `docs/getting-started.md`, `docs/container-formats.md`, `docs/release-notes.md`, or `src/TestFiles.txt` — those are Phase 2. Phase 0 (`plans/2026-05-05-format-pack-phase0-foundation.md`) is assumed merged: `IMediaContainer` already extends `IDisposable`, the lifetime contract is documented, and the canonical exception message is `"Source stream was detached or never read. WriteTo requires a live source."`.

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib/Formats/MpcStreamVersion.cs` | Create — enum (`Sv7`, `Sv8`). |
| `AudioVideoLib/Formats/MpcStreamHeader.cs` | Create — read-only top-of-file descriptor. |
| `AudioVideoLib/Formats/MpcPacket.cs` | Create — read-only per-packet model. |
| `AudioVideoLib/IO/MpcStream.cs` | Create — sealed walker, SV7+SV8 dispatch, `_source` lifetime, `WriteTo` passthrough. |
| `AudioVideoLib.Tests/MpcStreamTests.cs` | Create — xUnit suite. |
| `AudioVideoLib.Tests/TestFiles/mpc/sample-sv7.mpc` | Create — tiny pre-encoded SV7 sample (~few KB). |
| `AudioVideoLib.Tests/TestFiles/mpc/sample-sv8.mpc` | Create — tiny pre-encoded SV8 sample. |
| `AudioVideoLib.Tests/TestFiles/mpc/PROVENANCE.md` | Create — per-format provenance fragment. Phase 2 references it from `src/TestFiles.txt`. |
| `docs/container-formats/mpcstream.md` | Create — per-format docs page. |

**No existing files are modified.**

---

## Tasks

### Task 1: Create `MpcStreamVersion` enum

**Files:** Create `AudioVideoLib/Formats/MpcStreamVersion.cs`.

- [ ] Write the enum:

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// Identifies which Musepack bitstream version a file uses. Set on
/// <see cref="IO.MpcStream.Version"/> after a successful read.
/// </summary>
public enum MpcStreamVersion
{
    /// <summary>Stream version 7 — magic <c>'M','P','+',0x?7</c>. Frame-based bitstream.</summary>
    Sv7,

    /// <summary>Stream version 8 — magic <c>MPCK</c>. Keyed-packet bitstream.</summary>
    Sv8,
}
```

- [ ] `dotnet build AudioVideoLib/AudioVideoLib.csproj`. Expected: clean.

---

### Task 2: Create `MpcStreamHeader` (read-only descriptor)

**Files:** Create `AudioVideoLib/Formats/MpcStreamHeader.cs`.

Field set mirrors the inspection-relevant subset of `mpc_streaminfo` from `3rdparty/musepack_src_r475/include/mpc/streaminfo.h`. Decoder-internal state (`max_band`, `ms`, `pns`, `block_pwr`) is not exposed — this library doesn't decode PCM.

- [ ] Write:

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// Top-of-file descriptor parsed from a Musepack container. Read-only.
/// Field names follow <c>mpc_streaminfo</c> in
/// <c>3rdparty/musepack_src_r475/include/mpc/streaminfo.h</c>.
/// </summary>
public sealed class MpcStreamHeader
{
    internal MpcStreamHeader(
        MpcStreamVersion version, uint sampleRate, uint channels,
        ulong totalSamples, ulong beginningSilence, bool isTrueGapless,
        float profile, string profileName, uint encoderVersion, string encoderName,
        ushort rgTitleGain, ushort rgTitlePeak, ushort rgAlbumGain, ushort rgAlbumPeak)
    {
        Version = version;
        SampleRate = sampleRate;
        Channels = channels;
        TotalSamples = totalSamples;
        BeginningSilence = beginningSilence;
        IsTrueGapless = isTrueGapless;
        Profile = profile;
        ProfileName = profileName;
        EncoderVersion = encoderVersion;
        EncoderName = encoderName;
        ReplayGainTitleGain = rgTitleGain;
        ReplayGainTitlePeak = rgTitlePeak;
        ReplayGainAlbumGain = rgAlbumGain;
        ReplayGainAlbumPeak = rgAlbumPeak;
    }

    public MpcStreamVersion Version { get; }
    public uint SampleRate { get; }
    public uint Channels { get; }
    public ulong TotalSamples { get; }
    public ulong BeginningSilence { get; }
    public bool IsTrueGapless { get; }
    public float Profile { get; }
    public string ProfileName { get; }
    public uint EncoderVersion { get; }
    public string EncoderName { get; }
    public ushort ReplayGainTitleGain { get; }
    public ushort ReplayGainTitlePeak { get; }
    public ushort ReplayGainAlbumGain { get; }
    public ushort ReplayGainAlbumPeak { get; }
}
```

(Add an XML doc comment above each public property when implementing — one short sentence each.)

- [ ] `dotnet build`. Expected: clean.

---

### Task 3: Create `MpcPacket` (read-only per-packet model)

**Files:** Create `AudioVideoLib/Formats/MpcPacket.cs`.

- [ ] Write:

```csharp
namespace AudioVideoLib.Formats;

/// <summary>
/// One unit of the Musepack bitstream — an SV7 frame or an SV8 keyed packet.
/// </summary>
/// <remarks>
/// SV7 uses "frames", SV8 uses keyed packets. We unify on "packet". For SV7,
/// <see cref="Key"/> is <c>null</c>; for SV8 it is the 2-character ASCII key
/// (e.g. <c>"SH"</c>, <c>"AP"</c>, <c>"RG"</c>, <c>"EI"</c>, <c>"SE"</c>,
/// <c>"ST"</c>, <c>"SO"</c>, <c>"CT"</c>). See
/// <c>3rdparty/musepack_src_r475/libmpcdec/mpc_demux.c</c> (the <c>KEY_*</c>
/// macros and <c>mpc_demux_header</c> at line 477) for the full set.
/// </remarks>
public sealed class MpcPacket
{
    internal MpcPacket(string? key, long startOffset, long length, ulong sampleCount)
    {
        Key = key;
        StartOffset = startOffset;
        Length = length;
        SampleCount = sampleCount;
    }

    public string? Key { get; }
    public long StartOffset { get; }
    public long Length { get; }
    public ulong SampleCount { get; }
}
```

- [ ] `dotnet build`. Expected: clean.

---

### Task 4: Commit model classes

- [ ] `dotnet build AudioVideoLib.slnx -c Debug` — clean.
- [ ] Commit:

```bash
git add AudioVideoLib/Formats/MpcStreamVersion.cs AudioVideoLib/Formats/MpcStreamHeader.cs AudioVideoLib/Formats/MpcPacket.cs
git commit -m "feat(mpc): add MpcStreamVersion / MpcStreamHeader / MpcPacket model classes

Read-only descriptors for the upcoming MpcStream walker. Field set
follows mpc_streaminfo from 3rdparty/musepack_src_r475/include/mpc/streaminfo.h."
```

---

### Task 5: Scaffold `MpcStream`

**Files:** Create `AudioVideoLib/IO/MpcStream.cs`.

Mirror the shape of `Mp4Stream.cs` exactly: `IMediaContainer, IDisposable`, `private ISourceReader? _source`, dispose old source then create `new StreamSourceReader(stream, leaveOpen: true)` in `ReadStream`, throw the canonical message in `WriteTo` when `_source is null`.

- [ ] Write the skeleton:

```csharp
namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Walker for Musepack (<c>.mpc</c>) containers. Supports SV7 (magic
/// <c>'M','P','+',0x?7</c>) and SV8 (magic <c>MPCK</c>).
/// </summary>
/// <remarks>
/// Source-reference lifetime model per <see cref="IMediaContainer"/>:
/// <see cref="ReadStream"/> records per-packet offsets/lengths,
/// <see cref="WriteTo"/> streams those byte ranges directly from source to
/// destination via <see cref="ISourceReader.CopyTo"/>. No audio re-encoding;
/// tag handling (APEv2 footer, ID3v2 header) is delegated to <c>AudioTags</c>.
/// The walker is robust to a non-zero <see cref="StartOffset"/> when the
/// caller has advanced past an ID3v2 prefix.
/// </remarks>
public sealed class MpcStream : IMediaContainer, IDisposable
{
    private readonly List<MpcPacket> _packets = [];
    private ISourceReader? _source;
    private MpcStreamHeader? _header;

    public long StartOffset { get; private set; }
    public long EndOffset { get; private set; }
    public long TotalDuration =>
        _header is null || _header.SampleRate == 0
            ? 0
            : (long)(_header.TotalSamples * 1000UL / _header.SampleRate);
    public long TotalMediaSize => EndOffset - StartOffset;
    public int MaxFrameSpacingLength { get; set; }

    public MpcStreamHeader? Header => _header;
    public MpcStreamVersion Version => _header?.Version ?? MpcStreamVersion.Sv8;
    public IReadOnlyList<MpcPacket> Packets => _packets;

    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var start = stream.Position;
        if (stream.Length - start < 4) return false;

        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4) return false;
        stream.Position = start;
        if (!TryDetectVersion(magic, out var version)) return false;

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        var ok = version == MpcStreamVersion.Sv7 ? ReadSv7(stream) : ReadSv8(stream);
        stream.Position = EndOffset;
        return ok;
    }

    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        // Byte-passthrough — header + packets are already on disk; we simply
        // re-emit the parsed range. Tag mutations come back through ReadStream
        // on a freshly tagged source produced by AudioTags.
        _source.CopyTo(StartOffset, EndOffset - StartOffset, destination);
    }

    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }

    private static bool TryDetectVersion(ReadOnlySpan<byte> magic, out MpcStreamVersion version)
    {
        if (magic[0] == (byte)'M' && magic[1] == (byte)'P' && magic[2] == (byte)'C' && magic[3] == (byte)'K')
        {
            version = MpcStreamVersion.Sv8;
            return true;
        }

        // SV7: 'M','P','+', then a byte whose low nibble is 7. A 3-byte 'MP+'
        // check is NOT sufficient (spec §4.1).
        if (magic[0] == (byte)'M' && magic[1] == (byte)'P' && magic[2] == (byte)'+'
            && (magic[3] & 0x0F) == 0x07)
        {
            version = MpcStreamVersion.Sv7;
            return true;
        }

        version = MpcStreamVersion.Sv8;
        return false;
    }

    // Bodies in Tasks 6 (SV7) and 7 (SV8).
    private bool ReadSv7(Stream stream) => false;
    private bool ReadSv8(Stream stream) => false;
}
```

- [ ] `dotnet build` — clean.

---

### Task 6: Implement SV7 header parsing + audio-span packet

**Reference:** `3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:109` (`streaminfo_read_header_sv7`). The SV7 header is a 24-byte bit-packed struct following the 4-byte magic. The C function reads a sequence of bit-fields via `mpc_bits_read`; port them in order. Sample-rate index lookup table is at `streaminfo.c:144` (`freq[]`). Profile-name lookup is at `streaminfo.c:56` (`mpc_get_version_string`).

For inspection-only callers, per-frame sub-division is not needed — we record one `MpcPacket` covering `[postHeaderOffset, EndOffset)`.

- [ ] Add to `MpcStream.cs`:

```csharp
private bool ReadSv7(Stream stream)
{
    Span<byte> hdr = stackalloc byte[24];
    if (stream.Read(hdr) != 24) return false;

    // Port: streaminfo_read_header_sv7 (streaminfo.c:109). Decode bit-fields
    // in the order the C source does, populating the locals below. Use
    // a cursor-based bit reader over `hdr` (read each big-endian uint32 word
    // from `hdr` and shift-mask, mirroring mpc_bits_read).
    //
    // Required outputs:
    //   uint frames                  // total frame count
    //   uint sampleFreqIdx           // 2 bits, indexes freqTable below
    //   uint profileRaw              // 4 bits (0..15)
    //   ushort titleGain, titlePeak, albumGain, albumPeak  // each 16 bits
    //   bool trueGapless             // 1 bit
    //   uint lastFrameSamples        // 11 bits
    //   uint encoderVersion          // 8 bits
    uint frames = 0, sampleFreqIdx = 0, profileRaw = 0, lastFrameSamples = 0, encoderVersion = 0;
    ushort titleGain = 0, titlePeak = 0, albumGain = 0, albumPeak = 0;
    bool trueGapless = false;
    /* PORT BIT-LEVEL DECODE FROM streaminfo.c:109..170 HERE */

    ReadOnlySpan<uint> freqTable = [44100, 48000, 37800, 32000];
    var sampleRate = freqTable[(int)(sampleFreqIdx & 3)];
    var totalSamples = trueGapless
        ? (frames > 0 ? (ulong)frames * 1152UL - lastFrameSamples : 0UL)
        : (frames > 0 ? (ulong)frames * 1152UL - 481UL : 0UL);

    _header = new MpcStreamHeader(
        MpcStreamVersion.Sv7, sampleRate, channels: 2,
        totalSamples, beginningSilence: 0, isTrueGapless: trueGapless,
        profile: profileRaw, profileName: ProfileName(profileRaw),
        encoderVersion: encoderVersion, encoderName: string.Empty,
        rgTitleGain: titleGain, rgTitlePeak: titlePeak,
        rgAlbumGain: albumGain, rgAlbumPeak: albumPeak);

    var audioStart = stream.Position;
    _packets.Add(new MpcPacket(
        key: null, startOffset: audioStart,
        length: EndOffset - audioStart, sampleCount: totalSamples));
    return true;
}

private static string ProfileName(float profile) => profile switch
{
    // Port: mpc_get_version_string (streaminfo.c:56).
    7 => "Telephone",
    8 => "Thumb",
    9 => "Radio",
    10 => "Standard",
    11 => "Xtreme",
    12 => "Insane",
    13 => "BrainDead",
    _ => "Unknown",
};
```

- [ ] Implement the `/* PORT BIT-LEVEL DECODE */` block by translating `streaminfo.c:109..170` line-for-line. The structure is short (~60 lines of C). If the project already has a `BitStreamReader` helper, reuse it; otherwise inline the shift-mask reads against `hdr`.
- [ ] `dotnet build` — clean.

---

### Task 7: Commit SV7 parsing

- [ ] Commit:

```bash
git add AudioVideoLib/IO/MpcStream.cs
git commit -m "feat(mpc): MpcStream walker — SV7 header parse + audio-span packet

Ports streaminfo_read_header_sv7 from
3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:109. Records a single
MpcPacket covering the post-header span; per-frame sub-division is not
needed for inspection-only callers. WriteTo is full-source passthrough."
```

---

### Task 8: Implement SV8 keyed-packet enumeration

**Reference:** `3rdparty/musepack_src_r475/libmpcdec/mpc_demux.c:477` (`mpc_demux_header`) for the dispatch loop, `:25` for `KEY_*` macros. Varint reader: `mpc_bits_get_size` in `libmpcdec/mpc_bits_reader.c`. Per-key parsers: `streaminfo_read_header_sv8` (streaminfo.c:187), `streaminfo_gain` (streaminfo.c:172), `streaminfo_encoder_info` (streaminfo.c:221).

Each packet: 2-byte ASCII key, varint total length (key+size+payload), payload. Recognised keys: `SH` stream header, `RG` replaygain, `EI` encoder info, `SO`/`ST` seek table offset/data, `AP` audio, `CT` chapter, `SE` stream end.

- [ ] Add the varint reader and main loop to `MpcStream.cs`:

```csharp
/// <summary>Reads an SV8 size-encoded varint. Mirrors mpc_bits_get_size in mpc_bits_reader.c.</summary>
private static long ReadSv8VarInt(Stream stream, out int bytesConsumed)
{
    bytesConsumed = 0;
    long value = 0;
    while (true)
    {
        var b = stream.ReadByte();
        if (b < 0) return -1;
        bytesConsumed++;
        value = (value << 7) | (uint)(b & 0x7F);
        if ((b & 0x80) == 0) return value;
        if (bytesConsumed > 8) return -1;
    }
}

private static long ReadVarIntFromSpan(ReadOnlySpan<byte> buf, ref int pos)
{
    long value = 0;
    while (pos < buf.Length)
    {
        var b = buf[pos++];
        value = (value << 7) | (uint)(b & 0x7F);
        if ((b & 0x80) == 0) return value;
    }
    return -1;
}

private bool ReadSv8(Stream stream)
{
    stream.Position = StartOffset + 4; // past 'MPCK'
    var sawSh = false;
    var sawSe = false;

    while (stream.Position + 3 <= EndOffset && !sawSe)
    {
        var pktStart = stream.Position;
        Span<byte> keyBuf = stackalloc byte[2];
        if (stream.Read(keyBuf) != 2) break;

        var key = $"{(char)keyBuf[0]}{(char)keyBuf[1]}";
        var size = ReadSv8VarInt(stream, out _);
        if (size < 0 || pktStart + size > EndOffset) return false;

        var payloadStart = stream.Position;
        var payloadLen = pktStart + size - payloadStart;

        switch (key)
        {
            case "SH": ReadSv8StreamHeader(stream, payloadStart, payloadLen); sawSh = true; break;
            case "RG": ReadSv8ReplayGain(stream, payloadStart, payloadLen); break;
            case "EI": ReadSv8EncoderInfo(stream, payloadStart, payloadLen); break;
            case "SE": sawSe = true; break;
            // ST / SO / CT / AP recorded but not parsed for inspection.
        }

        _packets.Add(new MpcPacket(key, pktStart, size, sampleCount: 0));
        stream.Position = pktStart + size;
    }

    return sawSh && _header is not null;
}
```

- [ ] Add the per-key parsers (port from `streaminfo.c`):

```csharp
// SH packet — port streaminfo_read_header_sv8 (streaminfo.c:187).
// Layout: crc32(4) stream_version(1) sample_count(varint) beg_silence(varint)
//         then bit-packed: sample_freq_idx(3) max_used_band(5) channels(4)+1
//         ms(1) block_pwr(3).
private void ReadSv8StreamHeader(Stream stream, long payloadStart, long payloadLen)
{
    stream.Position = payloadStart;
    Span<byte> buf = stackalloc byte[(int)Math.Min(payloadLen, 64)];
    var read = stream.Read(buf);
    if (read < 8) return;

    var pos = 5; // skip 4-byte CRC + 1-byte stream_version
    var totalSamples = ReadVarIntFromSpan(buf, ref pos);
    var begSilence = ReadVarIntFromSpan(buf, ref pos);
    if (pos + 2 > read) return;

    var b0 = buf[pos++];
    var sampleFreqIdx = (b0 >> 5) & 0x07;
    var b1 = buf[pos++];
    var channels = (uint)((b1 >> 4) & 0x0F) + 1u;

    ReadOnlySpan<uint> freqTable = [44100, 48000, 37800, 32000, 0, 0, 0, 0];
    _shTotalSamples = (ulong)totalSamples;
    _shBegSilence = (ulong)begSilence;
    _shSampleRate = freqTable[sampleFreqIdx];
    _shChannels = channels;
    EnsureHeader();
}

// RG packet — port streaminfo_gain (streaminfo.c:172).
// Layout: version(1) title_gain(2) title_peak(2) album_gain(2) album_peak(2).
private void ReadSv8ReplayGain(Stream stream, long payloadStart, long payloadLen)
{
    stream.Position = payloadStart;
    Span<byte> buf = stackalloc byte[9];
    if (stream.Read(buf) < 9) return;
    _rgTitleGain = BinaryPrimitives.ReadUInt16BigEndian(buf[1..3]);
    _rgTitlePeak = BinaryPrimitives.ReadUInt16BigEndian(buf[3..5]);
    _rgAlbumGain = BinaryPrimitives.ReadUInt16BigEndian(buf[5..7]);
    _rgAlbumPeak = BinaryPrimitives.ReadUInt16BigEndian(buf[7..9]);
    EnsureHeader();
}

// EI packet — port streaminfo_encoder_info (streaminfo.c:221).
// Layout: profile-byte (7 bits profile fixed-point + 1 bit pns), version(3 bytes).
private void ReadSv8EncoderInfo(Stream stream, long payloadStart, long payloadLen)
{
    stream.Position = payloadStart;
    Span<byte> buf = stackalloc byte[4];
    if (stream.Read(buf) < 4) return;
    _eiProfile = (buf[0] >> 1) / 8.0f;
    _eiEncoderVersion = ((uint)buf[1] << 16) | ((uint)buf[2] << 8) | buf[3];
    EnsureHeader();
}

private ulong _shTotalSamples, _shBegSilence;
private uint _shSampleRate, _shChannels = 2;
private ushort _rgTitleGain, _rgTitlePeak, _rgAlbumGain, _rgAlbumPeak;
private float _eiProfile;
private uint _eiEncoderVersion;

private void EnsureHeader() =>
    _header = new MpcStreamHeader(
        MpcStreamVersion.Sv8, _shSampleRate, _shChannels,
        _shTotalSamples, _shBegSilence, isTrueGapless: true,
        profile: _eiProfile, profileName: ProfileName(_eiProfile),
        encoderVersion: _eiEncoderVersion, encoderName: string.Empty,
        rgTitleGain: _rgTitleGain, rgTitlePeak: _rgTitlePeak,
        rgAlbumGain: _rgAlbumGain, rgAlbumPeak: _rgAlbumPeak);
```

- [ ] `dotnet build` — clean.

---

### Task 9: Commit SV8 parsing

- [ ] Commit:

```bash
git add AudioVideoLib/IO/MpcStream.cs
git commit -m "feat(mpc): MpcStream walker — SV8 keyed-packet enumeration

Walks SV8 packets (SH, RG, EI, AP, ST, SO, CT, SE), recording each as
an MpcPacket. SH/RG/EI are parsed into MpcStreamHeader; everything
else is opaque (offset, length). Ports streaminfo_read_header_sv8,
streaminfo_gain, streaminfo_encoder_info from
3rdparty/musepack_src_r475/libmpcdec/streaminfo.c."
```

---

### Task 10: Add test sample files + provenance fragment

**Files:**
- Create `AudioVideoLib.Tests/TestFiles/mpc/sample-sv7.mpc` (~few KB)
- Create `AudioVideoLib.Tests/TestFiles/mpc/sample-sv8.mpc` (~few KB)
- Create `AudioVideoLib.Tests/TestFiles/mpc/PROVENANCE.md`

The .NET solution does not build the C encoder. Source the samples in this order:

1. **Preferred:** Musepack project's published reference vectors (musepack.net), BSD-3-clause.
2. **Fallback:** Encode locally with `mpcenc` (from `3rdparty/musepack_src_r475/mpcenc/`) for SV7 and `mpc2sv8` for SV8 from a public-domain WAV (e.g. one cycle of a sine wave).
3. **Last resort:** Mark dependent tests `[Fact(Skip = "no MPC sample available")]` with that exact reason and document in PROVENANCE.md.

- [ ] Place both `.mpc` files. Verify magic with `xxd -l 4`:
    - `sample-sv7.mpc`: `4D50 2B17` (or `…27`/etc.)
    - `sample-sv8.mpc`: `4D50 434B`

- [ ] Update `AudioVideoLib.Tests/AudioVideoLib.Tests.csproj` to copy samples to output. Use `Grep` to locate the existing `<None Include="TestFiles\…">` pattern in the csproj and follow it. Add:

```xml
<None Include="TestFiles\mpc\*.mpc" CopyToOutputDirectory="PreserveNewest" />
```

- [ ] Write `AudioVideoLib.Tests/TestFiles/mpc/PROVENANCE.md`:

```markdown
# MPC test samples

| File | Size | Source | License | Notes |
|---|---|---|---|---|
| sample-sv7.mpc | ~K bytes | <encoder cmdline + WAV source> | <license> | SV7, 44.1 kHz / 2 ch / Standard |
| sample-sv8.mpc | ~K bytes | <encoder cmdline + WAV source> | <license> | SV8, 44.1 kHz / 2 ch / Standard |

Magic bytes verified:
- sample-sv7.mpc: `4D 50 2B 17`  (`MP+\x17`)
- sample-sv8.mpc: `4D 50 43 4B`  (`MPCK`)

Reproduction commands (if regenerating from a public-domain WAV):

    mpcenc --quality 5.0 input.wav sample-sv7.mpc
    mpcenc --quality 5.0 input.wav tmp.mpc && mpc2sv8 tmp.mpc sample-sv8.mpc

Phase 2 will append a reference to this fragment from `src/TestFiles.txt`.
```

- [ ] `dotnet build AudioVideoLib.Tests` — clean. Verify both files appear under `bin/Debug/net10.0/TestFiles/mpc/`.

---

### Task 11: Write the test scaffold + lifetime tests

**Files:** Create `AudioVideoLib.Tests/MpcStreamTests.cs`.

- [ ] Write:

```csharp
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class MpcStreamTests
{
    private const string Sv7Sample = "TestFiles/mpc/sample-sv7.mpc";
    private const string Sv8Sample = "TestFiles/mpc/sample-sv8.mpc";
    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MpcStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        using var fs = File.OpenRead(Sv8Sample);
        var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }
}
```

- [ ] Run `dotnet test --filter "FullyQualifiedName~MpcStreamTests"` — both pass.

---

### Task 12: Add header-parse tests for SV7 + SV8

- [ ] Append to `MpcStreamTests`:

```csharp
[Fact]
public void ReadStream_Sv7_ParsesHeader()
{
    using var fs = File.OpenRead(Sv7Sample);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(fs));
    Assert.Equal(MpcStreamVersion.Sv7, walker.Version);
    Assert.NotNull(walker.Header);
    Assert.Equal(44100u, walker.Header!.SampleRate);
    Assert.Equal(2u, walker.Header.Channels);
    Assert.True(walker.Header.TotalSamples > 0);
}

[Fact]
public void ReadStream_Sv8_ParsesHeader()
{
    using var fs = File.OpenRead(Sv8Sample);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(fs));
    Assert.Equal(MpcStreamVersion.Sv8, walker.Version);
    Assert.NotNull(walker.Header);
    Assert.Equal(44100u, walker.Header!.SampleRate);
    Assert.Equal(2u, walker.Header.Channels);
    Assert.True(walker.Header.TotalSamples > 0);
}
```

(Adjust the asserted sample rate / channels to match the actual checked-in samples — pin to the known-good values, don't weaken to "nonzero".)

- [ ] Run — both pass.

---

### Task 13: Add packet-enumeration tests

- [ ] Append:

```csharp
[Fact]
public void ReadStream_Sv7_RecordsSingleAudioPacketWithNullKey()
{
    using var fs = File.OpenRead(Sv7Sample);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(fs));
    Assert.Single(walker.Packets);
    Assert.Null(walker.Packets[0].Key);
    Assert.True(walker.Packets[0].Length > 0);
}

[Fact]
public void ReadStream_Sv8_RecordsKeyedPackets()
{
    using var fs = File.OpenRead(Sv8Sample);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(fs));
    Assert.Contains(walker.Packets, p => p.Key == "SH");
    Assert.Contains(walker.Packets, p => p.Key == "AP");
    Assert.Contains(walker.Packets, p => p.Key == "SE");

    var sum = walker.Packets.Sum(p => p.Length);
    var expected = new FileInfo(Sv8Sample).Length - 4; // minus 'MPCK'
    Assert.Equal(expected, sum);
}
```

- [ ] Run — both pass.

---

### Task 14: Add round-trip identity tests

- [ ] Append:

```csharp
[Fact]
public void WriteTo_Sv7_RoundTripsByteIdentical()
{
    var original = File.ReadAllBytes(Sv7Sample);
    using var src = new MemoryStream(original);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(src));
    using var dst = new MemoryStream();
    walker.WriteTo(dst);
    Assert.Equal(original, dst.ToArray());
}

[Fact]
public void WriteTo_Sv8_RoundTripsByteIdentical()
{
    var original = File.ReadAllBytes(Sv8Sample);
    using var src = new MemoryStream(original);
    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(src));
    using var dst = new MemoryStream();
    walker.WriteTo(dst);
    Assert.Equal(original, dst.ToArray());
}
```

- [ ] Run — both pass.

---

### Task 15: Add magic-byte dispatch + non-zero StartOffset tests

- [ ] Append:

```csharp
[Fact]
public void ReadStream_RejectsForeignMagic()
{
    using var src = new MemoryStream([0x66, 0x4C, 0x61, 0x43, 0, 0, 0, 0]); // 'fLaC'
    using var walker = new MpcStream();
    Assert.False(walker.ReadStream(src));
}

[Fact]
public void ReadStream_RejectsBareMpPlusWithWrongVersionNibble()
{
    // 'M','P','+',0x18 — low nibble 8, NOT 7. Must not be misidentified as SV7.
    using var src = new MemoryStream([0x4D, 0x50, 0x2B, 0x18, 0, 0, 0, 0]);
    using var walker = new MpcStream();
    Assert.False(walker.ReadStream(src));
}

[Fact]
public void ReadStream_HonoursNonZeroStartOffset()
{
    // Simulate ID3v2-prefixed file: caller advanced past the tag.
    var raw = File.ReadAllBytes(Sv8Sample);
    var prefix = new byte[64];
    new Random(0).NextBytes(prefix);
    var combined = new byte[prefix.Length + raw.Length];
    Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
    Buffer.BlockCopy(raw, 0, combined, prefix.Length, raw.Length);

    using var src = new MemoryStream(combined);
    src.Position = prefix.Length;

    using var walker = new MpcStream();
    Assert.True(walker.ReadStream(src));
    Assert.Equal(prefix.Length, walker.StartOffset);
    Assert.Equal(combined.Length, walker.EndOffset);

    using var dst = new MemoryStream();
    walker.WriteTo(dst);
    Assert.Equal(raw, dst.ToArray());
}
```

- [ ] Run — all three pass.

---

### Task 16: Add tag-edit round-trip test

The spec §7.2 #4 test: edit an APE/ID3 field via `AudioTags`, save, re-parse, assert the audio (`AP`) packet payload bytes are byte-identical to the original audio bytes.

- [ ] Append:

```csharp
[Fact]
public void TagEdit_PreservesAudioBytesByteForByte()
{
    var original = File.ReadAllBytes(Sv8Sample);

    byte[] originalAudio;
    using (var src = new MemoryStream(original))
    using (var walker = new MpcStream())
    {
        Assert.True(walker.ReadStream(src));
        using var ms = new MemoryStream();
        foreach (var pkt in walker.Packets.Where(p => p.Key == "AP"))
        {
            ms.Write(original, (int)pkt.StartOffset, (int)pkt.Length);
        }
        originalAudio = ms.ToArray();
    }

    // Drive AudioTags here to add/modify an APEv2 field, write the tagged
    // result to a MemoryStream `tagged`, re-parse with MpcStream, and
    // assert the AP-packet payload bytes match `originalAudio`.
    // The exact AudioTags surface lives in the existing AudioTags class —
    // see AudioVideoLib.Tests/ApeTagTests.cs for the public method to call.
    //
    // If the existing AudioTags surface isn't reachable without out-of-scope
    // edits, mark this test [Fact(Skip = "AudioTags write API exposed in Phase 2")].
}
```

- [ ] Run — passes (or skipped with the documented reason).

---

### Task 17: Run full test suite + commit tests

- [ ] `dotnet test AudioVideoLib.Tests --filter "FullyQualifiedName~MpcStreamTests"` — all pass.
- [ ] `dotnet test AudioVideoLib.Tests` — full suite green, no regressions.
- [ ] Commit:

```bash
git add AudioVideoLib.Tests/MpcStreamTests.cs AudioVideoLib.Tests/TestFiles/mpc/ AudioVideoLib.Tests/AudioVideoLib.Tests.csproj
git commit -m "test(mpc): MpcStream walker — header, packets, round-trip, dispatch

Covers spec §7.2: header parse (SV7+SV8), packet enumeration
(SV7 single audio packet, SV8 keyed packets), byte-identical round-trip,
magic-byte rejection (foreign / wrong-nibble), non-zero StartOffset
(ID3v2-prefix), detached-source error. Tag-edit round-trip wired
against AudioTags where possible, otherwise skipped with documented reason."
```

---

### Task 18: Write the per-format docs page

**Files:** Create `docs/container-formats/mpcstream.md`.

- [ ] Read `docs/container-formats/flacstream.md` and `docs/container-formats/mp4stream.md` for layout. Match their style: short intro, "On-disk layout" section, code sample, no novel sections.

- [ ] Write:

```markdown
# MpcStream

Walks Musepack (`.mpc`) containers in either stream version 7 (SV7)
or stream version 8 (SV8). `MpcStream.Header` exposes the parsed
top-of-file descriptor; `MpcStream.Packets` lists each SV8 keyed
packet (or, for SV7, a single audio span). Tags (APEv2 footer, ID3v2
header) are surfaced through the existing `AudioTags` scanner.

## On-disk layout

A Musepack file begins with one of two magic markers. SV7 files start
with the 3-byte ASCII sequence `MP+` followed by a one-byte stream-
version field whose low nibble is `7` (commonly `0x17`); a 3-byte
prefix check is not enough — the version nibble must also match. SV8
files begin with the 4-byte literal `MPCK`. The walker dispatches on
the magic and exposes the chosen version through `MpcStream.Version`.

In SV7, the magic is followed by a fixed 24-byte bit-packed
descriptor encoding the frame count, sample-rate index (44.1, 48, 32,
or 37.8 kHz), profile, encoder version, ReplayGain quad, and a
true-gapless flag. The audio bitstream then runs to end-of-file
(modulo any trailing APE/ID3v1 tag). The walker records a single
`MpcPacket` covering the audio span, with `Key` equal to `null`.

In SV8, the file is a sequence of keyed packets. Each packet starts
with a 2-byte ASCII key, followed by a Musepack varint giving the
total packet length (key + size field + payload), followed by the
payload. Recognised keys include `SH` (stream header), `RG`
(replaygain), `EI` (encoder info), `SO` / `ST` (seek table offset and
seek table), `AP` (audio packet), `CT` (chapter), and `SE` (stream
end). The walker parses `SH`, `RG`, and `EI` into `MpcStreamHeader`;
everything else is recorded as an opaque `(Key, StartOffset, Length)`
triple. Audio playback is the concatenation of the `AP` packet
payloads.

`MpcStream.WriteTo` streams the parsed file unchanged from source to
destination — there is no audio re-encoder. Tag editing happens
through `AudioTags`, which writes a new file the caller then re-parses.

```csharp
using var fs = File.OpenRead("song.mpc");
using var mpc = new MpcStream();
if (!mpc.ReadStream(fs)) return;

Console.WriteLine($"Version: {mpc.Version}");
Console.WriteLine($"Header:  {mpc.Header!.SampleRate} Hz, {mpc.Header.Channels} ch, {mpc.Header.TotalSamples} samples");
Console.WriteLine($"Profile: {mpc.Header.ProfileName} ({mpc.Header.Profile:F1})");

if (mpc.Version == MpcStreamVersion.Sv8)
{
    foreach (var pkt in mpc.Packets)
    {
        Console.WriteLine($"  {pkt.Key} @ 0x{pkt.StartOffset:X8} ({pkt.Length} bytes)");
    }
}

using var dst = File.Create("song-copy.mpc");
mpc.WriteTo(dst); // byte-identical for unmodified input
```
```

- [ ] If `docfx` is available locally, run `docfx docfx.json` and verify zero new warnings. The Phase 2 plan adds the page to the index — do NOT edit `docs/container-formats.md` here.

---

### Task 19: Final validation + commit

- [ ] `dotnet build AudioVideoLib.slnx -c Release` — clean.
- [ ] `dotnet test AudioVideoLib.Tests -c Release --filter "FullyQualifiedName~MpcStreamTests"` — all green.
- [ ] Commit docs:

```bash
git add docs/container-formats/mpcstream.md
git commit -m "docs(mpc): per-format page for MpcStream

Layout follows docs/container-formats/flacstream.md. Phase 2 will
add the page to container-formats.md and release-notes.md."
```

---

### Task 20: Self-review checklist

Before declaring the worktree ready to merge, walk through each item.

- [ ] **Spec §4.1 facts covered:**
    - [ ] SV7 magic check is the 4-byte test (`MP+` + low-nibble-7), not 3-byte. Verified by `ReadStream_RejectsBareMpPlusWithWrongVersionNibble`.
    - [ ] SV8 magic is `MPCK`. Verified by `ReadStream_Sv8_ParsesHeader`.
    - [ ] `MpcPacket.Key` is `null` for SV7. Verified by `ReadStream_Sv7_RecordsSingleAudioPacketWithNullKey`.
    - [ ] `MpcPacket.Key` is the 2-character SV8 key. Verified by `ReadStream_Sv8_RecordsKeyedPackets`.
    - [ ] `MpcStreamVersion` enum has exactly `Sv7` and `Sv8`.
    - [ ] §4.1 file list matches: `Formats/MpcStreamVersion.cs`, `Formats/MpcStreamHeader.cs`, `Formats/MpcPacket.cs`, `IO/MpcStream.cs`.

- [ ] **Spec §3 lifetime contract:**
    - [ ] `MpcStream` declares `IMediaContainer, IDisposable`, holds `private ISourceReader? _source`.
    - [ ] `ReadStream` builds the source as `new StreamSourceReader(stream, leaveOpen: true)` after disposing any prior source.
    - [ ] `WriteTo` throws `InvalidOperationException` with the exact spec message when `_source is null`.
    - [ ] `Dispose` is idempotent and does not close the user's stream.

- [ ] **Mp4Stream pattern fidelity:** `ReadStream` order — peek magic, set `StartOffset`, dispose old source, create new source, set `EndOffset`, walk, restore `stream.Position` — matches `Mp4Stream.ReadStream`.

- [ ] **Read-only model invariants:** `MpcStreamHeader` and `MpcPacket` have no public setters. Constructors are `internal`.

- [ ] **Test names match implementation:** All `ReadStream_*` tests exercise `MpcStream.ReadStream`; `WriteTo_*` tests exercise `MpcStream.WriteTo`; both lifetime-contract tests assert the documented exception message.

- [ ] **No placeholders left:** Search the implementation for `TBD`, `FIXME`, `appropriate error handling`, `implement later`, `/* PORT BIT-LEVEL DECODE */` — zero hits. The bit-level decode in `ReadSv7` is fully ported.

- [ ] **Out-of-scope files untouched:** `git diff --stat <merge-base>` shows only files in the File Structure table. No edits to `MediaContainers.cs`, `_doc_snippets/`, `docs/getting-started.md`, `docs/container-formats.md`, `docs/release-notes.md`, or `src/TestFiles.txt`.

If any checkbox fails, fix before declaring done.

---

## Acceptance criteria

- `MpcStream` compiles, implements `IMediaContainer : IDisposable`, parses both SV7 and SV8 headers, enumerates packets correctly for both versions, and round-trips byte-identical via `WriteTo`.
- All `MpcStreamTests` pass.
- The per-format docs page renders cleanly under DocFX.
- `git diff --stat` against the merge base lists only files from the File Structure table — no incidental edits.
- Provenance fragment at `AudioVideoLib.Tests/TestFiles/mpc/PROVENANCE.md` is in place for Phase 2 to reference from `src/TestFiles.txt`.
