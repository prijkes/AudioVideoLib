/*
 * Date: 2012-10-24
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Enumerator for the Emphasis bit.
    /// </summary>
    public enum MpaFrameEmphasis
    {
        /// <summary>
        /// No emphasis set.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Emphasis is set to 50/15 milliseconds.
        /// </summary>
        Half = 0x01,

        /// <summary>
        /// Reserved field.
        /// </summary>
        Reserved = 0x02,

        /// <summary>
        /// Emphasis is set to CCIT J.17.
        /// </summary>
        Ccit = 0x03
    }
}
