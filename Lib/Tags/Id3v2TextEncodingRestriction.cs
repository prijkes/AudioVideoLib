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
    /// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> encoding restrictions.
    /// </summary>
    public enum Id3v2TextEncodingRestriction
    {
        /// <summary>
        /// No restrictions.
        /// </summary>
        NoRestriction = 0x00,

        /// <summary>
        /// Strings are only encoded with ISO-8859-1 [ISO-8859-1] or UTF-8 [UTF-8].
        /// </summary>
        EncodingRestricted = 0x01
    }
}
