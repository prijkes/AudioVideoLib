/*
 * Date: 2011-10-22
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTag
    {
        /// <summary>
        /// <see cref="ApeTag"/> header flags.
        /// </summary>
        //// Footer (and header) flags
        private struct ApeHeaderFlags
        {
            /// <summary>
            /// Flag indicating whether the <see cref="ApeTag"/> contains a header.
            /// </summary>
            public const int ContainsHeader = 1 << 31;

            /// <summary>
            /// Flag indicating whether the <see cref="ApeTag"/> does not contain a footer.
            /// </summary>
            public const int ContainsNoFooter = 1 << 30;

            /// <summary>
            /// Flag indicating whether the header instance is the header or footer.
            /// </summary>
            public const int IsHeader = 1 << 29;

            /// <summary>
            /// Flag indicating whether the <see cref="ApeTag"/> is read only or read/write.
            /// </summary>
            public const int IsReadOnly = 1 << 0;
        }
    }
}
