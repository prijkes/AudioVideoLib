/*
 * Date: 2013-01-12
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
    /// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> image size restrictions.
    /// </summary>
    public enum Id3v2ImageSizeRestriction
    {
        /// <summary>
        /// No restrictions.
        /// </summary>
        NoRestrictions = 0x00,

        /// <summary>
        /// All images are 256x256 pixels or smaller.
        /// </summary>
        Max256X256Pixels = 0x01,

        /// <summary>
        /// All images are 64x64 pixels or smaller.
        /// </summary>
        Max64X64Pixels = 0x02,

        /// <summary>
        /// All images are exactly 64x64 pixels, unless required otherwise.
        /// </summary>
        Exactly64X64Pixels = 0x03
    }
}
