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
    /// Class for storing a general encapsulated object.
    /// </summary>
    /// <remarks>
    /// In this frame any type of file can be encapsulated.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2GeneralEncapsulatedObjectFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _mimeType, _filename, _contentDescription;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2GeneralEncapsulatedObjectFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2GeneralEncapsulatedObjectFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2GeneralEncapsulatedObjectFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2GeneralEncapsulatedObjectFrame(Id3v2Version version) : base(version)
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
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="Filename"/> or the <see cref="ContentDescription"/> 
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
                if (!String.IsNullOrEmpty(Filename) && !IsValidTextString(Filename, value, false))
                    throw new InvalidDataException("Filename contains one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(ContentDescription) && !IsValidTextString(ContentDescription, value, false))
                {
                    throw new InvalidDataException(
                        "ContentDescription contains one or more invalid characters for the specified frame encoding type.");
                }

                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the MIME.
        /// </summary>
        /// <value>
        /// The type of the MIME.
        /// </value>
        /// <remarks>
        /// MIME type is always an ISO-8859-1 encoded terminated text string.
        /// <para />
        /// New lines are not allowed.
        /// </remarks>
        public string MimeType
        {
            get
            {
                return _mimeType;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                _mimeType = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        /// <remarks>
        /// The filename is case sensitive and represented as a terminated string.
        /// <para />
        /// New lines are not allowed.
        /// </remarks>
        public string Filename
        {
            get
            {
                return _filename;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _filename = value;
            }
        }

        /// <summary>
        /// Gets or sets the content description.
        /// </summary>
        /// <value>
        /// The content description.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public string ContentDescription
        {
            get
            {
                return _contentDescription;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _contentDescription = value;
            }
        }

        /// <summary>
        /// Gets or sets the encapsulated object.
        /// </summary>
        /// <value>
        /// The encapsulated object.
        /// </value>
        public byte[] EncapsulatedObject { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding mimeTypeEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                byte[] imageFormatTerminator = mimeTypeEncoding.GetBytes("\0");
                Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                byte[] stringTerminator = encoding.GetBytes("\0");
                using (StreamBuffer stream = new StreamBuffer())
                {
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // ImageFormat / MIME Type
                    if (MimeType != null)
                        stream.WriteString(MimeType, mimeTypeEncoding);

                    // 0x00
                    stream.Write(imageFormatTerminator);

                    // Preamble
                    stream.Write(preamble);

                    // Filename
                    if (Filename != null)
                        stream.WriteString(Filename, encoding);

                    // String terminator 0x00 (in encoding)
                    stream.Write(stringTerminator);

                    // Preamble
                    stream.Write(preamble);

                    // Content description
                    if (ContentDescription != null)
                        stream.WriteString(ContentDescription, encoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(stringTerminator);

                    // Encapsulated object
                    if (EncapsulatedObject != null)
                        stream.Write(EncapsulatedObject);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding mimeTypeEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _mimeType = stream.ReadString(mimeTypeEncoding, true);
                    _filename = stream.ReadString(encoding);
                    _contentDescription = stream.ReadString(encoding);
                    EncapsulatedObject = new byte[stream.Length - stream.Position];
                    stream.Read(EncapsulatedObject, EncapsulatedObject.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "GEO" : "GEOB"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2GeneralEncapsulatedObjectFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2GeneralEncapsulatedObjectFrame"/>.
        /// </summary>
        /// <param name="geo">The <see cref="Id3v2GeneralEncapsulatedObjectFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="ContentDescription"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2GeneralEncapsulatedObjectFrame geo)
        {
            if (ReferenceEquals(null, geo))
                return false;

            if (ReferenceEquals(this, geo))
                return true;

            return (geo.Version == Version) && String.Equals(geo.ContentDescription, ContentDescription, StringComparison.OrdinalIgnoreCase);
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
