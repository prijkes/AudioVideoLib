namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a verbatim subframe — uncompressed PCM samples.
/// </summary>
public sealed class FlacVerbatimSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded subblock.
    /// </summary>
    /// <value>
    /// The unencoded subblock.
    /// </value>
    public int[] UnencodedSubblocks { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>
    /// RFC 9639 §11.27: the verbatim subframe payload is <c>blockSize</c> samples,
    /// each <c>sampleSize</c> bits, signed, MSB-first within the bitstream.
    /// </remarks>
    protected override void Read(BitStream bs, int sampleSize, int blockSize)
    {
        UnencodedSubblocks = new int[blockSize];
        for (var i = 0; i < blockSize; i++)
        {
            UnencodedSubblocks[i] = bs.ReadSignedInt32(sampleSize);
        }
    }
}
