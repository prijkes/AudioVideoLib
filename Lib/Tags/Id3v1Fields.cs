/*
 * Date: 2012-11-10
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.id3.org/Id3v1
 *  http://en.wikipedia.org/wiki/ID3#Extended_tag
 *  http://lib313.sourceforge.net/id3v13.html
 */
using System;
using System.Text;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v1 tag.
    /// </summary>
    public partial class Id3v1Tag
    {
        private string _trackTitle, _artist, _albumTitle, _albumYear, _trackComment;

        private Encoding _encoding = Encoding.Default;

        private Id3v1Genre _genre;

        private string _extendedTrackGenre;

        private Id3v1TrackSpeed _trackSpeed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> used to read and write text to a byte array.
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _encoding = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>
        /// The artist.
        /// </value>
        /// <remarks>
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true), 
        /// the value will be cut to the max character count which fits within 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true).
        /// <para />
        /// The full string is stored internally and only cut to the max character count on retrieval.
        /// This is done to preserve the value when changing the <see cref="Encoding"/>, so characters aren't lost when changing encoding.
        /// </remarks>
        public string Artist
        {
            get
            {
                return (_artist == null) ? GetExtendedString(_artist, 30, 60) : null;
            }

            set
            {
                _artist = value;
            }
        }

        /// <summary>
        /// Gets or sets the album title.
        /// </summary>
        /// <value>
        /// The album title.
        /// </value>
        /// <remarks>
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true), 
        /// the value will be cut to the max character count which fits within 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true).
        /// <para />
        /// The full string is stored internally and only cut to the max character count on retrieval.
        /// This is done to preserve the value when changing the <see cref="Encoding"/>, so characters aren't lost when changing encoding.
        /// </remarks>
        /// Encoding issues could cause the first part to contain more than 30 bytes if we encode at max 90 bytes.
        /// This will cause issues when writing the string as 2 separate byte arrays; characters can get lost.
        /// This is why we have to encode the string in 2 parts: one for 30 bytes max and one fore 60 bytes max.
        public string AlbumTitle
        {
            get
            {
                return (_albumTitle == null) ? GetExtendedString(_albumTitle, 30, 60) : null;
            }

            set
            {
                _albumTitle = value;
            }
        }

        /// <summary>
        /// Gets or sets the album year.
        /// </summary>
        /// <value>
        /// The album year.
        /// </value>
        /// <remarks>
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds 4 bytes, the value will be cut to the max character count which fits within 4 bytes.
        /// <para />
        /// The full string is stored internally and only cut to the max character count on retrieval.
        /// This is done to preserve the value when changing the <see cref="Encoding"/>, so characters aren't lost when changing encoding.
        /// </remarks>
        public string AlbumYear
        {
            get
            {
                return (_albumYear != null) ? GetTruncatedEncodedString(_albumYear, 4) : null;
            }

            set
            {
                _albumYear = value;
            }
        }

        /// <summary>
        /// Gets or sets the track comment.
        /// </summary>
        /// <value>
        /// The track comment.
        /// </value>
        /// <remarks>
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds <see cref="TrackCommentLength"/> bytes, 
        /// the value will be cut to the max character count which fits within <see cref="TrackCommentLength"/> bytes.
        /// <para />
        /// The full string is stored internally and only cut to the max character count on retrieval.
        /// This is done to preserve the value when changing the <see cref="Encoding"/>, so characters aren't lost when changing encoding.
        /// </remarks>
        public string TrackComment
        {
            get
            {
                return (_trackComment != null) ? GetTruncatedEncodedString(_trackComment, TrackCommentLength) : null;
            }

            set
            {
                _trackComment = value;
            }
        }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        /// <value>
        /// The genre.
        /// </value>
        public Id3v1Genre Genre
        {
            get
            {
                return _genre;
            }

            set
            {
                if (!IsValidGenre(value))
                    throw new ArgumentOutOfRangeException("value");

                _genre = value;
            }
        }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        /// <value>
        /// The track number.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v1Version.Id3v11"/>.
        /// </remarks>
        public byte TrackNumber { get; set; }

        /// <summary>
        /// Gets or sets the track title.
        /// </summary>
        /// <value>
        /// The track title.
        /// </value>
        /// <remarks>
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true), 
        /// the value will be cut to the max character count which fits within 30 bytes (or 90 bytes when <see cref="UseExtendedTag"/> is set to true).
        /// <para />
        /// The full string is stored internally and only cut to the max character count on retrieval.
        /// This is done to preserve the value when changing the <see cref="Encoding"/>, so characters aren't lost when changing encoding.
        /// </remarks>
        public string TrackTitle
        {
            get
            {
                return (_trackTitle != null) ? GetExtendedString(_trackTitle, 30, 60) : null;
            }

            set
            {
                _trackTitle = value;
            }
        }

        /// <summary>
        /// Gets the length of the track comment.
        /// </summary>
        /// <value>
        /// The length of the track comment.
        /// </value>
        /// <remarks>
        /// This is 30 bytes for version <see cref="Id3v1Version.Id3v11"/> and 28 bytes for version <see cref="Id3v1Version.Id3v10"/> and later.
        /// </remarks>
        public int TrackCommentLength
        {
            get
            {
                return (Version >= Id3v1Version.Id3v11) ? 28 : 30;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether to use the extended tag.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the extended tag should be used; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The extended tag is an extra data block before an ID3v1 tag, which extends the title, artist and album fields by 60 bytes each, 
        /// offers a free text genre, a one-byte (values 0–5) speed and the start and stop time of the music in the MP3 file, e.g., for fading in. 
        /// If none of the fields are used, it will be automatically omitted.
        /// <para />
        /// Some programs supporting ID3v1 tags can read the extended tag, but writing may leave stale values in the extended block.
        /// The extended block is not an official standard, and is only supported by few programs, not including XMMS or Winamp.
        /// The extended tag is sometimes referred to as the "enhanced" tag.
        /// </remarks>
        public bool UseExtendedTag { get; set; }

        /// <summary>
        /// Gets or sets the track speed.
        /// </summary>
        /// <value>
        /// The track speed.
        /// </value>
        /// <remarks>
        /// This field is part of the extended tag and is only used when <see cref="UseExtendedTag"/> is set to true.
        /// </remarks>
        public Id3v1TrackSpeed TrackSpeed
        {
            get
            {
                return _trackSpeed;
            }

            set
            {
                if (!IsValidTrackSpeed(value))
                    throw new ArgumentOutOfRangeException("value");

                _trackSpeed = value;
            }
        }

        /// <summary>
        /// Gets or sets the extended track genre.
        /// </summary>
        /// <value>
        /// The extended track genre.
        /// </value>
        /// <remarks>
        /// This field is part of the extended tag and is only used when <see cref="UseExtendedTag"/> is set to true.
        /// <para />
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds 30 bytes, the value will be cut to the max character count which fits within 30 bytes.
        /// </remarks>
        public string ExtendedTrackGenre
        {
            get
            {
                return (_extendedTrackGenre != null) ? GetTruncatedEncodedString(_extendedTrackGenre, 30) : _extendedTrackGenre;
            }

            set
            {
                _extendedTrackGenre = value;
            }
        }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        /// <value>
        /// The start time.
        /// </value>
        /// <remarks>
        /// This field is part of the extended tag and is only used when <see cref="UseExtendedTag"/> is set to true.
        /// </remarks>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        /// <value>
        /// The end time.
        /// </value>
        /// <remarks>
        /// This field is part of the extended tag and is only used when <see cref="UseExtendedTag"/> is set to true.
        /// </remarks>
        public TimeSpan EndTime { get; set; }
    }
}
