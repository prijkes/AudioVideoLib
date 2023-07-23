/*
 * Date: 2011-06-02
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    public partial class Id3v2Frame
    {
        /// <summary>
        /// Static class with functions related to frame encoding for <see cref="Id3v2Frame"/>s.
        /// </summary>
        protected static class Id3v2FrameEncoding
        {
            /// <summary>
            /// The encoding values to use when writing the encoding type to a byte array.
            /// </summary>
            //// [_encodingType] -> ISO-8859-1, UTF16LE, UTF16BE, UTF16BEbomless, UTF8, UTF7
            private static readonly byte[] EncodingTypes = { 0, 1, 1, 2, 3, 4 };

            /// <summary>
            /// Gets the encoding for the specific encoding type.
            /// </summary>
            /// <param name="encodingType">Type of the encoding.</param>
            /// <returns>
            /// The encoding for the specific encoding type.
            /// </returns>
            /// <remarks>
            /// See <see cref="Id3v2FrameEncodingType"/> for possible values.
            /// </remarks>
            public static Encoding GetEncoding(Id3v2FrameEncodingType encodingType)
            {
                switch (encodingType)
                {
                    case Id3v2FrameEncodingType.Default:
                        return Encoding.GetEncoding("ISO-8859-1");

                    case Id3v2FrameEncodingType.UTF16LittleEndian:
                    case Id3v2FrameEncodingType.UTF16BigEndian:
                        return (encodingType == Id3v2FrameEncodingType.UTF16LittleEndian)
                                   ? new UnicodeEncoding(false, true)
                                   : new UnicodeEncoding(true, true);

                    case Id3v2FrameEncodingType.UTF16BigEndianWithoutBom:
                        return new UnicodeEncoding(true, false);

                    case Id3v2FrameEncodingType.UTF8:
                        return new UTF8Encoding(false);

                    case Id3v2FrameEncodingType.UTF7:
                        return new UTF7Encoding();
                }
                return Encoding.Default;
            }

            /// <summary>
            /// Reads the <see cref="Id3v2FrameEncodingType"/> from the stream.
            /// </summary>
            /// <param name="streamBuffer">The stream buffer.</param>
            /// <returns>
            /// The <see cref="Id3v2FrameEncodingType"/>.
            /// </returns>
            /// <remarks>
            /// Since the encoding type is first read from the stream, the position should be set to the byte containing the encoding type.
            /// If the encoding type is <see cref="Id3v2FrameEncodingType.UTF16LittleEndian"/>, 
            /// it will try to read the byte order marker from the stream 
            /// to see if the encoding type is <see cref="Id3v2FrameEncodingType.UTF16BigEndian"/>.
            /// If no byte order marker is found or if the byte order marker is not recognized, 
            /// it will return <see cref="Id3v2FrameEncodingType.UTF16LittleEndian"/> as default encoding.
            /// <para />
            /// The position of the stream will not be modified.
            /// </remarks>
            public static Id3v2FrameEncodingType ReadEncodingTypeFromStream(StreamBuffer streamBuffer)
            {
                int encodingByte = streamBuffer.ReadByte();
                if ((encodingByte == -1) || !IsValidEncodingType(encodingByte))
                    return Id3v2FrameEncodingType.Default;

                if (encodingByte > (byte)Id3v2FrameEncodingType.UTF16LittleEndian)
                    encodingByte += 1;

                // Cast the encoding type.
                Id3v2FrameEncodingType encodingType = (Id3v2FrameEncodingType)encodingByte;

                // If it's not little endian, then we don't need to figure out if it's little or big; return here.
                if (encodingType != Id3v2FrameEncodingType.UTF16LittleEndian)
                    return encodingType;

                // See which byte order the frame encoding is.
                byte[] byteOrderMarker = new byte[2];

                // Not enough bytes available; probably little endian then.
                if (streamBuffer.Read(byteOrderMarker, 2, false) < 2)
                    return Id3v2FrameEncodingType.UTF16LittleEndian;

                // Little endian byte order marker?
                if (((byteOrderMarker[0] == 0xFF) && (byteOrderMarker[1] == 0xFE))
                    || ((byteOrderMarker[0] == 0x00) && (byteOrderMarker[1] == 0x00)))
                    return Id3v2FrameEncodingType.UTF16LittleEndian;

                // Big endian byte order marker?
                if ((byteOrderMarker[0] == 0xFE) && (byteOrderMarker[1] == 0xFF))
                    return Id3v2FrameEncodingType.UTF16BigEndian;

                // Default to little endian.
                return Id3v2FrameEncodingType.UTF16LittleEndian;
            }

            /// <summary>
            /// Gets the preamble of the specified encoding.
            /// </summary>
            /// <param name="encodingType">Type of the encoding.</param>
            /// <returns>The preamble of the encoding, or an empty byte array if the encoding does not use a preamble.</returns>
            public static byte[] GetEncodingPreamble(Id3v2FrameEncodingType encodingType)
            {
                switch (encodingType)
                {
                    case Id3v2FrameEncodingType.Default:
                    case Id3v2FrameEncodingType.UTF16BigEndianWithoutBom:
                    case Id3v2FrameEncodingType.UTF8:
                    case Id3v2FrameEncodingType.UTF7:
                        return new byte[0];

                    case Id3v2FrameEncodingType.UTF16LittleEndian:
                    case Id3v2FrameEncodingType.UTF16BigEndian:
                        return
                            ((encodingType == Id3v2FrameEncodingType.UTF16LittleEndian) ? Encoding.Unicode : Encoding.BigEndianUnicode).GetPreamble();

                    default:
                        throw new ArgumentOutOfRangeException("encodingType");
                }
            }

            /// <summary>
            /// Determines whether the supplied byte order marker is valid.
            /// See the supported encodings at <see cref="Id3v2FrameEncodingType"/> for which the byte order marker is recognized, 
            /// if available.
            /// </summary>
            /// <param name="bom">The byte order marker.</param>
            /// <returns>
            ///   <c>true</c> if the supplied byte order marker is valid; otherwise, <c>false</c>.
            /// </returns>
            public static bool IsValidBom(byte[] bom)
            {
                if (bom != null)
                {
                    // UTF-16
                    // Little Endian
                    // 0xFF 0xFE
                    // - or -
                    // Big Endian
                    // 0xFE 0xFF
                    if ((bom.Length >= 2) && (((bom[0] == 0xFF) && (bom[1] == 0xFE)) || ((bom[0] == 0xFE) && (bom[1] == 0xFF))))
                        return true;

                    // UTF-8
                    // 0xEF 0xBB 0xBF
                    // - or -
                    // UTF-7
                    // 0x2B 0x2F 0x76
                    if ((bom.Length >= 3)
                        && (((bom[0] == 0xEF) && (bom[1] == 0xBB) && (bom[2] == 0xBF)) || ((bom[0] == 0x2B) && (bom[1] == 0x2F) && (bom[3] == 0x76))))
                        return true;

                    // UTF-32
                    // Little Endian
                    // 0xFF 0xFE 0x00 0x00
                    // - or -
                    // Big Endian
                    // 0x00 0x00 0xFE 0xFF
                    if ((bom.Length >= 4)
                        && (((bom[0] == 0xFF) && (bom[1] == 0xFE) && (bom[2] == 0x00) && (bom[3] == 0x00))
                            || ((bom[0] == 0x00) && (bom[1] == 0x00) && (bom[2] == 0xFE) && (bom[3] == 0xFF))))
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Gets the length of the byte order marker. <see cref="Id3v2FrameEncodingType"/> for possible encoding types.
            /// </summary>
            /// <param name="encodingType">Type of the encoding.</param>
            /// <returns>The length of the byte order marker, in bytes.</returns>
            public static int GetBomLength(Id3v2FrameEncodingType encodingType)
            {
                switch (encodingType)
                {
                    case Id3v2FrameEncodingType.Default:
                    case Id3v2FrameEncodingType.UTF16BigEndianWithoutBom:
                        return 0;

                    case Id3v2FrameEncodingType.UTF16BigEndian:
                    case Id3v2FrameEncodingType.UTF16LittleEndian:
                        return 2;

                    case Id3v2FrameEncodingType.UTF8:
                    case Id3v2FrameEncodingType.UTF7:
                        return 3;
                }
                return 0;
            }

            /// <summary>
            /// Gets the real value of the encoding type as defined in the Id3v2 specs.
            /// </summary>
            /// <param name="encodingType">Type of the encoding.</param>
            /// <returns>The real value of the encoding type, as defined in the Id3v2 specs.</returns>
            public static byte GetEncodingTypeValue(Id3v2FrameEncodingType encodingType)
            {
                if ((byte)encodingType >= EncodingTypes.Length)
                    throw new ArgumentOutOfRangeException("encodingType");

                return EncodingTypes[(byte)encodingType];
            }

            // Valid values are byte 0 till 3, the others are values defined by this library and not part of the specs.
            private static bool IsValidEncodingType(int encodingType)
            {
                return encodingType >= (byte)Id3v2FrameEncodingType.Default && encodingType <= (byte)Id3v2FrameEncodingType.UTF16BigEndianWithoutBom;
            }
        }

        /// <summary>
        /// Determines whether the specified encoding type is valid.
        /// </summary>
        /// <param name="encodingType">Type of the encoding.</param>
        /// <returns>
        ///   <c>true</c> if the specified encoding type is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidEncodingType(Id3v2FrameEncodingType encodingType)
        {
            return Enum.TryParse(encodingType.ToString(), true, out encodingType);
        }
    }
}
