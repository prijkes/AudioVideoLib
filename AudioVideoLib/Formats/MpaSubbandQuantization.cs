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
        // possible quantization per sub band table
        private class SubbandQuantization
        {
            private int[] _offsets = new int[30];

            /// <summary>
            /// Gets or sets the sub band limit.
            /// </summary>
            public int SubbandLimit { get; set; }

            /// <summary>
            /// Gets or sets the offsets.
            /// </summary>
            public int[] Offsets
            {
                get
                {
                    return _offsets;
                }

                set
                {
                    _offsets = value;
                }
            }
        }
    }
}
