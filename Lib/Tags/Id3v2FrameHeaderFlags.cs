/*
 * Date: 2011-06-12
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
    /// The header for an <see cref="Id3v2Frame"/>.
    /// </summary>
    public partial class Id3v2Frame
    {
        /// <summary>
        /// Id3v2.3.0 frame header flags.
        /// </summary>
        //// %abc00000 %ijk00000
        private struct Id3v230FrameHeaderFlags
        {
            /// <summary>
            /// This flag tells the software what to do with this frame if it is unknown and the tag is altered in any way.
            /// This applies to all kinds of alterations, including adding more padding and reordering the frames.
            /// </summary>
            /// <value>
            /// 0    Frame should be preserved.
            /// 1    Frame should be discarded.
            /// </value>
            //// Id3v2.3.0 - a
            public const int TagAlterPreservation = 0x8000;

            /// <summary>
            /// This flag tells the software what to do with this frame if it is unknown and the file, excluding the tag, is altered.
            /// This does not apply when the audio is completely replaced with other audio data.
            /// </summary>
            /// <value>
            /// 0    Frame should be preserved.
            /// 1    Frame should be discarded.
            /// </value>
            //// Id3v2.3.0 - b
            public const int FileAlterPreservation = 0x4000;

            /// <summary>
            /// This flag, if set, tells the software that the contents of this frame is intended to be read only.
            /// Changing the contents might break something, e.g. a signature.
            /// If the contents are changed, without knowledge in why the frame was flagged read only and without taking the proper means to compensate,
            /// e.g. recalculating the signature, the bit should be cleared.
            /// </summary>
            //// Id3v2.3.0 - c
            public const int ReadOnly = 0x2000;

            /// <summary>
            /// This flag indicates whether or not the frame is compressed.
            /// </summary>
            /// <value>
            /// 0    Frame is not compressed.
            /// 1    Frame is compressed using [#ZLIB zlib] with 4 bytes for 'decompressed size' appended to the frame header.
            /// </value>
            //// Id3v2.3.0 - i
            public const int Compressed = 0x80;

            /// <summary>
            /// This flag indicates whether or not the frame is encrypted.
            /// If set one byte indicating with which method it was encrypted will be appended to the frame header.
            /// </summary>
            /// <value>
            /// 0    Frame is not encrypted.
            /// 1    Frame is encrypted.
            /// </value>
            //// Id3v2.3.0 - j
            public const int Encrypted = 0x40;

            /// <summary>
            /// This flag indicates whether or not this frame belongs in a group with other frames.
            /// If set a group identifier byte is added to the frame header.
            /// Every frame with the same group identifier belongs to the same group.
            /// </summary>
            /// <value>
            /// 0    Frame does not contain group information.
            /// 1    Frame contains group information.
            /// </value>
            //// Id3v2.3.0 - k
            public const int GroupingIdentity = 0x20;
        }

        /// <summary>
        /// Id3v2.4.0 frame header flags.
        /// </summary>
        //// %0abc0000 %0h00kmnp
        private struct Id3v240FrameHeaderFlags
        {
            /// <summary>
            /// This flag tells the tag parser what to do with this frame if it is unknown and the tag is altered in any way.
            /// This applies to all kinds of alterations, including adding more padding and reordering the frames.
            /// </summary>
            /// <value>
            /// 0     Frame should be preserved.
            /// 1     Frame should be discarded.
            /// </value>
            //// Id3v2.4.0 - a
            public const int TagAlterPreservation = 0x4000;

            /// <summary>
            /// This flag tells the tag parser what to do with this frame if it is unknown and the file, excluding the tag, is altered.
            /// This does not apply when the audio is completely replaced with other audio data.
            /// </summary>
            /// <value>
            /// 0     Frame should be preserved.
            /// 1     Frame should be discarded.
            /// </value>
            //// Id3v2.4.0 - b
            public const int FileAlterPreservation = 0x2000;

            /// <summary>
            /// This flag, if set, tells the software that the contents of this frame are intended to be read only.
            /// Changing the contents might break something, e.g. a signature.
            /// If the contents are changed, without knowledge of why the frame was flagged read only 
            /// and without taking the proper means to compensate, e.g. recalculating the signature, the bit MUST be cleared
            /// </summary>
            //// Id3v2.4.0 - c
            public const int ReadOnly = 0x1000;

            /// <summary>
            /// This flag indicates whether or not this frame belongs in a group with other frames.
            /// If set, a group identifier byte is added to the frame.
            /// Every frame with the same group identifier belongs to the same group.
            /// </summary>
            /// <value>
            /// 0     Frame does not contain group information.
            /// 1     Frame contains group information.
            /// </value>
            //// Id3v2.4.0 - h
            public const int GroupingIdentity = 0x40;

            /// <summary>
            /// This flag indicates whether or not the frame is compressed.
            /// </summary>
            /// <value>
            /// 0     Frame is not compressed.
            /// 1     Frame is compressed using zlib [zlib] deflate method. 
            ///       If set, this requires the 'Data Length Indicator' bit to be set as well.
            /// </value>
            /// <remarks>
            /// A 'Data Length Indicator' byte MUST be included in the frame.
            /// </remarks>
            //// Id3v2.4.0 - k
            public const int Compressed = 0x8;

            /// <summary>
            /// This flag indicates whether or not the frame is encrypted.
            /// If set, one byte indicating with which method it was encrypted will be added to the frame.
            /// See description of the ENCR frame for more information about encryption method registration.
            /// Encryption should be done after compression.
            /// Whether or not setting this flag requires the presence of a 'Data Length Indicator' depends on the specific algorithm used.
            /// </summary>
            /// <value>
            /// 0     Frame is not encrypted.
            /// 1     Frame is encrypted.
            /// </value>
            //// Id3v2.4.0 - m
            public const int Encrypted = 0x4;

            /// <summary>
            /// This flag indicates whether or not unsynchronization was applied to this frame.
            /// If this flag is set all data from the end of this header to the end of this frame has been unsynchronized.
            /// Although desirable, the presence of a 'Data Length Indicator' is not made mandatory by unsynchronization.
            /// </summary>
            /// <value>
            /// 0     Frame has not been unsynchronized.
            /// 1     Frame has been unsynchronized.
            /// </value>
            //// Id3v2.4.0 - n
            public const int Unsynchronized = 0x2;

            /// <summary>
            /// This flag indicates that a data length indicator has been added to the frame.
            /// The data length indicator is the value one would write as the 'Frame length' if all of the frame format flags were zeroed,
            /// represented as a 32 bit synchsafe integer.
            /// </summary>
            /// <value>
            /// 0      There is no Data Length Indicator.
            /// 1      A data length Indicator has been added to the frame.
            /// </value>
            //// Id3v2.4.0 - p
            public const int DataLengthIndicator = 0x1;
        }
    }
}
