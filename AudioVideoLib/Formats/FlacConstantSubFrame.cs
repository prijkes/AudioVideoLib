namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a constant-signal subframe.
/// </summary>
public sealed class FlacConstantSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded constant value of the subblock.
    /// </summary>
    /// <value>
    /// The unencoded constant value of the subblock.
    /// </value>
    public int UnencodedConstantValue { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>
    /// RFC 9639 §11.26: the constant subframe payload is a single sample of
    /// <c>sampleSize</c> bits — the constant value repeated for the whole block.
    /// </remarks>
    protected override void Read(BitStream bs, int sampleSize, int blockSize)
    {
        UnencodedConstantValue = bs.ReadSignedInt32(sampleSize);
    }
}
