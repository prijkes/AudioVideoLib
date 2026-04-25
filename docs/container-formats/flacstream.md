# FlacStream

Walks the FLAC metadata blocks (`STREAMINFO`, `APPLICATION`, `PADDING`,
`SEEKTABLE`, `VORBIS_COMMENT`, `CUESHEET`, `PICTURE`) followed by frames.
`FlacStream.MetadataBlocks` surfaces the blocks; the Vorbis Comment
block holds the tag text via `VorbisComments`.

```csharp
var flac = streams.OfType<FlacStream>().First();

foreach (var block in flac.MetadataBlocks)
{
    Console.WriteLine(block.BlockType);
}

var info = flac.StreamInfoMetadataBlocks.FirstOrDefault();
if (info is not null)
{
    Console.WriteLine($"{info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit, {info.TotalSamples} samples");
}

foreach (var pic in flac.PictureMetadataBlocks)
{
    var ext = pic.MimeType.Split('/').Last();
    File.WriteAllBytes($"flac-{pic.PictureType}.{ext}", pic.PictureData);
}
```
