namespace AudioVideoLib.Tags;

/// <summary>
/// Sign indicator in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
/// </summary>
public enum Id3v2ReplayGainSign
{
    /// <summary>
    /// A positive sign.
    /// </summary>
    Positive = 0x00,

    /// <summary>
    /// A negative sign.
    /// </summary>
    Negative = 0x01
}
