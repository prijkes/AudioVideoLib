/*
 * Date: 2013-10-16
 * Sources used: 
 */
using System.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Defines the offset for an <see cref="IAudioTag"/> in a <see cref="Stream"/>
    /// </summary>
    public class AudioTagOffset : IAudioTagOffset
    {
        /// <summary>
        /// Gets the tag origin.
        /// </summary>
        /// <value>
        /// The tag origin.
        /// </value>
        public TagOrigin TagOrigin { get; private set; }

        /// <summary>
        /// Gets the start offset in the stream.
        /// </summary>
        /// <value>
        /// The start offset, counting from the start of the stream.
        /// </value>
        public long StartOffset { get; private set; }

        /// <summary>
        /// Gets the end offset in the stream.
        /// </summary>
        /// <value>
        /// The end offset, counting from the start of the stream.
        /// </value>
        public long EndOffset { get; private set; }

        /// <summary>
        /// Gets the audio tag.
        /// </summary>
        /// <value>
        /// The audio tag.
        /// </value>
        public IAudioTag AudioTag { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTagOffset" /> class.
        /// </summary>
        /// <param name="tagOrigin">The tag origin.</param>
        /// <param name="startOffset">The start offset.</param>
        /// <param name="endOffset">The end offset.</param>
        /// <param name="audioTag">The audio tag.</param>
        public AudioTagOffset(TagOrigin tagOrigin, long startOffset, long endOffset, IAudioTag audioTag)
        {
            TagOrigin = tagOrigin;
            StartOffset = startOffset;
            EndOffset = endOffset;
            AudioTag = audioTag;
        }
    }
}
