/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    public partial class FlacMetadataBlock
    {
        /// <summary>
        /// Field flags.
        /// </summary>
        private struct HeaderFlags
        {
            /// <summary>
            /// The last block bit.
            /// </summary>
            public const int IsLastBlock = 0x80;

            /// <summary>
            /// The block type bit.
            /// </summary>
            public const int BlockType = 0x7F;
        }
    }
}
