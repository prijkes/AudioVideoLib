# DsfStream / DffStream

DSD audio containers.

- **DSF** (LE): DSD chunk (file size + metadata pointer), fmt chunk
  (format version / channel type / sample frequency / bits/sample /
  sample count / block size), data chunk. Optional ID3v2 at the
  metadata pointer.
- **DFF** (BE): FRM8 form with FVER / PROP (itself nested with FS /
  CHNL / CMPR) / DSD / optional DIIN / COMT / ID3 sub-chunks.

## On-disk layout

### DSF

Sony's DSF (DSD Stream File, 2005) is a little-endian chunked format
opened by a 28-byte `DSD ` chunk that carries the total file size and
an absolute byte offset to optional ID3v2 metadata located elsewhere
in the file. A `fmt ` chunk follows with format parameters (format
version, channel type, sample rate, bits per sample, sample count,
block size per channel), then a `data` chunk holds the raw 1-bit DSD
samples.

The pointer-to-metadata design lets the ID3v2 block sit anywhere
after the audio data without requiring contiguous layout, which
simplifies in-place metadata edits — only the pointer field needs
patching when the tag block grows or shrinks.

Embedded ID3v2 metadata is reachable via `DsfStream.EmbeddedId3v2`.

### DFF

Philips's DFF (DSD Interchange File Format, 2004) is a big-endian IFF
form: the file is a single `FRM8` chunk with form-type `DSD `,
inside which the structural primitive is the IFF chunk (4-byte ID +
8-byte BE size + payload, padded to even length).

The form's children include `FVER` (format version), a nested `PROP`
form holding sub-chunks `FS` (sample rate), `CHNL` (channel count and
identifiers), and `CMPR` (compression descriptor); a `DSD ` chunk
carrying the audio payload; and optional metadata chunks `DIIN`
(description info), `COMT` (comments), and `ID3 ` (ID3v2 tag). The
nested `PROP` form keeps sample rate and channel configuration
logically grouped separate from the audio data.

Embedded ID3v2 metadata is reachable via `DffStream.EmbeddedId3v2`.

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
