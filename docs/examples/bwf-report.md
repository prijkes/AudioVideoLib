# Report broadcast-WAV (BWF) metadata

Pulls the `bext` chunk out of every `.wav` in a directory and prints
the description / originator / timestamp / loudness fields.
Demonstrates `RiffStream.BextChunk`.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

foreach (var path in Directory.EnumerateFiles(args[0], "*.wav"))
{
    using var fs = File.OpenRead(path);
    var streams = MediaContainers.ReadStream(fs);
    var riff = streams.OfType<RiffStream>().FirstOrDefault();
    if (riff?.BextChunk is not { } bext)
    {
        continue;
    }

    Console.WriteLine($"== {Path.GetFileName(path)} ==");
    Console.WriteLine($"  Description : {bext.Description}");
    Console.WriteLine($"  Originator  : {bext.Originator} / {bext.OriginatorReference}");
    Console.WriteLine($"  Timestamp   : {bext.OriginationDate} {bext.OriginationTime}");
    Console.WriteLine($"  TimeRef     : {bext.TimeReference}");
    if (bext.Version >= 1)
    {
        Console.WriteLine($"  Loudness v={bext.LoudnessValue}, range={bext.LoudnessRange}, peak={bext.MaxTruePeakLevel}");
    }
    Console.WriteLine($"  History     : {bext.CodingHistory.TrimEnd()}");
}
```
