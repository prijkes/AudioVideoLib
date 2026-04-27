# MpaStream — MPEG-1 / MPEG-2 / MPEG-2.5 audio

Reads Layer I, II, III frame-by-frame. Each `MpaFrame` exposes header
fields (`AudioVersion`, `LayerVersion`, `Bitrate`, `SamplingRate`,
`ChannelMode`, `Emphasis`, …), the optional CRC-16, the side
information, and the frame data. VBR headers (Xing, LAME, VBRI) are
automatically detected in the first frame and exposed via
`MpaStream.VbrHeader`.

## On-disk layout

MPEG-1 / MPEG-2 / MPEG-2.5 audio streams are built around frames as the
fundamental structural unit. Each frame consists of a 4-byte header
followed by variable-length audio data, forming a simple concatenated
sequence with no container wrapper.

The frame header encodes the audio version, layer (I, II, or III),
bitrate, sampling rate, channel mode, and other transport metadata.
Protection is optional: if the protection bit is set, a CRC-16
checksum follows the header, covering the subsequent side information
and audio samples. For Layer III (MP3), side information precedes the
compressed audio payload.

VBR files may contain a metadata header (Xing, LAME, or VBRI) embedded
in the first frame's audio data section. This header supplies an
accurate frame count and file size for seeking and duration estimation,
overriding frame-by-frame summation. CBR streams contain no such
header and rely on consistent bitrate across all frames.

Access frame data through `MpaStream.Frames`; check for VBR headers
via `MpaStream.VbrHeader`, which may include a LAME tag with encoder
metadata.

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
