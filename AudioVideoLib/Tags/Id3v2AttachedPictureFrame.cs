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
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing an attached picture frame.
    /// </summary>
    /// <remarks>
    /// This frame contains a picture directly related to the audio file.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2AttachedPictureFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _imageFormat, _description;

        private Id3v2AttachedPictureType _attachedPictureType;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2AttachedPictureFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2AttachedPictureFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2AttachedPictureFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2AttachedPictureFrame(Id3v2Version version) : base(version)
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
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="Description"/> is not valid in the new <see cref="Id3v2FrameEncodingType"/>.
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

                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the image format.
        /// </summary>
        /// <value>
        /// The image format.
        /// </value>
        /// <remarks>
        /// Image format is preferably "PNG" [PNG] or "JPG" [JFIF].
        /// <para />
        /// In versions before <see cref="Id3v2Version.Id3v230"/>, the image format has a maximum length of 3 characters. 
        /// If the value is longer than 3 characters, the value will be trimmed.
        /// </remarks>
        public string ImageFormat
        {
            get
            {
                return _imageFormat;
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _imageFormat = value;
                    return;
                }

                if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                _imageFormat = ((Version < Id3v2Version.Id3v230) && (value.Length > 3)) ? value.Substring(0, 3) : value;
            }
        }
        
        /// <summary>
        /// Gets or sets the type of the picture.
        /// </summary>
        /// <value>
        /// The type of the picture.
        /// </value>
        public Id3v2AttachedPictureType PictureType
        {
            get
            {
                return _attachedPictureType;
            }

            set
            {
                if (!IsValidAttachedPictureType(value))
                    throw new ArgumentOutOfRangeException("value");

                _attachedPictureType = value;
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        /// <remarks>
        /// Description is a short description of the picture, represented as a terminated text string.
        /// <para />
        /// In versions before <see cref="Id3v2Version.Id3v240"/>, the description has a maximum length of 64 characters, but may be empty.
        /// If the value is longer than 64 character the value will be trimmed.
        /// <para />
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
                if (String.IsNullOrEmpty(value))
                {
                    _description = value;
                    return;
                }

                if (!IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _description = ((Version < Id3v2Version.Id3v240) && (value.Length > 64)) ? value.Substring(0, 64) : value;
            }
        }

        /// <summary>
        /// Gets or sets the picture data.
        /// </summary>
        /// <value>
        /// The picture data.
        /// </value>
        /// <remarks>
        /// There is a possibility to put only a link to the image file by using the 'image format' "-->" 
        /// and having a complete URL [URL] instead of picture data.
        /// The use of linked files should however be used restrictively since there is the risk of separation of files.
        /// </remarks>
        public byte[] PictureData { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // ImageFormat / MIME Type
                    if (ImageFormat != null)
                    {
                        if (Version < Id3v2Version.Id3v230)
                            stream.WriteString(ImageFormat, defaultEncoding, 3);
                        else
                            stream.WriteString(ImageFormat, defaultEncoding);
                    }

                    // 0x00
                    if (Version >= Id3v2Version.Id3v230)
                        stream.Write(defaultEncoding.GetBytes("\0"));

                    // Picture type
                    stream.WriteByte((byte)PictureType);

                    // Preamble
                    stream.Write(preamble);

                    // Description
                    if (Description != null)
                        stream.WriteString(Description, encoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(encoding.GetBytes("\0"));

                    // Picture data
                    if (PictureData != null)
                        stream.Write(PictureData);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding imageFormatEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _imageFormat = (Version < Id3v2Version.Id3v230) ? stream.ReadString(3, true) : stream.ReadString(imageFormatEncoding, true);
                    _attachedPictureType = (Id3v2AttachedPictureType)stream.ReadByte();

                    // Some badly written ID3v2 programs write ID3v2.2.0 PIC frames instead of APIC frames; find out here...
                    if (_imageFormat.Length == 3)
                    {
                        int bytesRead;
                        _description = stream.ReadString(encoding, out bytesRead);
                        if (_description.Any(c => c == (char)0xFF))
                        {
                            stream.Position -= bytesRead;
                            _description = String.Empty;
                        }
                    }
                    else
                    {
                        // Don't bother here...
                        _description = stream.ReadString(encoding);
                    }
                    PictureData = new byte[stream.Length - stream.Position];
                    stream.Read(PictureData, PictureData.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "PIC" : "APIC"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2AttachedPictureFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2AttachedPictureFrame"/>.
        /// </summary>
        /// <param name="ap">The <see cref="Id3v2AttachedPictureFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="Description"/> properties are equal (case-insensitive), or,
        /// when their <see cref="Version"/> property is equal and both use <see cref="Id3v2AttachedPictureType.FileIcon"/> 
        /// or <see cref="Id3v2AttachedPictureType.OtherFileIcon"/> as <see cref="PictureType"/>.
        /// </remarks>
        public bool Equals(Id3v2AttachedPictureFrame ap)
        {
            if (ReferenceEquals(null, ap))
                return false;

            if (ReferenceEquals(this, ap))
                return true;

            return (ap.Version == Version)
                   && (String.Equals(ap.Description, Description, StringComparison.OrdinalIgnoreCase)
                       || ((ap.PictureType == Id3v2AttachedPictureType.FileIcon && PictureType == Id3v2AttachedPictureType.FileIcon)
                           || (ap.PictureType == Id3v2AttachedPictureType.OtherFileIcon && PictureType == Id3v2AttachedPictureType.OtherFileIcon)));
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

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidAttachedPictureType(Id3v2AttachedPictureType attachedPictureType)
        {
            return Enum.TryParse(attachedPictureType.ToString(), true, out attachedPictureType);
        }
    }
}
