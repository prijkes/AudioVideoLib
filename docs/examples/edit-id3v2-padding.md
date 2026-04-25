# Set the ID3v2 padding budget

ID3v2 supports trailing padding bytes inside the tag so future edits
that add a frame don't need to rewrite the whole audio payload. This
example sets every tag in a tree to a 4 KB padding budget, then writes
the file back via `AudioInfo`.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib;
using AudioVideoLib.Tags;

if (args.Length != 2 || !int.TryParse(args[1], out var paddingSize))
{
    Console.Error.WriteLine("Usage: id3v2-padding <directory> <bytes>");
    return 1;
}

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3", SearchOption.AllDirectories))
{
    using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    var info = AudioInfo.Analyse(fs);

    var dirty = false;
    foreach (var id3v2 in info.AudioTags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
    {
        if (id3v2.PaddingSize == paddingSize) continue;
        Console.WriteLine($"{Path.GetFileName(path)}: {id3v2.PaddingSize} -> {paddingSize}");
        id3v2.PaddingSize = paddingSize;
        dirty = true;
    }

    if (dirty)
    {
        info.Save(path);
    }
}

return 0;
```

> **Note:** padding only helps when the rewriter knows it can grow the
> tag in-place. The library always recomputes the tag's size header on
> write, so the bytes you commit will be exactly what `ToByteArray()`
> produced — extra padding here is a budget for the *next* edit, not a
> claim about the current one.
