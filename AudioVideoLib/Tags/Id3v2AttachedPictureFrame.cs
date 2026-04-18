namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

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
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
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
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(Description) && !IsValidTextString(Description, value, false))
            {
                throw new InvalidDataException("Description contains one or more invalid characters for the specified frame encoding type.");
            }

            field = value;
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
        get => field;

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                field = value;
                return;
            }

            if (!IsValidDefaultTextString(value, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters.");
            }

            field = ((Version < Id3v2Version.Id3v230) && (value.Length > 3)) ? value[..3] : value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the type of the picture.
    /// </summary>
    /// <value>
    /// The type of the picture.
    /// </value>
    public Id3v2AttachedPictureType PictureType
    {
        get => field;

        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
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
        get => field;

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                field = value;
                return;
            }

            if (!IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            field = ((Version < Id3v2Version.Id3v240) && (value.Length > 64)) ? value[..64] : value;
        }
    } = null!;

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
    public byte[] PictureData { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            var stream = new StreamBuffer();
            var preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

            // Text encoding
            stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

            // ImageFormat / MIME Type
            if (ImageFormat != null)
            {
                if (Version < Id3v2Version.Id3v230)
                {
                    stream.WriteString(ImageFormat, defaultEncoding, 3);
                }
                else
                {
                    stream.WriteString(ImageFormat, defaultEncoding);
                }
            }

            // 0x00
            if (Version >= Id3v2Version.Id3v230)
            {
                stream.Write(defaultEncoding.GetBytes("\0"));
            }

            // Picture type
            stream.WriteByte((byte)PictureType);

            // Preamble
            stream.Write(preamble);

            // Description
            if (Description != null)
            {
                stream.WriteString(Description, encoding);
            }

            // String terminator (0x00 in encoding)
            stream.Write(encoding.GetBytes("\0"));

            // Picture data
            if (PictureData != null)
            {
                stream.Write(PictureData);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var imageFormatEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var stream = new StreamBuffer(value);
            TextEncoding = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            ImageFormat = (Version < Id3v2Version.Id3v230) ? stream.ReadString(3, true) : stream.ReadString(imageFormatEncoding, true);
            PictureType = (Id3v2AttachedPictureType)stream.ReadByte();

            // Some badly written ID3v2 programs write ID3v2.2.0 PIC frames instead of APIC frames; find out here...
            if (ImageFormat.Length == 3)
            {
                int bytesRead;
                Description = stream.ReadString(encoding, out bytesRead);
                if (Description.Any(c => c == (char)0xFF))
                {
                    stream.Position -= bytesRead;
                    Description = string.Empty;
                }
            }
            else
            {
                // Don't bother here...
                Description = stream.ReadString(encoding);
            }
            PictureData = new byte[stream.Length - stream.Position];
            stream.Read(PictureData, PictureData.Length);
        }
    }

    /// <inheritdoc />
    public override string Identifier => (Version < Id3v2Version.Id3v230) ? "PIC" : "APIC";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame) => Equals(frame as Id3v2AttachedPictureFrame);

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
    public bool Equals(Id3v2AttachedPictureFrame? ap)
    {
        return ap is not null && (ReferenceEquals(this, ap) || ((ap.Version == Version)
               && (string.Equals(ap.Description, Description, StringComparison.OrdinalIgnoreCase)
                   || (ap.PictureType == Id3v2AttachedPictureType.FileIcon && PictureType == Id3v2AttachedPictureType.FileIcon)
                       || (ap.PictureType == Id3v2AttachedPictureType.OtherFileIcon && PictureType == Id3v2AttachedPictureType.OtherFileIcon))));
    }

    /// <summary>
    /// Determines whether the specified version is supported by the frame.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsVersionSupported(Id3v2Version version) => true;
}
