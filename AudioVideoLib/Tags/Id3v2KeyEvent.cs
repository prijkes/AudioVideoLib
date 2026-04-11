/*
 * Date: 2012-12-22
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing a key event.
    /// </summary>
    public class Id3v2KeyEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2KeyEvent"/> class.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="timeStamp">The time stamp.</param>
        public Id3v2KeyEvent(Id3v2KeyEventType eventType, int timeStamp)
        {
            EventType = eventType;
            TimeStamp = timeStamp;
        }

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
        public Id3v2KeyEventType EventType { get; private set; }

        /// <summary>
        /// Gets the Timestamp.
        /// </summary>
        /// <remarks>
        /// The Timestamp is set to zero if directly at the beginning of the sound or after the previous event.
        /// </remarks>
        public int TimeStamp { get; private set; }
    }
}
