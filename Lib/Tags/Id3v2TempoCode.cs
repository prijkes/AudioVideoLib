/*
 * Date: 2011-08-27
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
    /// Class for storing a tempo code.
    /// </summary>
    /// <remarks>
    /// Each tempo code consists of one tempo part and one time part.
    /// </remarks>
    public class Id3v2TempoCode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2TempoCode"/> class.
        /// </summary>
        /// <param name="beatsPerMinute">The beats per minute, in the range of 2 - 510 BPM.</param>
        /// <param name="timeStamp">The time stamp.</param>
        public Id3v2TempoCode(int beatsPerMinute, int timeStamp)
        {
            BeatsPerMinute = beatsPerMinute;
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// Gets the beats per minute, in the range of 2 - 510 BPM.
        /// </summary>
        /// <remarks>
        /// The values 0 and 1 are reserved.
        /// 0 is used to describe a beat-free time period, which is not the same as a music-free time period.
        /// 1 is used to indicate one single beat-stroke followed by a beat-free period.
        /// </remarks>
        public int BeatsPerMinute { get; private set; }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        public int TimeStamp { get; private set; }
    }
}
