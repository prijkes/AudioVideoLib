# WAV / AIFF container-level metadata

WAV and AIFF carry metadata inside their container structure rather
than as a flat tag block, so it surfaces on the container walkers
(`RiffStream` / `AiffStream`) instead of through `AudioTags`.

- **WAV `LIST` chunk with form-type `INFO`:** ASCII key/value
  (`INAM`/`IART`/`IPRD`/`ICRD`/`ICMT`/`IGNR`/`ITRK`/`IENG`/`ISFT`/`ICOP`).
  Multiple `LIST INFO` chunks merge in file order.
- **WAV `bext` (BWF):** broadcast metadata — description, originator,
  origination date / time, time reference, UMID, loudness fields (v1+),
  coding history. Surfaced as `RiffStream.BextChunk`.
- **WAV `iXML`:** UTF-8 XML for film/audio production. Raw payload
  preserved for byte-identical round-trip.
- **WAV `id3 ` chunk:** full ID3v2 tag — parsed via the existing
  `Id3v2TagReader` and exposed as `RiffStream.EmbeddedId3v2`.
- **AIFF `NAME` / `AUTH` / `ANNO` / `COMT`:** ASCII text (or, per spec,
  Mac Roman). Exposed as `AiffStream.TextChunks` with typed
  `Name`, `Author`, `Annotation`, and `Comments` (timestamped).

```csharp
// All four WAV side-channels surface as their own properties on RiffStream.
var riff = streams.OfType<RiffStream>().Single();

if (riff.InfoTag is { } info)
{
    Console.WriteLine($"INFO {info.Title} / {info.Artist} ({info.CreationDate})");
}
if (riff.BextChunk is { } bext)
{
    Console.WriteLine($"BWF {bext.Description} – {bext.Originator} on {bext.OriginationDate}");
}
if (riff.IxmlChunk is { IsWellFormed: true } ixml)
{
    Console.WriteLine($"iXML PROJECT={ixml.ProjectName} SCENE={ixml.SceneName}");
}
if (riff.EmbeddedId3v2?.AudioTag is Id3v2Tag id3v2)
{
    Console.WriteLine($"ID3v2 in 'id3 ' chunk: {id3v2.TrackTitle?.Values.FirstOrDefault()}");
}
```

```csharp
// AIFF text chunks.
var aiff = streams.OfType<AiffStream>().Single();
if (aiff.TextChunks is { IsEmpty: false } text)
{
    Console.WriteLine($"NAME: {text.Name}");
    Console.WriteLine($"AUTH: {text.Author}");
    Console.WriteLine($"ANNO: {text.Annotation}");
    foreach (var c in text.Comments)
    {
        Console.WriteLine($"  COMT @ {c.TimeStampUtc:u}: {c.Text}");
    }
}
```
