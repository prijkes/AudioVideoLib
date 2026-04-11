/*
 * Date: 2013-10-16
 * Sources used: 
 */
using System.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Interface for audio tag readers.
    /// </summary>
    public interface IAudioTagReader
    {
        /// <summary>
        /// Occurs when the audio tag raises an exception.
        /// </summary>
        //event EventHandler<EventArgs> AudioTagException;

        /// <summary>
        /// Reads an <see cref="IAudioTagOffset" /> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="tagOrigin">The tag origin in the stream to start reading.</param>
        /// <returns>
        /// An <see cref="IAudioTagOffset" /> instance with the tag and offset info; or null when the tag could not be found.
        /// </returns>
        IAudioTagOffset ReadFromStream(Stream stream, TagOrigin tagOrigin);
    }
}
