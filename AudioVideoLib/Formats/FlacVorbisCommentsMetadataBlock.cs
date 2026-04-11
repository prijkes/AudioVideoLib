/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public class FlacVorbisCommentsMetadataBlock : FlacMetadataBlock
    {
        /// <inheritdoc/>
        public override FlacMetadataBlockType BlockType
        {
            get
            {
                return FlacMetadataBlockType.VorbisComment;
            }
        }

        /// <inheritdoc/>
        public override byte[] Data
        {
            get
            {
                return VorbisComments.ToByteArray();
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                VorbisComments = VorbisComments.ReadStream(new StreamBuffer(value));
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the vorbis comment.
        /// </summary>
        /// <value>
        /// The vorbis comment.
        /// </value>
        public VorbisComments VorbisComments { get; set; }
    }
}
