/*
 * Date: 2013-01-26
 * Sources used: 
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Interface for audio frames.
    /// </summary>
    public interface IAudioFrame
    {
        /// <summary>
        /// Gets the start offset of the <see cref="IAudioFrame"/>, where it starts in the stream.
        /// </summary>
        /// <value>
        /// The start offset of the <see cref="IAudioFrame"/>, counting from the start of the stream.
        /// </value>
        long StartOffset { get; }

        /// <summary>
        /// Gets the end offset of the <see cref="IAudioFrame"/>, where it ends in the stream.
        /// </summary>
        /// <value>
        /// The end offset of the <see cref="IAudioFrame"/>, counting from the start of the stream.
        /// </value>
        long EndOffset { get; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the sampling rate per second in Hz of the audio.
        /// </summary>
        /// <value>
        /// The sampling rate per second, in Hz, of the audio.
        /// </value>
        int SamplingRate { get; }

        /// <summary>
        /// Gets the size of a sample, in bits.
        /// </summary>
        /// <value>
        /// The size of a sample, in bits.
        /// </value>
        int SampleSize { get; }

        /// <summary>
        /// Gets the bitrate of the audio, in KBit.
        /// </summary>
        /// <value>
        /// The bitrate of the audio, in KBit.
        /// </value>
        int Bitrate { get; }

        /// <summary>
        /// Gets the frame length, in bytes.
        /// </summary>
        /// <value>
        /// The length of the frame, in bytes.
        /// </value>
        /// <remarks>
        /// Length is the length of a frame in bytes when compressed.
        /// </remarks>
        int FrameLength { get; }

        /// <summary>
        /// Gets the frame size of the current frame, this is the number of samples contained in the frame.
        /// </summary>
        /// <value>
        /// The number of samples in the frame.
        /// </value>
        /// <remarks>
        /// Frame size is the number of samples contained in a frame.
        /// </remarks>
        int FrameSize { get; }

        /// <summary>
        /// Gets the length of audio, in milliseconds.
        /// </summary>
        /// <value>
        /// The length of audio, in milliseconds.
        /// </value>
        long AudioLength { get; }
    }
}
