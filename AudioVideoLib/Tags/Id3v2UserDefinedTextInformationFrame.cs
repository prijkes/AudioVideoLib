/*
 * Date: 2011-05-28
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing user defined text information.
    /// </summary>
    /// <remarks>
    /// This frame is intended for one-string text information concerning the audio file in a similar way to an <see cref="Id3v2TextFrame"/>.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2UserDefinedTextInformationFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _description, _value;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2UserDefinedTextInformationFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2UserDefinedTextInformationFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2UserDefinedTextInformationFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2UserDefinedTextInformationFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="Description"/> or the <see cref="Value"/> 
        /// is not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }

            set
            {
                if (!String.IsNullOrEmpty(Description) && !IsValidTextString(Description, value, false))
                    throw new InvalidDataException("Description contains one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(Value) && !IsValidTextString(Value, value, false))
                    throw new InvalidDataException("Value contains one or more invalid characters for the specified frame encoding type.");

                _frameEncodingType = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the description of the string.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _description = value;
            }
        }

        /// <summary>
        /// Gets or sets the actual value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public string Value
        {
            get
            {
                return _value;
            }
            
            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _value = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Preamble
                    stream.Write(preamble);

                    // Description
                    if (Description != null)
                        stream.WriteString(Description, encoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(encoding.GetBytes("\0"));

                    // Preamble
                    stream.Write(preamble);

                    // Value
                    if (Value != null)
                        stream.WriteString(Value, encoding);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _description = stream.ReadString(encoding);
                    _value = stream.ReadString(encoding);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "TXX" : "TXXX"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2UserDefinedTextInformationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2UserDefinedTextInformationFrame"/>.
        /// </summary>
        /// <param name="udti">The <see cref="Id3v2UserDefinedTextInformationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="Description"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2UserDefinedTextInformationFrame udti)
        {
            if (ReferenceEquals(null, udti))
                return false;

            if (ReferenceEquals(this, udti))
                return true;

            return (udti.Version == Version) && String.Equals(udti.Description, Description, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified version is supported by the frame.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsVersionSupported(Id3v2Version version)
        {
            return true;
        }
    }
}
