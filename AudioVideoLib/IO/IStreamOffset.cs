/*
 * Date: 2013-10-16
 * Sources used: 
 */
using System.IO;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Defines the offset of an item in a <see cref="Stream"/>.
    /// </summary>
    public interface IStreamOffset
    {
        /// <summary>
        /// Gets the start offset in the stream.
        /// </summary>
        /// <value>
        /// The start offset, counting from the start of the stream.
        /// </value>
        long StartOffset { get; }

        /// <summary>
        /// Gets the end offset in the stream.
        /// </summary>
        /// <value>
        /// The end offset, counting from the start of the stream.
        /// </value>
        long EndOffset { get; }
    }
}
