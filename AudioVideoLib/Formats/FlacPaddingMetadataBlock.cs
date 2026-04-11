/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public class FlacPaddingMetadataBlock : FlacMetadataBlock
    {
        /// <inheritdoc/>
        public override FlacMetadataBlockType BlockType
        {
            get
            {
                return FlacMetadataBlockType.Padding;
            }
        }

        /// <inheritdoc/>
        public override byte[] Data
        {
            get
            {
                return base.Data;
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                base.Data = value;
            }
        }
    }
}
