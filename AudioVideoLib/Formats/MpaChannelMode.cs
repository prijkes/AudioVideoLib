/*
 * Date: 2012-10-24
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Enumerator for the Channel Mode bit.
    /// </summary>
    public enum MpaChannelMode
    {
        /// <summary>
        /// Channel mode is Stereo.
        /// </summary>
        Stereo = 0x00,

        /// <summary>
        /// Channel mode is Joint stereo (Stereo).
        /// </summary>
        JointStereo = 0x01,

        /// <summary>
        /// Channel mode is Dual channel (Stereo).
        /// </summary>
        DualChannel = 0x02,

        /// <summary>
        /// Channel mode is Single Channel (Mono).
        /// </summary>
        SingleChannel = 0x03
    }
}
