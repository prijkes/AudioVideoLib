namespace AudioVideoLib.Formats;

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
}
