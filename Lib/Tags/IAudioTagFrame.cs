/*
 * Date: 2011-03-11
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Interface for implementing an tag frame.
    /// </summary>
    public interface IAudioTagFrame : IEquatable<IAudioTagFrame>
    {
        /// <summary>
        /// Gets the data and only the data of this frame.
        /// </summary>
        /// <value>
        /// The data of this frame as a byte array.
        /// </value>
        /// <remarks>
        /// The data should not include any header(s) and/or footer(s).
        /// The header(s) and/or footer(s) should be included when calling <see cref="ToByteArray()"/>, together with the data.
        /// </remarks>
        byte[] Data { get; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the whole frame into a byte array.
        /// </summary>
        /// <returns>A byte array that represents the frame.</returns>
        byte[] ToByteArray();
    }
}
