# Bulk-remove an ID3v2 frame from every file in a library

Drops every `PRIV` and `GEOB` frame across an entire directory tree —
useful for stripping per-tool metadata (Windows Media DRM IDs, encoder
sidecars) before publishing. Demonstrates `Id3v2Tag.GetFrames<T>(string)`
+ `RemoveFrames(IEnumerable<Id3v2Frame>)` and the high-level
`AudioInfo` save facade.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib;
using AudioVideoLib.Tags;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: strip-priv-geob <directory>");
    return 1;
}

string[] frameIds = ["PRIV", "GEOB"];
var processed = 0;
var stripped = 0;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3", SearchOption.AllDirectories))
{
    using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    var info = AudioInfo.Analyse(fs);

    var dirty = false;
    foreach (var id3v2 in info.AudioTags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
    {
        var doomed = frameIds.SelectMany(id => id3v2.GetFrames<Id3v2Frame>(id)).ToList();
        if (doomed.Count == 0) continue;

        id3v2.RemoveFrames(doomed);
        stripped += doomed.Count;
        dirty = true;
    }

    if (dirty)
    {
        info.Save(path);
        processed++;
    }
}

Console.WriteLine($"Stripped {stripped} frame(s) across {processed} file(s).");
return 0;
```

The same pattern works for any frame identifier — pass `"COMM"` to drop
comments, `"WXXX"` for user-defined URLs, etc. Use
`Id3v2Tag.RemoveFrames<T>()` to drop every frame of a given .NET type
regardless of identifier.
