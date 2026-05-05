# MacStream

Walks a Monkey's Audio (`.ape`) container in version 3.98 or later — the
fixed `APE_DESCRIPTOR`, the `APE_HEADER`, the seek table, and the
resulting list of audio frames. `MacStream.Descriptor`,
`MacStream.Header`, `MacStream.SeekEntries`, and `MacStream.Frames`
expose the parsed model; the trailing APEv2 / ID3v1 tag footers are
handled by the existing `AudioTags` scanner, not by the walker. The
class is named `MacStream` — for Monkey's Audio Codec, the upstream
project's own name — to keep it distinct from the unrelated
`ApeTag` family.

## On-disk layout

A 3.98+ Monkey's Audio file (per `3rdparty/MAC_1284_SDK/Source/`) is
laid out as:

1. **`APE_DESCRIPTOR`** at the start of the audio span. Begins with one
   of two four-byte magic markers — `MAC ` (ASCII, trailing space) for
   integer-PCM source, surfaced as `MacFormat.Integer`, or `MACF` for
   IEEE-float source, surfaced as `MacFormat.Float`. The walker
   dispatches on the magic and exposes the chosen variant through
   `MacStream.Format`. The descriptor self-describes its own size
   (`DescriptorBytes`, currently 52), the header size
   (`HeaderBytes`, currently 24), the seek-table size
   (`SeekTableBytes`), the optional preserved-WAV-header size
   (`HeaderDataBytes`), the audio span size split across
   `ApeFrameDataBytes` (low 32) and `ApeFrameDataBytesHigh` (high 32 —
   combined via `TotalApeFrameDataBytes`), the trailing junk size
   (`TerminatingDataBytes`), and a 16-byte file MD5.
2. **`APE_HEADER`.** Compression level, format-flags bitfield (the
   walker recognises bit 5, `MAC_FORMAT_FLAG_CREATE_WAV_HEADER`,
   surfaced as `MacHeader.CreatesWavHeaderOnDecode`), blocks-per-frame,
   final-frame block count, total frame count, bits per sample,
   channel count, and sample rate.
3. **Seek table.** `SeekTableBytes / 4` little-endian uint32 file
   offsets, surfaced as `MacStream.SeekEntries`. Frame `i`'s start
   offset equals `SeekEntries[i].FileOffset`; frame `i`'s length runs
   to `SeekEntries[i+1].FileOffset` (or, for the last frame, to
   `SeekEntries[0].FileOffset + Descriptor.TotalApeFrameDataBytes`).
4. **Preserved WAV header (optional).** `Descriptor.HeaderDataBytes`
   bytes copied verbatim from the source `.wav`. Zero when
   `CreatesWavHeaderOnDecode` is set on the header — in that case the
   decoder synthesises a fresh RIFF wrapper, so no source bytes are
   carried along.
5. **APE audio frames.** `Descriptor.TotalApeFrameDataBytes` bytes of
   encoded audio, addressed by the seek table. Every frame except the
   last carries `Header.BlocksPerFrame` blocks; the last frame carries
   `Header.FinalFrameBlocks`.
6. **Terminating data (optional).** `Descriptor.TerminatingDataBytes`
   bytes of trailing junk preserved from the source WAV (typically
   the trailing `data` padding or a trailing `LIST INFO` chunk).
7. **Optional APEv2 / ID3v1 tag footers** trailing the audio span.
   Recognised by `AudioTags`, not by the walker.

## Inspecting an APE stream

```csharp
var ape = streams.OfType<MacStream>().First();

Console.WriteLine($"Format       : {ape.Format}");                  // Integer or Float
Console.WriteLine($"Sample rate  : {ape.Header!.SampleRate} Hz");
Console.WriteLine($"Channels     : {ape.Header.Channels}");
Console.WriteLine($"Bits/sample  : {ape.Header.BitsPerSample}");
Console.WriteLine($"Total frames : {ape.Header.TotalFrames}");
Console.WriteLine($"Compression  : {ape.Header.CompressionLevel}");

foreach (var frame in ape.Frames)
{
    Console.WriteLine(
        $"  frame @{frame.StartOffset:X8}  len={frame.Length}  blocks={frame.BlockCount}");
}
```

## Save semantics

`MacStream.WriteTo` is a byte-for-byte passthrough. The walker holds an
`ISourceReader` populated by `ReadStream`; on save the original audio
container span is streamed verbatim to the destination. There is no
audio re-encoding path, and `MacStream` does not touch the trailing
APEv2 / ID3v1 footer regions — tag editing flows through `AudioTags`,
which writes a new file the caller then re-parses. Callers must keep
the source `Stream` alive between `ReadStream` and `WriteTo`; calling
`WriteTo` after `Dispose` throws `InvalidOperationException` with the
message
`"Source stream was detached or never read. WriteTo requires a live source."`.

Pre-3.98 files (`APE_HEADER_OLD`, no descriptor) are not supported:
`ReadStream` returns `false` for those, as it does for any file whose
magic is neither `MAC ` nor `MACF`.
