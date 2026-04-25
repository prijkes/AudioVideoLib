# Validate MPEG audio frames and report VBR header / LAME tag

Useful sanity check for an MP3 archive. Counts frames, verifies CRC
where present, and reports the VBR header (Xing / VBRI) plus any
LAME tag fields.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var streams = MediaContainers.ReadStream(fs);
    var mpa = streams.OfType<MpaStream>().FirstOrDefault();
    if (mpa is null)
    {
        Console.WriteLine($"{Path.GetFileName(path)}: no MPEG audio stream found");
        continue;
    }

    var crcFailures = mpa.Frames.Count(f => f.IsCrcProtected && f.Crc != f.CalculateCrc());
    Console.WriteLine($"{Path.GetFileName(path)}: {mpa.Frames.Count():N0} frames, {mpa.TotalDuration:N0} ms, {crcFailures} CRC failures");

    if (mpa.VbrHeader is { } vbr)
    {
        Console.WriteLine($"  VBR : {vbr.HeaderType} ({vbr.FrameCount} frames, {vbr.FileSize} bytes)");
        if (vbr.LameTag is { } lame)
        {
            Console.WriteLine($"  LAME: {lame.EncoderVersion} – {lame.VbrMethodName}, lowpass {lame.LowpassFilterValue} Hz");
        }
    }
}
```
