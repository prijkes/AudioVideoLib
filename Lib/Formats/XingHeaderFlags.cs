/*
 * Date: 2013-01-19
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.mp3-tech.org/programmer/decoding.html
 *  http://www.hydrogenaudio.org/forums/index.php?showtopic=85690
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// XING VBR Header flags.
    /// Flags which indicate what fields are present, flags are combined with a logical OR. Field is mandatory.
    /// </summary>
    public struct XingHeaderFlags
    {
        /// <summary>
        /// Number of frames in the file stored as Big-Endian unsigned int (optional), excluding the XING Frame (total - 1).
        /// </summary>
        public const int FrameCountFlag = 0x0001;

        /// <summary>
        /// Number of bytes in the file stored as Big-Endian unsigned int (optional)
        /// </summary>
        public const int FileSizeFlag = 0x0002;

        /// <summary>
        /// 100 TOC entries for seeking as integral byte (optional)
        /// </summary>
        public const int TocFlag = 0x0004;

        /// <summary>
        /// Quality indicator as Big-Endian unsigned int
        /// from 0 - best quality to 100 - worst quality (optional)
        /// </summary>
        public const int VbrScaleFlag = 0x0008;
    }
}
