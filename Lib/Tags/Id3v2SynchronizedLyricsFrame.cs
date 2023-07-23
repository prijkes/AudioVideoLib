/*
 * Date: 2011-06-26
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing synchronized lyrics.
    /// </summary>
    /// <remarks>
    /// This frame is another way of incorporating the words, said or sung lyrics, in the audio file as text, this time, however, in sync with the audio.
    /// It might also be used to describing events e.g. occurring on a stage or on the screen in sync with the audio.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2SynchronizedLyricsFrame : Id3v2Frame
    {
        private readonly EventList<Id3v2LyricSync> _lyricSyncs = new EventList<Id3v2LyricSync>();

        private Id3v2FrameEncodingType _frameEncodingType;

        private string _language, _contentDescriptor;

        private Id3v2TimeStampFormat _timeStampFormat;

        private Id3v2ContentType _contentType;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2SynchronizedLyricsFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2SynchronizedLyricsFrame() : base(Id3v2Version.Id3v230)
        {
            BindLyricSyncListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2SynchronizedLyricsFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2SynchronizedLyricsFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindLyricSyncListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when <see cref="LyricSyncs"/> contains any <see cref="Id3v2LyricSync.Syllable"/> 
        /// entry not valid in the new <see cref="Id3v2FrameEncodingType"/>, or when the <see cref="ContentDescriptor"/> 
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
                if (_lyricSyncs.Any(t => !String.IsNullOrEmpty(t.Syllable) && !IsValidTextString(t.Syllable, value, true)))
                    throw new InvalidDataException("LyricSyncs contains one or more Syllable entries with one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(ContentDescriptor) && !IsValidTextString(ContentDescriptor, value, false))
                    throw new InvalidDataException("ContentDescriptor contains one or more invalid characters for the specified frame encoding type.");

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
        /// The language code should be a valid ISO-639-2 language code.
        /// <para />
        /// In <see cref="Id3v2Version.Id3v240"/> and later, if the language is not known the string "XXX" should be used.
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
                _language = (Version >= Id3v2Version.Id3v240) ? value.ToLower() : value;
            }
        }

        /// <summary>
        /// Gets or sets the time stamp format.
        /// </summary>
        /// <value>
        /// The time stamp.
        /// </value>
        public Id3v2TimeStampFormat TimeStampFormat
        {
            get
            {
                return _timeStampFormat;
            }

            set
            {
                if (!IsValidTimeStampFormat(value))
                    throw new ArgumentOutOfRangeException("value");

                _timeStampFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        /// <value>
        /// The content type.
        /// </value>
        public Id3v2ContentType ContentType
        {
            get
            {
                return _contentType;
            }

            set
            {
                if (!IsValidContentType(value))
                    throw new ArgumentOutOfRangeException("value", String.Format("value '{0}' is not supported in version {1}.", value, Version));

                _contentType = value;
            }
        }

        /// <summary>
        /// Gets or sets the content descriptor of the lyrics.
        /// </summary>
        /// <value>
        /// The content descriptor.
        /// </value>
        /// <remarks>
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
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _contentDescriptor = value;
            }
        }

        /// <summary>
        /// Gets or sets a list of lyric syncs.
        /// </summary>
        /// <value>
        /// A list of lyric syncs.
        /// </value>
        /// <remarks>
        /// The lyric syncs will be sorted and saved in chronological order.
        /// </remarks>
        public ICollection<Id3v2LyricSync> LyricSyncs
        {
            get
            {
                return _lyricSyncs;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Language
                    if (Language != null)
                        stream.WriteString(Language, languageEncoding);

                    // Timestamp format
                    stream.WriteByte((byte)TimeStampFormat);

                    // Content type
                    stream.WriteByte((byte)ContentType);

                    // Preamble
                    stream.Write(Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding));

                    // Content descriptor
                    if (ContentDescriptor != null)
                        stream.WriteString(ContentDescriptor, encoding);

                    // Syncs
                    byte[] syncIdentifier = encoding.GetBytes("\0");

                    // Write content descriptor NULL-terminator
                    stream.Write(syncIdentifier);

                    bool requiresNewLine = (Version >= Id3v2Version.Id3v240) && ((ContentType == Id3v2ContentType.MovementName) || (ContentType == Id3v2ContentType.Events));
                    foreach (Id3v2LyricSync ls in LyricSyncs)
                    {
                        // Syllable
                        string syllable = (requiresNewLine && !ls.Syllable.EndsWith("\n")) ? ls.Syllable + "\n" : ls.Syllable;
                        stream.WriteString(syllable, encoding);

                        // Write sync identifier ('\0' in encoding)
                        stream.Write(syncIdentifier);

                        // Timestamp
                        stream.WriteBigEndianInt32(ls.TimeStamp);
                    }
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _language = stream.ReadString(3, languageEncoding);
                    _timeStampFormat = (Id3v2TimeStampFormat)stream.ReadByte();
                    _contentType = (Id3v2ContentType)stream.ReadByte();
                    _contentDescriptor = stream.ReadString(encoding);

                    // Read the syncs.
                    UnbindLyricSyncListEvents();
                    _lyricSyncs.Clear();
                    while (stream.Position < stream.Length)
                    {
                        string syllable = stream.ReadString(encoding);

                        // The 'time stamp' is set to zero or the whole sync is omitted if located directly at the beginning of the sound.
                        // All time stamps should be sorted in chronological order. The sync can be considered as a validator of the subsequent string.
                        int timeStamp = (stream.Position < stream.Length) ? stream.ReadBigEndianInt32() : 0;
                        _lyricSyncs.Add(new Id3v2LyricSync(syllable, timeStamp));
                    }
                    BindLyricSyncListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "SLT" : "SYLT"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2SynchronizedLyricsFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2SynchronizedLyricsFrame"/>.
        /// </summary>
        /// <param name="sl">The <see cref="Id3v2SynchronizedLyricsFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/>, <see cref="Language"/> and <see cref="ContentDescriptor"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2SynchronizedLyricsFrame sl)
        {
            if (ReferenceEquals(null, sl))
                return false;

            if (ReferenceEquals(this, sl))
                return true;

            return (sl.Version == Version) && String.Equals(sl.Language, Language, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(sl.ContentDescriptor, ContentDescriptor, StringComparison.OrdinalIgnoreCase);
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

        private bool IsValidContentType(Id3v2ContentType contentType)
        {
            Id3v2ContentType maxAllowedOrderedContentType, type;
            if (Version < Id3v2Version.Id3v230)
                maxAllowedOrderedContentType = Id3v2ContentType.Chord;
            else if (Version < Id3v2Version.Id3v240)
                maxAllowedOrderedContentType = Id3v2ContentType.Trivia;
            else
                maxAllowedOrderedContentType = Id3v2ContentType.ImagesUrls;

            return Enum.TryParse(contentType.ToString(), true, out type) && (contentType <= maxAllowedOrderedContentType);
        }

        private void BindLyricSyncListEvents()
        {
            _lyricSyncs.ItemAdd += LyricSyncAdd;

            _lyricSyncs.ItemReplace += LyricSyncReplace;
        }

        private void UnbindLyricSyncListEvents()
        {
            _lyricSyncs.ItemAdd -= LyricSyncAdd;

            _lyricSyncs.ItemReplace -= LyricSyncReplace;
        }

        private void LyricSyncAdd(object sender, ListItemAddEventArgs<Id3v2LyricSync> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (e.Item.Syllable == null)
                throw new NullReferenceException("e.Syllable may not be null");

            if (!IsValidTextString(e.Item.Syllable, TextEncoding, false))
            {
                throw new InvalidDataException(
                    "value contains one or more Syllable entries with one or more invalid characters for the current frame encoding type.");
            }

            for (int i = 0; i < _lyricSyncs.Count; i++)
            {
                Id3v2LyricSync lyricSync = _lyricSyncs[i];
                if (lyricSync.TimeStamp >= e.Item.TimeStamp)
                {
                    e.Index = i;
                    break;
                }
            }
        }

        private void LyricSyncReplace(object sender, ListItemReplaceEventArgs<Id3v2LyricSync> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _lyricSyncs.RemoveAt(e.Index);
            e.Cancel = true;
            _lyricSyncs.Add(e.NewItem);
        }
    }
}
