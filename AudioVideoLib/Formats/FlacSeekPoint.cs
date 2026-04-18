namespace AudioVideoLib.Formats;

/// <summary>
///
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FlacSeekPoint" /> class.
/// </remarks>
/// <param name="sampleNumber">The sample number.</param>
/// <param name="offset">The offset.</param>
/// <param name="samples">The samples.</param>
public sealed class FlacSeekPoint(long sampleNumber, long offset, int samples)
{
    /// <summary>
    /// Gets or sets the sample number of first sample.
    /// </summary>
    /// <value>
    /// The sample number, or 0xFFFFFFFFFFFFFFFF for a placeholder point.
    /// </value>
    public long SampleNumber { get; private set; } = sampleNumber;

    /// <summary>
    /// Gets or sets the offset.
    /// </summary>
    /// <value>
    /// The offset.
    /// </value>
    public long Offset { get; private set; } = offset;

    /// <summary>
    /// Gets or sets the number of samples.
    /// </summary>
    /// <value>
    /// The number of samples.
    /// </value>
    public int Samples { get; private set; } = samples;
}
