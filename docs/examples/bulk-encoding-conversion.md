# Bulk-convert ID3v2.4 frame text encodings

Walks every `.mp3` in a directory, switches all text frames in v2.4
tags to UTF-8, and writes the rebuilt tag bytes back into a sidecar.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var tags = AudioTags.ReadStream(fs);
    if (tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault() is not { } id3v2)
    {
        continue;
    }
    if (id3v2.Version < Id3v2Version.Id3v240)
    {
        continue;
    }

    var changed = 0;
    foreach (var frame in id3v2.GetFrames<Id3v2TextFrame>())
    {
        if (frame.TextEncoding == Id3v2FrameEncodingType.UTF8)
        {
            continue;
        }
        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
        changed++;
    }

    if (changed > 0)
    {
        File.WriteAllBytes(Path.ChangeExtension(path, ".id3v2"), id3v2.ToByteArray());
        Console.WriteLine($"{Path.GetFileName(path)}: re-encoded {changed} frame(s) to UTF-8");
    }
}

return 0;
```
