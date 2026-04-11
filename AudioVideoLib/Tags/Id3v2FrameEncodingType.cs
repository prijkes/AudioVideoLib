/*
 * Date: 2011-06-17
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Id3v2 encoding values used to specify how to encode text frames.
    /// </summary>
    public enum Id3v2FrameEncodingType
    {
        /// <summary>
        /// The default text encoding; ISO-8859-1 [ISO-8859-1].
        /// Terminated with 0x00.
        /// </summary>
        Default = 0x00,

        /// <summary>
        /// UTF-16 Little Endian [UTF-16] encoded Unicode [UNICODE] with BOM.
        /// All strings in the same frame SHALL have the same byte order.
        /// Terminated with 0x00 0x00.
        /// </summary>
        UTF16LittleEndian = 0x01,

        /// <summary>
        /// UTF-16 Big Endian [UTF-16] encoded Unicode [UNICODE] with BOM.
        /// All strings in the same frame SHALL have the same byte order.
        /// Terminated with 0x00 0x00.
        /// </summary>
        UTF16BigEndian = 0x02,

        /// <summary>
        /// UTF-16 Big Endian [UTF-16] encoded Unicode [UNICODE] without BOM.
        /// Terminated with 0x00 0x00.
        /// </summary>
        /// <remarks>
        /// Only supported in version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        UTF16BigEndianWithoutBom = 0x03,

        /// <summary>
        /// UTF-8 [UTF-8] encoded Unicode [UNICODE].
        /// Terminated with 0x00.
        /// </summary>
        /// <remarks>
        /// Only supported in version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        UTF8 = 0x04,

        /// <summary>
        /// UTF-7 [UTF-7] encoded Unicode [UNICODE] with BOM. Not officially supported.
        /// </summary>
        /// <remarks>
        /// Not supported in any version; included for testing purpose only.
        /// </remarks>
        UTF7 = 0x05
    }
}
