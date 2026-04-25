# Audit an MP3 library: bitrate and channel-mode histogram

Walks every `.mp3` in a tree, counts each file's first-frame bitrate
and channel mode, and prints two summary tables. Useful for spotting
mono files in a stereo library, or low-bitrate stragglers in a
high-fidelity collection. Demonstrates `MpaStream.Frames` +
`MpaFrame.Bitrate` / `ChannelMode`.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: mp3-audit <directory>");
    return 1;
}

var bitrates = new SortedDictionary<int, int>();
var channels = new SortedDictionary<MpaChannelMode, int>();

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3", SearchOption.AllDirectories))
{
    using var fs = File.OpenRead(path);
    var streams = MediaContainers.ReadStream(fs);
    var first = streams.OfType<MpaStream>().FirstOrDefault()?.Frames.FirstOrDefault();
    if (first is null) continue;

    bitrates.TryGetValue(first.Bitrate, out var b);
    bitrates[first.Bitrate] = b + 1;

    channels.TryGetValue(first.ChannelMode, out var c);
    channels[first.ChannelMode] = c + 1;
}

Console.WriteLine("Bitrate (kbps)  Files");
foreach (var (bitrate, count) in bitrates)
{
    Console.WriteLine($"  {bitrate,5}        {count}");
}

Console.WriteLine();
Console.WriteLine("Channel mode    Files");
foreach (var (mode, count) in channels)
{
    Console.WriteLine($"  {mode,-12}  {count}");
}

return 0;
```

For VBR files the first-frame bitrate is the *nominal* rate, not the
average. If you need the true average, compute it from
`mpa.TotalMediaSize / mpa.TotalDuration` or read it off the
[VBR header](../container-formats/mpastream.md) when present.
