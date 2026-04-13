namespace AudioVideoLib.Tags;

/// <summary>
/// Class for storing a key event.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2KeyEvent"/> class.
/// </remarks>
/// <param name="eventType">Type of the event.</param>
/// <param name="timeStamp">The time stamp.</param>
public class Id3v2KeyEvent(Id3v2KeyEventType eventType, int timeStamp)
{

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    /// <value>
    /// The type of the event.
    /// </value>
    /// <remarks>
    /// Terminating the start events such as <see cref="Id3v2KeyEventType.IntroStart"/> is OPTIONAL.
    /// The 'Not predefined synch's (0xE0 - 0xEF) are for user events.
    /// You might want to synchronize your music to something, like setting off an explosion on-stage, activating a screensaver, etcetera.
    /// </remarks>
    public Id3v2KeyEventType EventType { get; private set; } = eventType;

    /// <summary>
    /// Gets the Timestamp.
    /// </summary>
    /// <remarks>
    /// The Timestamp is set to zero if directly at the beginning of the sound or after the previous event.
    /// </remarks>
    public int TimeStamp { get; private set; } = timeStamp;
}
