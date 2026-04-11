/*
 * Date: 2013-03-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public sealed class FlacVerbatimSubFrame : FlacSubFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlacVerbatimSubFrame"/> class.
        /// </summary>
        /// <param name="flacFrame">The <see cref="FlacFrame"/>.</param>
        public FlacVerbatimSubFrame(FlacFrame flacFrame) : base(flacFrame)
        {
            if (flacFrame == null)
                throw new ArgumentNullException("flacFrame");
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the unencoded subblock.
        /// </summary>
        /// <value>
        /// The unencoded subblock.
        /// </value>
        public int[] UnencodedSubblocks { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the specified stream buffer.
        /// </summary>
        /// <param name="sb">The stream buffer.</param>
        /// <param name="sampeSize">Size of the sample.</param>
        /// <param name="blockSize">Size of the block.</param>
        protected override void Read(StreamBuffer sb, int sampeSize, int blockSize)
        {
            UnencodedSubblocks = new int[blockSize];
            for (int i = 0; i < blockSize; i++)
                UnencodedSubblocks[i] = sb.ReadBigEndianInt32();
        }
    }
}
