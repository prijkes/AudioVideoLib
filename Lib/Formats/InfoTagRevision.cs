/*
 * Date: 2013-01-20
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://gabriel.mp3-tech.org/mp3infotag.html
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Possible Info Tag revision values.
    /// </summary>
    public struct InfoTagRevision
    {
        /// <summary>
        /// Indicates an info tag revision 0.
        /// </summary>
        public const int Revision0 = 0x0;

        /// <summary>
        /// Indicates an info tag revision 1.
        /// </summary>
        public const int Revision1 = 0x1;

        /// <summary>
        /// Indicates a reserved info tag value.
        /// </summary>
        public const int Reserved = 0xF;
    }
}
