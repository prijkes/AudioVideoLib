# FlacStream

Walks the FLAC metadata blocks (`STREAMINFO`, `APPLICATION`, `PADDING`,
`SEEKTABLE`, `VORBIS_COMMENT`, `CUESHEET`, `PICTURE`) followed by frames.
`FlacStream.MetadataBlocks` surfaces the blocks; the Vorbis Comment
block holds the tag text via `VorbisComments`.

## On-disk layout

FLAC (Free Lossless Audio Codec, defined in RFC 9639) is built around
a linear sequence of metadata blocks followed by audio frames. The
file begins with a 4-byte magic identifier `fLaC`, after which zero
or more metadata blocks precede the audio frame stream.

Each metadata block opens with a 4-byte header: the first byte
contains a 1-bit "last block" flag and a 7-bit block-type identifier;
the next three bytes encode the block length in big-endian form.
Block types include STREAMINFO (essential audio parameters),
VORBIS_COMMENT (text tags), SEEKTABLE (sample-accurate seek points),
CUESHEET (CD-style cue points), PICTURE (embedded images), PADDING,
and APPLICATION. STREAMINFO conventionally appears first and supplies
the sample rate, channel count, bit depth, and total sample count
needed to decode the stream.

The frame stream begins immediately after the block flagged as last.
Each frame contains a sync code and audio samples; frames may not
align to metadata-block boundaries, and the walker tolerates small
spacing gaps during parsing.

`FlacStream.MetadataBlocks` surfaces every block as an enumerable;
the typed accessors `StreamInfoMetadataBlocks`,
`VorbisCommentsMetadataBlock`, and `PictureMetadataBlocks` reach
specific block types directly.

```csharp
var flac = streams.OfType<FlacStream>().First();

foreach (var block in flac.MetadataBlocks)
{
    Console.WriteLine(block.BlockType);
}

var info = flac.StreamInfoMetadataBlocks.FirstOrDefault();
if (info is not null)
{
    Console.WriteLine($"{info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit, {info.TotalSamples} samples");
}

foreach (var pic in flac.PictureMetadataBlocks)
{
    var ext = pic.MimeType.Split('/').Last();
    File.WriteAllBytes($"flac-{pic.PictureType}.{ext}", pic.PictureData);
}
```
