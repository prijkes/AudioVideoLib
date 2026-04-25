# Find files missing required tags

Walks a directory and prints any audio file whose ID3v2 tag is missing
a Title or an Artist. Easy to extend for "all files without cover
art", "files using ID3v2.2 we want to upgrade", etc. Demonstrates the
`Id3v2Tag` strongly-typed properties + the `AttachedPictures` collection.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: find-missing-tags <directory>");
    return 1;
}

var problems = 0;

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3", SearchOption.AllDirectories))
{
    using var fs = File.OpenRead(path);
    var tags = AudioTags.ReadStream(fs);
    var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault();

    var missing = new List<string>();

    if (id3v2 is null)
    {
        missing.Add("no ID3v2 tag");
    }
    else
    {
        if (string.IsNullOrEmpty(id3v2.TrackTitle?.Values.FirstOrDefault())) missing.Add("Title");
        if (string.IsNullOrEmpty(id3v2.Artist?.Values.FirstOrDefault()))     missing.Add("Artist");
        if (string.IsNullOrEmpty(id3v2.AlbumTitle?.Values.FirstOrDefault())) missing.Add("Album");
        if (id3v2.AttachedPictures.Count == 0)                                missing.Add("Cover");
    }

    if (missing.Count > 0)
    {
        Console.WriteLine($"{path}  --  missing: {string.Join(", ", missing)}");
        problems++;
    }
}

Console.Error.WriteLine($"{problems} file(s) flagged.");
return problems == 0 ? 0 : 1;
```

Returning a non-zero exit code on flagged files makes this drop-in to
a CI step that audits a music library before release.
