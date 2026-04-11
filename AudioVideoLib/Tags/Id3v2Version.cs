/*
 * Date: 2011-06-02
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
    /// Enumerator to indicate the Id3v2 version.
    /// </summary>
    public enum Id3v2Version
    {
        /// <summary>
        /// Indicates version Id3v2.2.0.
        /// </summary>
        Id3v220 = 20,

        /// <summary>
        /// Indicates version Id3v2.2.1.
        /// </summary>
        Id3v221 = 21,

        /// <summary>
        /// Indicates version Id3v2.3.0.
        /// </summary>
        Id3v230 = 30,

        /// <summary>
        /// Indicates version Id3v2.4.0.
        /// </summary>
        Id3v240 = 40
    }
}
