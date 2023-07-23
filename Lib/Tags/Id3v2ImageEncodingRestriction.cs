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
    /// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> image encoding restrictions.
    /// </summary>
    public enum Id3v2ImageEncodingRestriction
    {
        /// <summary>
        /// No restrictions.
        /// </summary>
        NoRestrictions = 0x00,

        /// <summary>
        /// Images are encoded only with PNG [PNG] or JPEG [JFIF].
        /// </summary>
        ImageRestricted = 0x01
    }
}
