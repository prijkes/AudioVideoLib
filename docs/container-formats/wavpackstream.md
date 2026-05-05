# WavPackStream

Walks a WavPack (`.wv`) file as a sequence of fixed-prefix blocks; each block
carries a 32-byte preamble plus a stream of metadata sub-blocks that describe
decorrelation terms, entropy variables, RIFF wrappers, optional sample rates,
and the audio bitstream itself. `WavPackStream.Blocks` surfaces every block
with its `Header`, `StartOffset`, `Length`, and `SubBlocks` list.

## On-disk layout

A WavPack file is a flat concatenation of blocks. Every block begins with the
ASCII magic `wvpk`, followed by 28 little-endian bytes that decode into the
`WavPackBlockHeader` model (block size, stream version, 40-bit total samples
split across `TotalSamplesLow` / `TotalSamplesHigh`, 40-bit block index split
across `BlockIndexLow` / `BlockIndexHigh`, block-samples count, flags
bitfield, and CRC). The `Flags` field encodes the sample-rate index, the
bytes-per-sample value, and the mono / hybrid / float / DSD bits along with
the multichannel sequencing markers exposed as `IsInitialBlock` and
`IsFinalBlock`.

Inside each block, after the 32-byte header, lives a sequence of metadata
sub-blocks. A sub-block opens with a single `id` byte; the high three bits
signal the size width (a 24-bit length header instead of 8-bit, surfaced as
`IsLargeSize`), an odd-size flag (payload is one byte short of the on-disk
word-aligned length), and an optional-data flag (id is from the 0x20 band,
covering things like the RIFF header and explicit sample-rate sub-blocks,
surfaced as `IsOptional`). The remaining low five bits — exposed as
`UniqueId` — identify the chunk: decorrelation terms, entropy vars, the
WV / WVC / WVX bitstream slices, channel info, MD5 checksum, and so on. The
walker records every sub-block as a `(RawId, PayloadOffset, PayloadLength)`
triple; the payload bytes can be materialised on demand via
`WavPackSubBlock.ReadPayload()`.

`WavPackStream.WriteTo(destination)` streams the original block bytes from
the source stream directly to the destination — there is no audio re-encode.
APEv2 and ID3v1 footers (carried *outside* the wvpk block stream) are managed
by the `AudioTags` scanner.

```csharp
var wv = streams.OfType<WavPackStream>().First();

foreach (var block in wv.Blocks)
{
    Console.WriteLine(
        $"block @ {block.StartOffset}: {block.Header.SampleRate} Hz, " +
        $"{block.Header.BytesPerSample}-byte samples, " +
        $"{(block.Header.IsMono ? "mono" : "stereo")}, " +
        $"{block.SubBlocks.Count} sub-blocks");

    foreach (var sub in block.SubBlocks)
    {
        Console.WriteLine($"  sub 0x{sub.RawId:x2} ({sub.PayloadLength} bytes)");
    }
}
```

Hybrid mode: when a `.wv` file contains correction-stream blocks interleaved
inline (rather than in a separate `.wvc` companion), the walker enumerates
those blocks too — no special-casing needed. Multi-file hybrid (audio in
`.wv`, residuals in `.wvc`) is out of scope and not currently supported.
