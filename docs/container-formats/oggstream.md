# OggStream

Page-by-page walker. Each `OggPage` records its header (version, flags,
granule position, serial, sequence, CRC, segment count, payload size)
and byte range. On the first beginning-of-stream page the walker peeks
at the payload and identifies the codec (`"vorbis"` or `"opus"`),
extracting the channel count and sample rate for duration calculation.

```csharp
var ogg = streams.OfType<OggStream>().First();
Console.WriteLine($"{ogg.Codec}: {ogg.Channels} ch @ {ogg.SampleRate} Hz across {ogg.PageCount} pages");

foreach (var page in ogg.Pages.Take(3))
{
    Console.WriteLine($"  page {page.SequenceNumber}: {page.PayloadSize} bytes, granule {page.GranulePosition}");
}
```
