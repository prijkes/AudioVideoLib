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
    /// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> text fields size restrictions.
    /// </summary>
    /// <remarks>
    /// Note that nothing is said about how many bytes is used to represent those characters, since it is encoding dependent.
    /// If a text frame consists of more than one string, the sum of the strings is restricted as stated.
    /// </remarks>
    public enum Id3v2TextFieldsSizeRestriction
    {
        /// <summary>
        /// No restrictions.
        /// </summary>
        NoRestrictions = 0x00,

        /// <summary>
        /// No string is longer than 1024 characters.
        /// </summary>
        Max1024Characters = 0x01,

        /// <summary>
        /// No string is longer than 128 characters.
        /// </summary>
        Max128Characters = 0x02,

        /// <summary>
        /// No string is longer than 30 characters.
        /// </summary>
        Max30Characters = 0x03
    }
}
