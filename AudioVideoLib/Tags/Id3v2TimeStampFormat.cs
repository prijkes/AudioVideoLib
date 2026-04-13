namespace AudioVideoLib.Tags;

/// <summary>
/// The time stamp format of event timing codes.
/// </summary>
/// <remarks>
/// Absolute time means that every stamp contains the time from the beginning of the file.
/// </remarks>
public enum Id3v2TimeStampFormat
{
    /// <summary>
    /// Absolute time, 32 bit sized, using MPEG [MPEG] frames as unit.
    /// </summary>
    AbsoluteTimeMpegFrames = 0x01,

    /// <summary>
    /// Absolute time, 32 bit sized, using milliseconds as unit.
    /// </summary>
    AbsoluteTimeMilliseconds = 0x02
}
