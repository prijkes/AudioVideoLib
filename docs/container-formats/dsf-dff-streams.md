# DsfStream / DffStream

DSD audio containers.

- **DSF** (LE): DSD chunk (file size + metadata pointer), fmt chunk
  (format version / channel type / sample frequency / bits/sample /
  sample count / block size), data chunk. Optional ID3v2 at the
  metadata pointer.
- **DFF** (BE): FRM8 form with FVER / PROP (itself nested with FS /
  CHNL / CMPR) / DSD / optional DIIN / COMT / ID3 sub-chunks.

```csharp
foreach (var dsf in streams.OfType<DsfStream>())
{
    Console.WriteLine($"DSF: {dsf.SampleRate} Hz, {dsf.Channels} ch, {dsf.BitsPerSample}-bit, {dsf.SampleCount} samples");
    Console.WriteLine($"  embedded ID3v2 title: {dsf.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}

foreach (var dff in streams.OfType<DffStream>())
{
    Console.WriteLine($"DFF: {dff.SampleRate} Hz, {dff.Channels} ch ({string.Join(",", dff.ChannelIds)})");
}
```

For the embedded-ID3v2 model itself see
[DSF / DFF](../tag-formats/dsf-dff.md).
