# Extract every embedded picture, regardless of format

Pulls cover art out of ID3v2 (`APIC`), FLAC (`PICTURE` block), and
MP4 (`covr` atom) — all in one pass. Output filenames encode the
source container and picture type so multiple covers don't collide.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: extract-art <input-file> <output-dir>");
    return 1;
}

var (inputPath, outDir) = (args[0], args[1]);
Directory.CreateDirectory(outDir);

using var fs = File.OpenRead(inputPath);
var tags = AudioTags.ReadStream(fs);
fs.Position = 0;
var streams = MediaContainers.ReadStream(fs);

var stem = Path.GetFileNameWithoutExtension(inputPath);
var written = 0;

foreach (var id3v2 in tags.Select(o => o.AudioTag).OfType<Id3v2Tag>())
{
    foreach (var pic in id3v2.AttachedPictures)
    {
        var ext = pic.ImageFormat.Split('/').Last();
        var name = $"{stem}.id3v2-{pic.PictureType}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), pic.PictureData);
        written++;
    }
}

foreach (var flac in streams.OfType<FlacStream>())
{
    foreach (var pic in flac.PictureMetadataBlocks)
    {
        var ext = pic.MimeType.Split('/').Last();
        var name = $"{stem}.flac-{pic.PictureType}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), pic.PictureData);
        written++;
    }
}

foreach (var mp4 in streams.OfType<Mp4Stream>())
{
    var idx = 0;
    foreach (var cover in mp4.Tag.CoverArt)
    {
        var ext = cover.Format.ToString().ToLowerInvariant();
        var name = $"{stem}.mp4-{idx++}.{ext}";
        File.WriteAllBytes(Path.Combine(outDir, name), cover.Data);
        written++;
    }
}

Console.WriteLine($"Wrote {written} picture(s) for {stem}.");
return 0;
```
