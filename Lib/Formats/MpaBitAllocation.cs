/*
 * Date: 2013-01-28
 * Sources used: 
 *  http://sourceforge.net/tracker/index.php?func=detail&aid=3534143&group_id=979&atid=100979
 *  https://github.com/Sjord/checkmate/blob/master/mpck/layer2.c
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Public class for MPEG audio frames.
    /// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
    /// </summary>
    public sealed partial class MpaFrame
    {
        private class BitAllocation
        {
            /// <summary>
            /// Gets or sets the number of bits allocated.
            /// </summary>
            public short BitsAllocated { get; set; }

            /// <summary>
            /// Gets or sets the offset of the bits.
            /// </summary>
            public short Offset { get; set; }
        }
    }
}
