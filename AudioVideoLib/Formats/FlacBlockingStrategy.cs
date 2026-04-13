namespace AudioVideoLib.Formats;

/// <summary>
/// 
/// </summary>
public enum FlacBlockingStrategy
{
    /// <summary>
    /// Fixed-blocksize stream; frame header encodes the frame number.
    /// </summary>
    FixedBlocksize = 0,

    /// <summary>
    /// Fixed variable-blocksize stream; frame header encodes the sample number.
    /// </summary>
    VariableBlocksize = 1
}
