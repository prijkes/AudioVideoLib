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
    /// Class to store an Id3v2 tag.
    /// </summary>
    public sealed partial class Id3v2ExtendedHeader
    {
        /// <summary>
        /// Id3v2.3.0 Extended header flags.
        /// The extended flags are a secondary flag set which describes further attributes of the tag.
        /// </summary>
        //// Id3v2.3.0 (2 bytes) - %x0000000 00000000
        private struct Id3v230ExtendedHeaderFlags
        {
            /// <summary>
            /// If this flag is set four bytes of CRC-32 data is appended to the extended header.
            /// The CRC should be calculated before unsynchronization on the data between the extended header 
            /// and the padding, i.e. the frames and only the frames.
            /// </summary>
            public const int CrcPresent = 0x8000;
        }

        /// <summary>
        /// Id3v2.4.0 Extended header flags.
        /// The extended header contains information that can provide further insight in the structure of the tag,
        /// but is not vital to the correct parsing of the tag information; hence the extended header is optional.
        /// </summary>
        //// Id3v2.4.0 (1 byte) - %0bcd0000
        private struct Id3v240ExtendedHeaderFlags
        {
            /// <summary>
            /// If this flag is set, the present tag is an update of a tag found earlier in the present file or stream.
            /// If frames defined as unique are found in the present tag, they are to override any corresponding ones found in the earlier tag.
            /// This flag has no corresponding data.
            /// </summary>
            public const int TagIsUpdate = 0x40;

            /// <summary>
            ///  If this flag is set, a CRC-32 [ISO-3309] data is included in the extended header.
            ///  The CRC is calculated on all the data between the header and footer as indicated by the header's tag length field,
            ///  minus the extended header. Note that this includes the padding (if there is any), but excludes the footer.
            ///  The CRC-32 is stored as an 35 bit synchsafe integer, leaving the upper four bits always zeroed.
            /// </summary>
            public const int CrcPresent = 0x20;

            /// <summary>
            /// For some applications it might be desired to restrict a tag in more ways than imposed by the Id3v2 specification.
            /// Note that the presence of these restrictions does not affect how the tag is decoded, merely how it was restricted before encoding.
            /// If this flag is set the tag is restricted as follows:
            /// </summary>
            public const int TagIsRestricted = 0x10;
        }
    }
}
