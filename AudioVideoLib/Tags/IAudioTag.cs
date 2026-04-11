/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Interface which tag implementations must implement.
    /// </summary>
    public interface IAudioTag : IEquatable<IAudioTag>
    {
        /// <summary>
        /// Places the <see cref="IAudioTag"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="IAudioTag"/>.
        /// </returns>
        byte[] ToByteArray();

        /// <summary>
        /// Returns a <see cref="String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="String" /> that represents this instance.
        /// </returns>
        string ToString();
    }
}
