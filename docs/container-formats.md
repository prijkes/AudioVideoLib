# Container formats

Walkers in `AudioVideoLib.IO`. Each implements `IMediaContainer` and is
auto-detected by `MediaContainers.ReadStream(stream)`.

## MpaStream — MPEG-1 / MPEG-2 / MPEG-2.5 audio

Reads Layer I, II, III frame-by-frame. Each `MpaFrame` exposes header
fields (`AudioVersion`, `LayerVersion`, `Bitrate`, `SamplingRate`,
`ChannelMode`, `Emphasis`, …), the optional CRC-16, the side
information, and the frame data. VBR headers (Xing, LAME, VBRI) are
automatically detected in the first frame and exposed via
`MpaStream.VbrHeader`.

```csharp
var mpa = streams.OfType<MpaStream>().First();
Console.WriteLine($"MPEG: {mpa.Frames.Count():N0} frames, {mpa.TotalDuration:N0} ms");

if (mpa.VbrHeader is { } vbr)
{
    Console.WriteLine($"VBR: {vbr.HeaderType} ({vbr.FrameCount} frames, {vbr.FileSize} bytes)");
    if (vbr.LameTag is { } lame)
    {
        Console.WriteLine($"LAME {lame.EncoderVersion} – {lame.VbrMethodName}");
    }
}
```

## FlacStream

Walks the FLAC metadata blocks (`STREAMINFO`, `APPLICATION`, `PADDING`,
`SEEKTABLE`, `VORBIS_COMMENT`, `CUESHEET`, `PICTURE`) followed by frames.
`FlacStream.MetadataBlocks` surfaces the blocks; the Vorbis Comment
block holds the tag text via `VorbisComments`.

```csharp
var flac = streams.OfType<FlacStream>().First();

foreach (var block in flac.MetadataBlocks)
{
    Console.WriteLine(block.BlockType);
}

var info = flac.StreamInfoMetadataBlocks.FirstOrDefault();
if (info is not null)
{
    Console.WriteLine($"{info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit, {info.TotalSamples} samples");
}

foreach (var pic in flac.PictureMetadataBlocks)
{
    var ext = pic.MimeType.Split('/').Last();
    File.WriteAllBytes($"flac-{pic.PictureType}.{ext}", pic.PictureData);
}
```

## RiffStream — RIFF / WAV / RIFX

Chunk walker. Supports little-endian `RIFF` and big-endian `RIFX`.
Only `WAVE` form type is currently recognised. Surfaces:

- `fmt` (channels, sample rate, bits/sample, byte rate, block align)
- `data` (offset and size of audio payload)
- `LIST INFO` (→ `InfoTag`)
- `id3 ` / `ID3 ` (→ `EmbeddedId3v2`)
- `bext` (→ `BextChunk`)
- `iXML` (→ `IxmlChunk`)
- All other chunks preserved in `Chunks` with start/end offsets and
  raw bytes.

## AiffStream — AIFF / AIFF-C

Same shape as RIFF but big-endian. Surfaces `COMM` (channels, sample
frames, sample size, sample rate as IEEE 80-bit extended precision,
optional AIFC compression 4-CC), `SSND` (offset and size of audio),
and the text chunks `NAME` / `AUTH` / `ANNO` / `COMT` via
`TextChunks`.

## OggStream

Page-by-page walker. Each `OggPage` records its header (version, flags,
granule position, serial, sequence, CRC, segment count, payload size)
and byte range. On the first beginning-of-stream page the walker peeks
at the payload and identifies the codec (`"vorbis"` or `"opus"`),
extracting the channel count and sample rate for duration calculation.

```csharp
var ogg = streams.OfType<OggStream>().First();
Console.WriteLine($"{ogg.Codec}: {ogg.Channels} ch @ {ogg.SampleRate} Hz across {ogg.PageCount} pages");

foreach (var page in ogg.Pages.Take(3))
{
    Console.WriteLine($"  page {page.SequenceNumber}: {page.PayloadSize} bytes, granule {page.GranulePosition}");
}
```

## Mp4Stream — MP4 / M4A

Iterative box walker (depth-capped at 16 to avoid stack blow-up).
Handles 64-bit extended size (`size == 1`), "to end of file"
(`size == 0`), full-box version/flags headers on `meta`, and the
standard path `moov.udta.meta.ilst` for iTunes metadata. Surfaces:

- `Boxes` — top-level atoms.
- `Tag` (`Mp4MetaTag`) — strongly-typed ilst model.
- `TotalDuration` from `moov.mvhd` (v0 or v1 duration / timescale).

## AsfStream — ASF / WMA / WMV

Header Object walker. Descends the Header Object's children and
recurses into the Header Extension Object to pick up Metadata Object
and Metadata Library Object. Surfaces `Objects` (all top-level objects)
and `MetadataTag` (the aggregated `AsfMetadataTag`). Duration comes
from the File Properties Object's Play Duration (100-ns units).

## MatroskaStream — MKV / MKA / WebM

EBML walker for the EBML Header + first Segment. Parses
`Segment.Info` for `TimecodeScale` + `Duration` and `Segment.Tags` for
the tag tree. Skips `Cluster` and `BlockGroup`. Surfaces `DocType`
(`"matroska"` or `"webm"`) and `Tag` (`MatroskaTag` holding the tag
entries).

## DsfStream / DffStream

DSD audio containers.

- **DSF** (LE): DSD chunk (file size + metadata pointer), fmt chunk
  (format version / channel type / sample frequency / bits/sample /
  sample count / block size), data chunk. Optional ID3v2 at the
  metadata pointer.
- **DFF** (BE): FRM8 form with FVER / PROP (itself nested with FS /
  CHNL / CMPR) / DSD / optional DIIN / COMT / ID3 sub-chunks.

```csharp
foreach (var dsf in streams.OfType<DsfStream>())
{
    Console.WriteLine($"DSF: {dsf.SampleRate} Hz, {dsf.Channels} ch, {dsf.BitsPerSample}-bit, {dsf.SampleCount} samples");
    Console.WriteLine($"  embedded ID3v2 title: {dsf.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}

foreach (var dff in streams.OfType<DffStream>())
{
    Console.WriteLine($"DFF: {dff.SampleRate} Hz, {dff.Channels} ch ({string.Join(",", dff.ChannelIds)})");
}
```
