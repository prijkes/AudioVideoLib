namespace AudioVideoLib.Formats;

/// <summary>
/// Enumerator for the Layer version bit.
/// </summary>
public enum MpaFrameLayerVersion
{
    /// <summary>
    /// Reserved field.
    /// </summary>
    Reserved = 0x00,

    /// <summary>
    /// Layer is Layer III.
    /// </summary>
    Layer3 = 0x01,

    /// <summary>
    /// Layer is Layer II.
    /// </summary>
    Layer2 = 0x02,

    /// <summary>
    /// Layer is Layer I.
    /// </summary>
    Layer1 = 0x03
}
