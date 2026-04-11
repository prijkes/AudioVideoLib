/*
 * Date: 2013-01-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Possible VBR header types in a MPA stream.
    /// </summary>
    public enum VbrHeaderType
    {
        /// <summary>
        /// Indicates a <see cref="XingHeader"/>.
        /// </summary>
        Xing = 0x01,

        /// <summary>
        /// Indicates a <see cref="VbriHeader"/>.
        /// </summary>
        Vbri = 0x02
    }
}
