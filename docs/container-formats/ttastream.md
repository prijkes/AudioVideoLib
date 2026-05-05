# TtaStream

Walks a TrueAudio (`.tta`) stream — the 22-byte TTA1 fixed header,
the per-frame seek table, and the resulting list of audio frames.
`TtaStream.Header`, `TtaStream.SeekTable`, and `TtaStream.Frames`
expose the parsed model; tag editing for the surrounding APEv2 footer,
ID3v1 footer, and ID3v2 header is handled via the existing `AudioTags`
scanner, not on the walker itself.

## On-disk layout

A TrueAudio file (per `3rdparty/libtta-c-2.3/libtta.c`) is laid out as:

1. **Optional ID3v2 header** at offset 0. Recognised and skipped by the
   outer container scanner before the walker is dispatched.
2. **Fixed 22-byte TTA1 header.** Four-byte magic `TTA1`, followed by
   five little-endian info fields — format (1 = simple, 2 = encrypted),
   channel count (<=6), bits per sample (16-24), sample rate, and total
   samples — and a four-byte CRC32 over the preceding 18 bytes.
3. **Seek table.** One little-endian uint32 per frame giving the
   compressed frame length, followed by a single uint32 CRC32 over the
   table.
4. **Audio frames.** Each frame's start offset is fixed by the seek
   table; the first frame begins immediately after the seek-table CRC.
   Every frame except the last carries `256 * SampleRate / 245`
   samples; the last frame carries the remainder.
5. **Optional APEv2 footer / ID3v1 footer** trailing the audio data.
   Recognised by `AudioTags`, not by the walker.

## Inspecting a TTA stream

```csharp
var tta = streams.OfType<TtaStream>().First();

var header = tta.Header!;
Console.WriteLine($"{header.SampleRate} Hz, {header.NumChannels} ch, " +
                  $"{header.BitsPerSample}-bit, {header.TotalSamples} samples");

Console.WriteLine($"Frames: {tta.Frames.Count}");
foreach (var frame in tta.Frames.Take(3))
{
    Console.WriteLine($"  @{frame.StartOffset:X8}  len={frame.Length}  samples={frame.SampleCount}");
}
```

## Save semantics

`TtaStream.WriteTo` is a byte-for-byte passthrough. The walker holds an
`ISourceReader` populated by `ReadStream`; on save the original audio
container span is streamed verbatim to the destination. There is no
audio re-encoding path. Callers must keep the source `Stream` alive
between `ReadStream` and `WriteTo`; calling `WriteTo` after `Dispose`
throws `InvalidOperationException` with the message
`"Source stream was detached or never read. WriteTo requires a live source."`.
