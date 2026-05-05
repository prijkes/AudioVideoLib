namespace AudioVideoLib.Formats;

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
}
