/*
 * Date: 2013-10-26
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 field.
    /// </summary>
    public partial class Lyrics3v2Field
    {
        /// <summary>
        /// Reads an <see cref="Lyrics3v2Field"/> from a <see cref="Stream"/> at the current position.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="maximumFieldSize">Maximum size of the field.</param>
        /// <returns>
        /// An <see cref="Lyrics3v2Field"/> if found; otherwise, null.
        /// </returns>
        public static Lyrics3v2Field ReadFromStream(Stream stream, long maximumFieldSize)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return ReadField(stream as StreamBuffer ?? new StreamBuffer(stream), maximumFieldSize);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static Lyrics3v2Field ReadField(StreamBuffer sb, long maximumFieldSize)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            string identifier = sb.ReadString(FieldIdentifierLength, false, false);
            Lyrics3v2Field field = GetField(identifier);
            return field.ReadFieldInstance(sb, maximumFieldSize) ? field : null;
        }

        private bool ReadFieldInstance(StreamBuffer sb, long maximumFieldSize)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            string identifier = sb.ReadString(FieldIdentifierLength);
            string strFieldSize = sb.ReadString(FieldSizeLength);

            int fieldSize;
            if (!Int32.TryParse(strFieldSize, out fieldSize))
            {
#if DEBUG
                return false;//throw new InvalidDataException(String.Format("Size value for field {0} is not an int: {1}", identifier, strFieldSize));
#else
                return false;
#endif
            }
            Identifier = identifier;
            _data = new byte[fieldSize];
            sb.Read(_data, fieldSize);

            if (!IsValidData(_data))
                return false;

            Data = _data;
            return true;
        }
    }
}
