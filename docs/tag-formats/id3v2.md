# ID3v2

**Spec:** [id3.org](https://id3.org) — 2.2.0 (1998), 2.3.0 (1999),
2.4.0 (2000).

**Shape:** Frames prefixed with a 10-byte header (6 bytes for 2.2).
Each frame has an identifier ("TIT2", "APIC", …), a flags word, and a
payload whose layout is identifier-specific.

**Encodings:** Per-frame, one of Latin-1, UTF-16LE with BOM, UTF-16BE,
UTF-8 (2.4+ only).

**Parse errors don't abort the tag:** `Id3v2TagReader.FrameParseError`
(and the bubbling `AudioTags.Id3v2FrameParseError`) fire for each frame
that can't be constructed. The walker skips that frame and continues.

**Unsynchronization:** 2.3 and 2.4 support `$FF $00` escaping to keep
the tag from being mistaken for an MPEG frame sync. The library
resynchronizes on read and re-unsynchronizes on write when the flag is
set.

**Extended header CRC:** 2.3 and 2.4 can carry a CRC-32 over the frame
data. `Id3v2TagReader` validates it on read; `Id3v2Tag.ToByteArray()`
recomputes it on write.

**Language codes:** Frames with a language subfield (`COMM`, `USLT`,
etc.) accept both ISO 639-2/B (bibliographic, e.g. `dut`) and
ISO 639-2/T (terminological, e.g. `nld`). The library also accepts
`XXX` (unknown) per 2.4+.

```csharp
// Read the well-known text frames; properties hide the version-specific
// identifier mapping (TIT2 / TT2, TYER vs TDRC, etc.).
var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();
var title  = id3v2.TrackTitle?.Values.FirstOrDefault();
var artist = id3v2.Artist?.Values.FirstOrDefault();
var year   = id3v2.RecordingTime?.Values.FirstOrDefault()    // 2.4
          ?? id3v2.YearRecording?.Values.FirstOrDefault();   // 2.2 / 2.3
```

```csharp
// Set a TIT2, asking for UTF-8 encoding (only honoured by 2.4+).
var title = new Id3v2TextFrame(id3v2.Version, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF8 };
title.Values.Add("Track 1");
id3v2.TrackTitle = title;
```

```csharp
// Bulk-convert every text frame in a 2.4 tag to UTF-8.
if (id3v2.Version >= Id3v2Version.Id3v240)
{
    foreach (var frame in id3v2.GetFrames<Id3v2TextFrame>())
    {
        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
    }
}
```

```csharp
// Extract every embedded picture.
foreach (var pic in id3v2.AttachedPictures)
{
    var ext = pic.ImageFormat.Split('/').Last();
    File.WriteAllBytes($"cover-{pic.PictureType}.{ext}", pic.PictureData);
}
```
