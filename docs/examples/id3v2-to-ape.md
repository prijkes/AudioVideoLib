# Migrate ID3v2 → APEv2 alongside the original file

Reads the ID3v2 tag from each input file and emits a sidecar `.apev2`
binary containing the equivalent APEv2 tag. Demonstrates the
two-model walk that underpins format-to-format conversion.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: id3v2-to-ape <directory>");
    return 1;
}

foreach (var path in Directory.EnumerateFiles(args[0], "*.mp3"))
{
    using var fs = File.OpenRead(path);
    var tags = AudioTags.ReadStream(fs);
    if (tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().FirstOrDefault() is not { } id3v2)
    {
        continue;
    }

    var ape = new ApeTag(ApeVersion.Version2) { UseHeader = true, UseFooter = true };

    Copy(id3v2.TrackTitle,    ape, ApeItemKey.Title);
    Copy(id3v2.Artist,        ape, ApeItemKey.Artist);
    Copy(id3v2.AlbumTitle,    ape, ApeItemKey.AlbumName);
    Copy(id3v2.ComposerName,  ape, ApeItemKey.Composer);
    Copy(id3v2.ConductorName, ape, ApeItemKey.Conductor);
    Copy(id3v2.RecordingTime, ape, ApeItemKey.Year);
    Copy(id3v2.YearRecording, ape, ApeItemKey.Year);

    foreach (var apic in id3v2.AttachedPictures)
    {
        var key = apic.PictureType == Id3v2AttachedPictureType.CoverFront
            ? "Cover Art (Front)"
            : $"Cover Art ({apic.PictureType})";
        ape.SetItem(new ApeBinaryItem(ape.Version, key) { Data = apic.PictureData });
    }

    File.WriteAllBytes(Path.ChangeExtension(path, ".apev2"), ape.ToByteArray());
}

return 0;

static void Copy(Id3v2TextFrame? source, ApeTag ape, ApeItemKey key)
{
    if (source is null || source.Values.Count == 0) return;
    var item = new ApeUtf8Item(ape.Version, key);
    foreach (var v in source.Values) item.Values.Add(v);
    ape.SetItem(item);
}
```
