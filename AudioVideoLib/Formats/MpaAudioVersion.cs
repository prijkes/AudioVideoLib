/*
 * Date: 2011-11-05
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Enumerator for the MPEG Audio Version bit.
    /// </summary>
    public enum MpaAudioVersion
    {
        /// <summary>
        /// MPEG Version 2.5.
        /// </summary>
        Version25 = 0x00,

        /// <summary>
        /// Reserved field.
        /// </summary>
        Reserved = 0x01,

        /// <summary>
        /// MPEG Version 2 (ISO/IEC 13818-3).
        /// </summary>
        Version20 = 0x02,

        /// <summary>
        /// MPEG Version 1 (ISO/IEC 11172-3).
        /// </summary>
        Version10 = 0x03
    }
}
