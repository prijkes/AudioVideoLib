/*
 * Date: 2011-08-12
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing the terms of use.
/// </summary>
/// <remarks>
/// This frame contains a brief description of the terms of use and ownership of the file.
/// More detailed information concerning the legal terms might be available through <see cref="Id3v2Tag.CopyrightInformation"/>.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
/// </remarks>
public sealed class Id3v2TermsOfUseFrame : Id3v2Frame
{
    private Id3v2FrameEncodingType _frameEncodingType;

    private string _language = null!, _text = null!;
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2TermsOfUseFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2TermsOfUseFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2TermsOfUseFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2TermsOfUseFrame(Id3v2Version version) : base(version)
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
    public Id3v2FrameEncodingType TextEncoding
    {
        get
        {
            return _frameEncodingType;
        }

        set
        {
            if (!string.IsNullOrEmpty(Text) && !IsValidTextString(Text, value, false))
            {
                throw new InvalidDataException("Text contains one or more invalid characters for the specified frame encoding type.");
            }

            _frameEncodingType = value;
        }
    }

    /// <summary>
    /// Gets or sets the language of the terms of use.
    /// </summary>
    /// <value>
    /// The language.
    /// </value>
    /// <remarks>
    /// The language code should be a valid ISO-639-2 language code.
    /// </remarks>
    public string Language
    {
        get
        {
            return _language;
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _language = value;
                return;
            }

            // Id3v2.4.0 and later: If the language is not known the string "XXX" should be used.
            if (!IsValidLanguageCode(value) && ((Version != Id3v2Version.Id3v240) || !string.Equals(value, "XXX", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException(string.Format("Language code '{0}' is not a valid ISO-639-2 language code.", value));
            }

            // Id3v2.4.0 and later: The language should be represented in lower case.
            _language = (Version >= Id3v2Version.Id3v240) && !string.IsNullOrEmpty(value) ? value.ToLower() : value;
        }
    }

    /// <summary>
    /// Gets or sets the actual text.
    /// </summary>
    /// <value>
    /// The actual text.
    /// </value>
    public string Text
    {
        get
        {
            return _text;
        }

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            _text = value;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            // Text encoding
            stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

            // Language (3 bytes, ISO-639-2 in ISO-8859-1)
            if (Language != null)
            {
                stream.WriteString(Language, Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default));
            }

            // Text (according to text encoding — no BOM preamble per ID3v2 USER spec)
            if (Text != null)
            {
                stream.WriteString(Text, Id3v2FrameEncoding.GetEncoding(TextEncoding));
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
            _language = stream.ReadString(3, defaultEncoding);
            _text = stream.ReadString(encoding);
        }
    }

    /// <inheritdoc />
    public override string Identifier
    {
        get { return "USER"; }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2TermsOfUseFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2TermsOfUseFrame"/>.
    /// </summary>
    /// <param name="tos">The <see cref="Id3v2TermsOfUseFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> property is equal.
    /// </remarks>
    public bool Equals(Id3v2TermsOfUseFrame? tos)
    {
        return tos is not null && (ReferenceEquals(this, tos) || tos.Version == Version);
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
        return version >= Id3v2Version.Id3v230;
    }
}
