/*
 * Date: 2013-03-01
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Flac channel assignment.
    /// </summary>
    public enum FlacChannelAssignment
    {
        /// <summary>
        /// Independent channel.
        /// </summary>
        Independent,

        /// <summary>
        /// The left side stereo.
        /// </summary>
        /// <remarks>
        /// Channel 0 is the left channel, channel 1 is the right channel.
        /// </remarks>
        LeftSide,

        /// <summary>
        /// The right side stereo.
        /// </summary>
        /// <remarks>
        /// Channel 0 is the side (difference) channel, channel 1 is the right channel.
        /// </remarks>
        RightSide,

        /// <summary>
        /// The mid side stereo.
        /// </summary>
        /// <remarks>
        /// Channel 0 is the mid (average) channel, channel 1 is the side (difference) channel.
        /// </remarks>
        MidSide,

        /// <summary>
        /// Channel assignment value is reserved.
        /// </summary>
        Reserved
    }
}
