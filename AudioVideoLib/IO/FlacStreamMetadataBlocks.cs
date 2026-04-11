/*
 * Date: 2013-02-02
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Formats;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// The stream containing FLAC Audio <see cref="FlacFrame"/>s.
    /// </summary>
    public sealed partial class FlacStream
    {
        /// <summary>
        /// Gets the metadata blocks in the stream.
        /// </summary>
        /// <value>
        /// The metadata blocks in the stream.
        /// </value>
        public IEnumerable<FlacMetadataBlock> MetadataBlocks
        {
            get
            {
                return _metadataBlocks;
            }
        }

        /// <summary>
        /// Gets the stream info metadata blocks.
        /// </summary>
        /// <value>
        /// The stream info metadata blocks.
        /// </value>
        public IEnumerable<FlacStreamInfoMetadataBlock> StreamInfoMetadataBlocks
        {
            get
            {
                return _metadataBlocks.OfType<FlacStreamInfoMetadataBlock>();
            }
        }

        /// <summary>
        /// Gets the application metadata blocks.
        /// </summary>
        /// <value>
        /// The application metadata blocks.
        /// </value>
        public IEnumerable<FlacApplicationMetadataBlock> ApplicationMetadataBlocks
        {
            get
            {
                return _metadataBlocks.OfType<FlacApplicationMetadataBlock>();
            }
        }

        /// <summary>
        /// Gets the padding metadata blocks.
        /// </summary>
        /// <value>
        /// The padding metadata blocks.
        /// </value>
        public IEnumerable<FlacPaddingMetadataBlock> PaddingMetadataBlocks
        {
            get
            {
                return _metadataBlocks.OfType<FlacPaddingMetadataBlock>();
            }
        }

        /// <summary>
        /// Gets the seek table metadata block.
        /// </summary>
        /// <value>
        /// The seek table metadata block.
        /// </value>
        public FlacSeekTableMetadataBlock SeekTableMetadataBlock
        {
            get
            {
                return _metadataBlocks.OfType<FlacSeekTableMetadataBlock>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the vorbis comments metadata block.
        /// </summary>
        /// <value>
        /// The vorbis comments metadata block.
        /// </value>
        public FlacVorbisCommentsMetadataBlock VorbisCommentsMetadataBlock
        {
            get
            {
                return _metadataBlocks.OfType<FlacVorbisCommentsMetadataBlock>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the flac cue sheet metadata blocks.
        /// </summary>
        /// <value>
        /// The flac cue sheet metadata blocks.
        /// </value>
        public IEnumerable<FlacCueSheetMetadataBlock> FlacCueSheetMetadataBlocks
        {
            get
            {
                return _metadataBlocks.OfType<FlacCueSheetMetadataBlock>();
            }
        }

        /// <summary>
        /// Gets the picture metadata blocks.
        /// </summary>
        /// <value>
        /// The picture metadata blocks.
        /// </value>
        public IEnumerable<FlacPictureMetadataBlock> PictureMetadataBlocks
        {
            get
            {
                return _metadataBlocks.OfType<FlacPictureMetadataBlock>();
            }
        }
    }
}
