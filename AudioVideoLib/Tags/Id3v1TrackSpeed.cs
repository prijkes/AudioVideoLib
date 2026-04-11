/*
 * Date: 2012-11-17
 * Sources used:
 *  http://en.wikipedia.org/wiki/ID3#Extended_tag
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Enumerator values for the <see cref="Id3v1Tag.TrackSpeed"/> field in an extended <see cref="Id3v1Tag"/>.
    /// </summary>
    public enum Id3v1TrackSpeed
    {
        /// <summary>
        /// Indicates unset speed.
        /// </summary>
        Unset = 0x00,

        /// <summary>
        /// Indicates Slow speed.
        /// </summary>
        Slow = 0x01,

        /// <summary>
        /// Indicates Medium speed.
        /// </summary>
        Medium = 0x02,

        /// <summary>
        /// Indicates Fast speed.
        /// </summary>
        Fast = 0x03,

        /// <summary>
        /// Indicates Hardcore speed.
        /// </summary>
        Hardcore = 0x04
    }
}
