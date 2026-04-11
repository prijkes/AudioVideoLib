/*
 * Date: 2012-11-17
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 field.
    /// </summary>
    public partial class Lyrics3v2Field : IAudioTagFrame
    {
        private const int FieldIdentifierLengthBytes = 3;

        private const int FieldSizeLengthBytes = 5;

        private byte[] _data;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Lyrics3v2Field"/> class.
        /// </summary>
        /// <param name="identifier">The identifier of the field.</param>
        public Lyrics3v2Field(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            Identifier = identifier;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Lyrics3v2Field"/> class from being created.
        /// </summary>
        private Lyrics3v2Field()
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the field identifier.
        /// </summary>
        /// <value>
        /// The length of the field identifier.
        /// </value>
        public static int FieldIdentifierLength
        {
            get
            {
                return FieldIdentifierLengthBytes;
            }
        }

        /// <summary>
        /// Gets the length of the field size.
        /// </summary>
        /// <value>
        /// The length of the field size.
        /// </value>
        public static int FieldSizeLength
        {
            get
            {
                return FieldSizeLengthBytes;
            }
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        /// <value>
        /// The name of the field.
        /// </value>
        public virtual string Identifier { get; private set; }

        /// <summary>
        /// Gets or sets the value of the field.
        /// </summary>
        /// <value>
        /// The value of the field.
        /// </value>
        public virtual byte[] Data
        {
            get
            {
                return _data;
            }

            protected set
            {
                if ((value != null) && !IsValidData(value))
                    throw new InvalidDataException("Data contains one or more invalid values.");

                _data = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Determines whether the specified data is valid.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        ///   <c>true</c> if the specified data is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The data in a field can consist of ASCII characters in the range 01 to 254 according to the standard.
        /// </remarks>
        public static bool IsValidData(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // All characters should be in the range of 0x01 to 0xFE; only the last character may be 0x00.
            return !data.Where((c, i) => ((c <= 0x00) || (c >= 0xFF)) && ((c != 0x00) || (i != data.Length - 1))).Any();
        }

        /// <summary>
        /// Determines whether the specified string is valid.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>
        ///   <c>true</c> if the specified data is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The data in a field can consist of ASCII characters in the range 01 to 254 according to the standard.
        /// </remarks>
        public static bool IsValidString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            // All characters should be in the range of 0x01 to 0xFE; only the last character may be 0x00.
            return !value.Where((c, i) => ((c <= 0x00) || (c >= 0xFF)) && ((c != 0x00) || (i != value.Length - 1))).Any();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Lyrics3v2Field);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTagFrame audioFrame)
        {
            return Equals(audioFrame as Lyrics3v2Field);
        }

        /// <summary>
        /// Equals the specified <see cref="Lyrics3v2Field"/>.
        /// </summary>
        /// <param name="field">The <see cref="Lyrics3v2Field"/>.</param>
        /// <returns>
        /// true if equal; false otherwise.
        /// </returns>
        public virtual bool Equals(Lyrics3v2Field field)
        {
            if (ReferenceEquals(null, field))
                return false;

            if (ReferenceEquals(this, field))
                return true;

            return String.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase)
                   && ((field.Data != null) && (Data != null) ? StreamBuffer.SequenceEqual(field.Data, Data) : (field.Data == null) && (Data == null));
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return Identifier.GetHashCode() * 397;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the frame into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the frame.
        /// </returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer sb = new StreamBuffer())
            {
                sb.WriteString(Identifier, Encoding.ASCII, FieldIdentifierLength);
                byte[] data = Data;
                if (data != null)
                {
                    sb.WriteString(data.Length.ToString("D" + FieldSizeLength, CultureInfo.InvariantCulture));
                    sb.Write(data);
                }
                else
                {
                    sb.WriteString(0.ToString("D" + FieldSizeLength, CultureInfo.InvariantCulture));
                }
                return sb.ToByteArray();
            }
        }
    }
}
