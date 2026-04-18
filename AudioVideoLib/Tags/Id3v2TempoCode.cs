namespace AudioVideoLib.Tags;

/// <summary>
/// Class for storing a tempo code.
/// </summary>
/// <remarks>
/// Each tempo code consists of one tempo part and one time part.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2TempoCode"/> class.
/// </remarks>
/// <param name="beatsPerMinute">The beats per minute, in the range of 2 - 510 BPM.</param>
/// <param name="timeStamp">The time stamp.</param>
public sealed class Id3v2TempoCode(int beatsPerMinute, int timeStamp)
{
    /// <summary>
    /// Gets the beats per minute, in the range of 2 - 510 BPM.
    /// </summary>
    /// <remarks>
    /// The values 0 and 1 are reserved.
    /// 0 is used to describe a beat-free time period, which is not the same as a music-free time period.
    /// 1 is used to indicate one single beat-stroke followed by a beat-free period.
    /// </remarks>
    public int BeatsPerMinute { get; init; } = beatsPerMinute;

    /// <summary>
    /// Gets the time stamp.
    /// </summary>
    public int TimeStamp { get; init; } = timeStamp;
}
