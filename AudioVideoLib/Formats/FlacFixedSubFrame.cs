namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a fixed-predictor subframe.
/// </summary>
public sealed class FlacFixedSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded warm-up samples.
    /// </summary>
    /// <value>
    /// The unencoded warm-up samples.
    /// </value>
    public int[] UnencodedWarmUpSamples { get; private set; } = null!;

    /// <summary>
    /// Gets the residual.
    /// </summary>
    /// <value>
    /// The residual.
    /// </value>
    public FlacResidual Residual { get; private set; } = null!;

    // RFC 9639 §11.25 + §11.28: subframe types 0b001000..0b001100 are FIXED, with
    // predictor order encoded as (type - 0b001000), i.e. type bits 6..1 minus 8.
    private int Order => ((Header >> 1) & 0x3F) - 0x08;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>
    /// RFC 9639 §11.28: a fixed-predictor subframe is <c>order</c> warm-up samples
    /// (each <c>sampleSize</c> bits, signed) followed by a residual block.
    /// </remarks>
    protected override void Read(BitStream bs, int sampleSize, int blockSize)
    {
        var order = Order;
        UnencodedWarmUpSamples = new int[order];
        for (var i = 0; i < order; i++)
        {
            UnencodedWarmUpSamples[i] = bs.ReadSignedInt32(sampleSize);
        }

        Residual = FlacResidual.Read(bs, blockSize, order);
    }
}
