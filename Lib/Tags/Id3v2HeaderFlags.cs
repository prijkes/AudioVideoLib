/*
 * Date: 2011-08-28
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
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
    public partial class Id3v2Tag
    {
        /// <summary>
        /// Id3v2.2.0 Header flags.
        /// </summary>
        //// %ab000000
        private struct Id3v220HeaderFlags
        {
            /// <summary>
            /// Indicates whether or not unsynchronization is used; a set bit indicates usage.
            /// </summary>
            public const int Unsynchronization = 0x80;

            /// <summary>
            /// Indicates whether or not compression is used; a set bit indicates usage.
            /// Since no compression scheme has been decided yet, 
            /// the ID3 decoder (for now) should just ignore the entire tag if the compression bit is set.
            /// </summary>
            public const int Compression = 0x40;
        }

        /// <summary>
        /// Id3v2.3.0 Header flags, currently only three flags are used.
        /// </summary>
        //// %abc00000
        private struct Id3v230HeaderFlags
        {
            /// <summary>
            /// Indicates whether or not unsynchronization is used; a set bit indicates usage.
            /// </summary>
            public const int Unsynchronization = 0x80;

            /// <summary>
            /// Indicates whether or not the header is followed by an extended header. A set bit indicates the presence of an extended header.
            /// </summary>
            /// <remarks>
            /// This flag replaces the Id3v2.2.0 Compression flag.
            /// </remarks>
            public const int ExtendedHeader = 0x40;

            /// <summary>
            /// Should be used as an 'experimental indicator'. This flag should always be set when the tag is in an experimental stage.
            /// </summary>
            public const int ExperimentalIndicator = 0x20;
        }

        /// <summary>
        /// Id3v2.4.0 Header flags, currently four flags are used.
        /// </summary>
        //// %abcd0000
        private struct Id3v240HeaderFlags
        {
            /// <summary>
            /// Indicates whether or not unsynchronization is applied on all frames; a set bit indicates usage.
            /// </summary>
            public const int Unsynchronization = 0x80;

            /// <summary>
            /// Indicates whether or not the header is followed by an extended header. A set bit indicates the presence of an extended header.
            /// </summary>
            public const int ExtendedHeader = 0x40;

            /// <summary>
            /// Used as an 'experimental indicator'. This flag SHALL always be set when the tag is in an experimental stage.
            /// </summary>
            public const int ExperimentalIndicator = 0x20;

            /// <summary>
            /// Indicates that a footer is present at the very end of the tag. A set bit indicates the presence of a footer.
            /// </summary>
            public const int Footer = 0x10;
        }
    }
}
