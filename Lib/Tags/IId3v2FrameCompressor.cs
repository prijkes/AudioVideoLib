/*
 * Date: 2013-11-17
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Interface for handling <see cref="Id3v2Frame"/> compression/decompression.
    /// </summary>
    public interface IId3v2FrameCompressor
    {
        /// <summary>
        /// Compresses the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// The compressed data.
        /// </returns>
        byte[] Compress(byte[] data);

        /// <summary>
        /// Decompresses the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <returns>
        /// The decompressed data.
        /// </returns>
        byte[] Decompress(byte[] data, int dataSize);
    }
}
