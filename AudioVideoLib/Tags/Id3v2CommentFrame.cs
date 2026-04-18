namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing a comment.
/// </summary>
/// <remarks>
/// This frame replaces the old 30-character <see cref="Id3v1Tag.TrackComment"/> field in <see cref="Id3v1Tag"/>.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2CommentFrame : Id3v2Frame
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2CommentFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2CommentFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2CommentFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2CommentFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType" /> for possible values.
    /// </summary>
    /// <value>
    /// The text encoding.
    /// </value>
    /// <exception cref="System.IO.InvalidDataException">
    /// ShortContentDescription contains one or more invalid characters for the specified frame encoding type.
    /// or
    /// Text contains one or more invalid characters for the specified frame encoding type.
    /// </exception>
    /// <remarks>
    /// An <see cref="InvalidDataException" /> will be thrown when the <see cref="ShortContentDescription" /> or the <see cref="Text" />
    /// are not valid in the new <see cref="Id3v2FrameEncodingType" />.
    /// </remarks>
    public Id3v2FrameEncodingType TextEncoding
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(ShortContentDescription) && !IsValidTextString(ShortContentDescription, value, false))
            {
                throw new InvalidDataException("ShortContentDescription contains one or more invalid characters for the specified frame encoding type.");
            }

            if (!string.IsNullOrEmpty(Text) && !IsValidTextString(Text, value, true))
            {
                throw new InvalidDataException("Text contains one or more invalid characters for the specified frame encoding type.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the language of the comments.
    /// </summary>
    /// <value>
    /// The language.
    /// </value>
    /// <remarks>
    /// The language code should be a valid ISO-639-2 language code.
    /// <para />
    /// For version <see cref="Id3v2Version.Id3v240"/> and later, the string 'XXX' should be used if the language is not known.
    /// For version <see cref="Id3v2Version.Id3v240"/> and later, the language will be saved in lower case.
    /// </remarks>
    public string Language
    {
        get => field;

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                field = value;
                return;
            }

            // Id3v2.4.0 and later: If the language is not known the string "XXX" should be used.
            if (!IsValidLanguageCode(value) && ((Version != Id3v2Version.Id3v240) || !string.Equals(value, "XXX", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"Language code '{value}' is not a valid ISO-639-2 language code.");
            }

            // Id3v2.4.0 and later: The language should be represented in lower case.
            field = (Version >= Id3v2Version.Id3v240) ? value.ToLower() : value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the short content description.
    /// </summary>
    /// <value>
    /// The short content description.
    /// </value>
    /// <remarks>
    /// New lines are not allowed.
    /// </remarks>
    public string ShortContentDescription
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the actual text.
    /// </summary>
    /// <value>
    /// The actual text.
    /// </value>
    public string Text
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, true))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            field = value;
        }
    } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            var stream = new StreamBuffer();
            var preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

            // Text encoding
            stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

            // Language
            if (Language != null)
            {
                stream.WriteString(Language, languageEncoding);
            }

            // Preamble
            stream.Write(preamble);

            // Short content description
            if (ShortContentDescription != null)
            {
                stream.WriteString(ShortContentDescription, encoding);
            }

            // String terminator (0x00 in encoding)
            stream.Write(encoding.GetBytes("\0"));

            // Preamble
            stream.Write(preamble);

            // Text
            if (Text != null)
            {
                stream.WriteString(Text, encoding);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var stream = new StreamBuffer(value);
            TextEncoding = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            Language = stream.ReadString(3, languageEncoding);
            ShortContentDescription = stream.ReadString(encoding);
            Text = stream.ReadString(encoding);
        }
    }

    /// <inheritdoc />
    public override string Identifier => (Version < Id3v2Version.Id3v230) ? "COM" : "COMM";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2CommentFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2CommentFrame"/>.
    /// </summary>
    /// <param name="comment">The <see cref="Id3v2CommentFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/>, <see cref="Language"/> and <see cref="ShortContentDescription"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2CommentFrame? comment)
    {
        return comment is not null && (ReferenceEquals(this, comment) || ((comment.Version == Version)
            && string.Equals(comment.Language, Language, StringComparison.OrdinalIgnoreCase)
            && string.Equals(comment.ShortContentDescription, ShortContentDescription, StringComparison.OrdinalIgnoreCase)));
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
