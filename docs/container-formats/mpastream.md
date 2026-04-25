# MpaStream — MPEG-1 / MPEG-2 / MPEG-2.5 audio

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
