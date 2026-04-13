namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing an user defined URL link.
/// </summary>
/// <remarks>
/// This frame is intended for URL [URL] links concerning the audio file in a similar way to the an <see cref="Id3v2UrlLinkFrame"/> frame.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2UserDefinedUrlLinkFrame : Id3v2Frame
{
    private Id3v2FrameEncodingType _frameEncodingType;

    private string _description = null!, _url = null!;
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UserDefinedUrlLinkFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2UserDefinedUrlLinkFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UserDefinedUrlLinkFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2UserDefinedUrlLinkFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException(string.Format("Version {0} not supported by this frame.", version));
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
        get
        {
            return _frameEncodingType;
        }

        set
        {
            if (!string.IsNullOrEmpty(Description) && !IsValidTextString(Description, TextEncoding, false))
            {
                throw new InvalidDataException("Description contains one or more invalid characters for the specified frame encoding type.");
            }

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
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            _description = value;
        }
    }

    /// <summary>
    /// Gets or sets the actual URL.
    /// </summary>
    /// <value>
    /// The URL value.
    /// </value>
    /// <remarks>
    /// The URL must be a valid RFC 1738 URL, use <see cref="Id3v2Frame.IsValidUrl"/> to check if a value is a valid URL.
    /// </remarks>
    public string Url
    {
        get
        {
            return _url;
        }

        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!IsValidDefaultTextString(value, false))
                {
                    throw new InvalidDataException("value contains one or more invalid characters.");
                }

                if (!IsValidUrl(value))
                {
                    throw new InvalidDataException("value is not a valid RFC 1738 URL.");
                }
            }
            _url = value;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            var stream = new StreamBuffer();
            // Text Encoding
            stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

            // Preamble
            stream.Write(Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding));

            // Description
            if (Description != null)
            {
                stream.WriteString(Description, encoding);
            }

            // String terminator (0x00 in encoding)
            stream.Write(encoding.GetBytes("\0"));

            // URL (ISO-8859-1)
            if (Url != null)
            {
                stream.WriteString(Url, defaultEncoding);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var stream = new StreamBuffer(value);
            _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
            var encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
            _description = stream.ReadString(encoding);

            // Seems that some players like to add a BOM before the URL; even though the value is corrupt.
            _url = stream.ReadString(defaultEncoding, true);
        }
    }

    /// <inheritdoc />
    public override string Identifier
    {
        get { return (Version < Id3v2Version.Id3v230) ? "WXX" : "WXXX"; }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2UserDefinedUrlLinkFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2UserDefinedUrlLinkFrame"/>.
    /// </summary>
    /// <param name="udti">The <see cref="Id3v2UserDefinedUrlLinkFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> and <see cref="Description"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2UserDefinedUrlLinkFrame? udti)
    {
        return udti is not null && (ReferenceEquals(this, udti) || ((udti.Version == Version) && string.Equals(udti.Description, Description, StringComparison.OrdinalIgnoreCase)));
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
