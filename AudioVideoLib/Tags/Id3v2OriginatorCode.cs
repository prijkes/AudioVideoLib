namespace AudioVideoLib.Tags;

/// <summary>
/// Originator code in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
/// </summary>
public enum Id3v2OriginatorCode
{
    /// <summary>
    /// The replay gain is unspecified.
    /// </summary>
    Unspecified = 0x00,

    /// <summary>
    /// The replay gain is pre-set by the artist/producer/mastering engineer.
    /// </summary>
    PreSetByProducer = 0x01,

    /// <summary>
    /// The replay gain is set by user.
    /// </summary>
    SetByUser = 0x02,

    /// <summary>
    /// The replay gain is determined automatically.
    /// </summary>
    DeterminedAutomatically = 0x03,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved1 = 0x04,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved2 = 0x05,

    /// <summary>
    /// Reserved value for future use.
    /// </summary>
    Reserved3 = 0x06
}
