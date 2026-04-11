/*
 * Date: 2013-01-20
 * Sources used: 
 */

using System.IO;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Interface for an audio format to implement streaming.
    /// </summary>
    public interface IAudioStream
    {
        /// <summary>
        /// Gets the start offset of the <see cref="IAudioStream"/>, where it starts in the stream.
        /// </summary>
        /// <value>
        /// The start offset of the <see cref="IAudioStream"/>, counting from the start of the stream.
        /// </value>
        long StartOffset { get; }

        /// <summary>
        /// Gets the end offset of the <see cref="IAudioStream"/>, where it ends in the stream.
        /// </summary>
        /// <value>
        /// The end offset of the <see cref="IAudioStream"/>, counting from the start of the stream.
        /// </value>
        long EndOffset { get; }

        /// <summary>
        /// Gets the total length of audio in milliseconds.
        /// </summary>
        /// <value>
        /// The total length of audio, in milliseconds.
        /// </value>
        long TotalAudioLength { get; }

        /// <summary>
        /// Gets the total size of audio data in bytes.
        /// </summary>
        /// <value>
        /// The total size of the audio data in the stream, in bytes.
        /// </value>
        long TotalAudioSize { get; }

        /// <summary>
        /// Gets or sets the max length of spacing, in bytes, between 2 frames when searching for frames.
        /// </summary>
        /// <value>
        /// The max length of spacing.
        /// </value>
        /// <remarks>
        /// When searching for frames, spacing might exist between 2 frames.
        /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
        /// </remarks>
        int MaxFrameSpacingLength { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the audio stream from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// true if the audio stream was successfully read; otherwise, false.
        /// </returns>
        bool ReadStream(Stream stream);

        /// <summary>
        /// Places the <see cref="IAudioStream"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="IAudioStream"/>.
        /// </returns>
        byte[] ToByteArray();

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        string ToString();
    }
}
