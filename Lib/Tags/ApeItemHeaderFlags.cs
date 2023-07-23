/*
 * Date: 2011-04-11
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
    /// Class to store an Ape Item in an <see cref="ApeTag"/>.
    /// </summary>
    public partial class ApeItem
    {
        /// <summary>
        /// Field flags.
        /// </summary>
        private struct HeaderFlags
        {
            /// <summary>
            /// Flag indicating whether the tag or item is read only.
            /// </summary>
            public const int ReadOnly = 0x01;

            /// <summary>
            /// Flag indicating the item type. See <see cref="ApeItemType"/> for possible values.
            /// </summary>
            public const int ItemType = 0x06;
        }
    }
}
