namespace AudioVideoLib.Tags;

/// <summary>
/// The 'interpolation method' describes which method is preferred when an interpolation between the adjustment point that follows.
/// </summary>
public enum Id3v2InterpolationMethod
{
    /// <summary>
    /// No interpolation is made. A jump from one adjustment level to another occurs in the middle between two adjustment points.
    /// </summary>
    Band = 0x00,

    /// <summary>
    /// Interpolation between adjustment points is linear.
    /// </summary>
    Linear = 0x01
}
