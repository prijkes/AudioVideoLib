# DSF / DFF (DSD audio)

- **DSF** (Sony): DSD / fmt / data chunks (LE), optional ID3v2 metadata
  pointed to by the DSD chunk's metadata pointer field. Exposed as
  `DsfStream.EmbeddedId3v2`.
- **DFF** (Philips): FRM8 form (BE) with FVER / PROP / DSD / optional
  DIIN / COMT / ID3 sub-chunks. Exposed as `DffStream.EmbeddedId3v2`.

```csharp
// DSF / DFF expose any ID3v2 inside the container directly as a fully-parsed Id3v2Tag.
foreach (var dsf in streams.OfType<DsfStream>())
{
    Console.WriteLine($"DSF: {dsf.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}
foreach (var dff in streams.OfType<DffStream>())
{
    Console.WriteLine($"DFF: {dff.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}
```
