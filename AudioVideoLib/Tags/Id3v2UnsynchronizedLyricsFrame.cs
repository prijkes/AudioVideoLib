/*
 * Date: 2011-06-25
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
    /// Class for storing unsynchronized lyrics.
    /// </summary>
    /// <remarks>
    /// This frame contains the lyrics of the song or a text transcription of other vocal activities.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2UnsynchronizedLyricsFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _language, _contentDescriptor, _lyrics;

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
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="ContentDescriptor"/> or the <see cref="Lyrics"/> 
        /// are not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }

            set
            {
                if (!String.IsNullOrEmpty(ContentDescriptor) && !IsValidTextString(ContentDescriptor, value, false))
                    throw new InvalidDataException("ContentDescriptor contains one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(Lyrics) && !IsValidTextString(Lyrics, value, true))
                    throw new InvalidDataException("Lyrics contains one or more invalid characters for the specified frame encoding type.");

                _frameEncodingType = value;
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
            get
            {
                return _language;
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _language = value;
                    return;
                }

                // Id3v2.4.0 and later: If the language is not known the string "XXX" should be used.
                if (!IsValidLanguageCode(value) && ((Version != Id3v2Version.Id3v240) || !String.Equals(value, "XXX", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException(String.Format("Language code '{0}' is not a valid ISO-639-2 language code.", value));

                // Id3v2.4.0 and later: The language should be represented in lower case.
                _language = (Version >= Id3v2Version.Id3v240) && !String.IsNullOrEmpty(value) ? value.ToLower() : value;
            }
        }

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
            get
            {
                return _contentDescriptor;
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _contentDescriptor = value;
                    return;
                }

                if (!IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _contentDescriptor = (Version < Id3v2Version.Id3v230) ? GetTruncatedEncodedString(value, 64) : value;
            }
        }
        
        /// <summary>
        /// Gets or sets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        public string Lyrics
        {
            get
            {
                return _lyrics;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, true))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _lyrics = value;
            }
        }

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
                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Language
                    if (Language != null)
                        stream.WriteString(Language, defaultEncoding);

                    // Preamble
                    stream.Write(Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding));

                    // Content descriptor
                    if (!String.IsNullOrEmpty(ContentDescriptor))
                    {
                        if (Version < Id3v2Version.Id3v230)
                            stream.WriteString(ContentDescriptor, encoding, 64);
                        else
                            stream.WriteString(ContentDescriptor, encoding);
                    }

                    // String terminator (0x00 in encoding)
                    stream.Write(encoding.GetBytes("\0"));

                    // Preamble
                    stream.Write(Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding));

                    // Lyrics/text
                    if (Lyrics != null)
                        stream.WriteString(Lyrics, encoding);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _language = stream.ReadString(3, defaultEncoding);
                    _contentDescriptor = stream.ReadString(encoding);
                    _lyrics = stream.ReadString(encoding);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "ULT" : "USLT"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2UnsynchronizedLyricsFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2UnsynchronizedLyricsFrame"/>.
        /// </summary>
        /// <param name="ul">The <see cref="Id3v2UnsynchronizedLyricsFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/>, <see cref="Language"/> and <see cref="ContentDescriptor"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2UnsynchronizedLyricsFrame ul)
        {
            if (ReferenceEquals(null, ul))
                return false;

            if (ReferenceEquals(this, ul))
                return true;

            return (ul.Version == Version) && String.Equals(ul.Language, Language, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(ul.ContentDescriptor, ContentDescriptor, StringComparison.OrdinalIgnoreCase);
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

        private string GetTruncatedEncodedString(string value, int maxBytesAllowed)
        {
            Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
            return StreamBuffer.GetTruncatedEncodedString(value, encoding, maxBytesAllowed);
        }
    }
}
