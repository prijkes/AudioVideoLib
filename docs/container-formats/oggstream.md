# OggStream

Page-by-page walker. Each `OggPage` records its header (version, flags,
granule position, serial, sequence, CRC, segment count, payload size)
and byte range. On the first beginning-of-stream page the walker peeks
at the payload and identifies the codec (`"vorbis"` or `"opus"`),
extracting the channel count and sample rate for duration calculation.

## On-disk layout

An Ogg bitstream (RFC 3533) is built around pages as the fundamental
structural unit. Each page begins with the four-byte magic `OggS`,
followed by a 23-byte fixed header containing the page version, flags,
granule position (a codec-specific sample count), serial number
(logical bitstream identifier), sequence number, CRC-32 checksum, and
segment count. A variable-length segment table — one byte per segment
giving the segment's length — immediately follows the header; the
page payload is the concatenation of those segments.

The flags byte encodes three single-bit fields: continuation (`0x01`,
this page continues a packet from a prior page), beginning-of-stream
(`0x02`, the first page of a logical bitstream — where codec
identification happens), and end-of-stream (`0x04`, the final page).
All multi-byte integers in the header are little-endian.

```text
OggS │ version │ flags │ granule │ serial │ seq │ CRC │ segs │ seg-table │ payload
4    │ 1       │ 1     │ 8       │ 4      │ 4   │ 4   │ 1    │ N         │ variable
```

Inspect individual pages via `OggStream.Pages`; codec / channel /
sample-rate fields surface from the beginning-of-stream peek as
`OggStream.Codec`, `OggStream.Channels`, and `OggStream.SampleRate`.

```csharp
var ogg = streams.OfType<OggStream>().First();
Console.WriteLine($"{ogg.Codec}: {ogg.Channels} ch @ {ogg.SampleRate} Hz across {ogg.PageCount} pages");

foreach (var page in ogg.Pages.Take(3))
{
    Console.WriteLine($"  page {page.SequenceNumber}: {page.PayloadSize} bytes, granule {page.GranulePosition}");
}
```
