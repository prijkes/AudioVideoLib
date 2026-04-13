namespace AudioVideoLib.Tags;

/// <summary>
/// Name code in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
/// </summary>
public enum Id3v2NameCode
{
    /// <summary>
    /// Not set.
    /// </summary>
    NotSet = 0x00,

    /// <summary>
    /// Radio Gain Adjustment.
    /// </summary>
    RadioGainAdjustment = 0x01,

    /// <summary>
    /// Audiophile Gain Adjustment.
    /// </summary>
    AudiophileGainAdjustment = 0x02,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved1 = 0x03,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved2 = 0x04,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved3 = 0x05,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved4 = 0x06,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved5 = 0x07
}
