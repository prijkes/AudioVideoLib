namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing unsynchronized lyrics.
/// </summary>
/// <remarks>
/// This frame contains the lyrics of the song or a text transcription of other vocal activities.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2UnsynchronizedLyricsFrame : Id3v2Frame
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UnsynchronizedLyricsFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2UnsynchronizedLyricsFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UnsynchronizedLyricsFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2UnsynchronizedLyricsFrame(Id3v2Version version) : base(version)
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
    /// </summary>
    /// <value>
    /// The text encoding.
    /// </value>
    /// <remarks>
    /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="ContentDescriptor"/> or the <see cref="Lyrics"/>
    /// are not valid in the new <see cref="Id3v2FrameEncodingType"/>.
    /// </remarks>
    public Id3v2FrameEncodingType TextEncoding
    {
        get => field;

        set
        {
            ValidateEncodedText(ContentDescriptor, value, nameof(ContentDescriptor), false);
            ValidateEncodedText(Lyrics, value, nameof(Lyrics), true);
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the language of the lyrics.
    /// </summary>
    /// <value>
    /// The language.
    /// </value>
    /// <remarks>
    /// The language code should be a valid ISO-639-2 language code;
    /// see <see cref="Id3v2Frame.IsValidLanguageCode"/> to check if a string is a valid ISO-639-2 language code.
    /// </remarks>
    public string Language
    {
        get => field;

        set => field = NormalizeLanguageCode(Version, value);
    } = null!;

    /// <summary>
    /// Gets or sets the content descriptor of the lyrics.
    /// </summary>
    /// <value>
    /// The content descriptor.
    /// </value>
    /// <remarks>
    /// For <see cref="Id3v2Version.Id3v220"/> and <see cref="Id3v2Version.Id3v221"/>, the maximum length of the <see cref="ContentDescriptor"/> is 64 bytes.
    /// <para />
    /// New lines are not allowed.
    /// </remarks>
    public string ContentDescriptor
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

            field = (Version < Id3v2Version.Id3v230) ? GetTruncatedEncodedString(value, 64) : value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the lyrics.
    /// </summary>
    /// <value>
    /// The lyrics.
    /// </value>
    public string Lyrics
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
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            var stream = new StreamBuffer();
            // Text encoding
            Id3v2FrameEncoding.WriteHeader(stream, TextEncoding);

            // Language
            if (Language != null)
            {
                stream.WriteString(Language, Id3v2FrameEncoding.DefaultEncoding);
            }

            // Preamble
            Id3v2FrameEncoding.WritePreamble(stream, TextEncoding);

            // Content descriptor
            if (!string.IsNullOrEmpty(ContentDescriptor))
            {
                if (Version < Id3v2Version.Id3v230)
                {
                    stream.WriteString(ContentDescriptor, encoding, 64);
                }
                else
                {
                    stream.WriteString(ContentDescriptor, encoding);
                }
            }

            // String terminator (0x00 in encoding)
            Id3v2FrameEncoding.WriteNullTerminator(stream, TextEncoding);

            // Preamble
            Id3v2FrameEncoding.WritePreamble(stream, TextEncoding);

            // Lyrics/text
            if (Lyrics != null)
            {
                stream.WriteString(Lyrics, encoding);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            TextEncoding = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            Language = stream.ReadString(LanguageCodeLength, Id3v2FrameEncoding.DefaultEncoding);
            ContentDescriptor = stream.ReadString(encoding);
            Lyrics = stream.ReadString(encoding);
        }
    }

    /// <inheritdoc />
    public override string Identifier => GetIdentifierFromFactory();

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame) => Equals(frame as Id3v2UnsynchronizedLyricsFrame);

    /// <summary>
    /// Equals the specified <see cref="Id3v2UnsynchronizedLyricsFrame"/>.
    /// </summary>
    /// <param name="ul">The <see cref="Id3v2UnsynchronizedLyricsFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/>, <see cref="Language"/> and <see cref="ContentDescriptor"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2UnsynchronizedLyricsFrame? ul)
    {
        return ul is not null && (ReferenceEquals(this, ul) || ((ul.Version == Version) && string.Equals(ul.Language, Language, StringComparison.OrdinalIgnoreCase)
               && string.Equals(ul.ContentDescriptor, ContentDescriptor, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Determines whether the specified version is supported by the frame.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsVersionSupported(Id3v2Version version) => true;

    ////------------------------------------------------------------------------------------------------------------------------------

    private string GetTruncatedEncodedString(string value, int maxBytesAllowed)
    {
        var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
        return StreamBuffer.GetTruncatedEncodedString(value, encoding, maxBytesAllowed);
    }
}
