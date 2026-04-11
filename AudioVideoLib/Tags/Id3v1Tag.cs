/*
 * Date: 2010-05-19
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.id3.org/Id3v1
 *  http://en.wikipedia.org/wiki/ID3#Extended_tag
 *  http://lib313.sourceforge.net/id3v13.html
 */
using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v1 tag.
    /// </summary>
    public sealed partial class Id3v1Tag : IAudioTag
    {
        /// <summary>
        /// The total size of an <see cref="Id3v1Tag"/>.
        /// </summary>
        /// <remarks>
        /// The total size of the <see cref="Id3v1Tag"/> is always 128 bytes.
        /// </remarks>
        public const int TotalSize = 128;

        /// <summary>
        /// The extended size of an <see cref="Id3v1Tag"/>.
        /// </summary>
        /// <remarks>
        /// The extended size of an <see cref="Id3v1Tag"/> is always 277 bytes, and is only used when <see cref="Id3v1Tag.UseExtendedTag"/> is set to true.
        /// </remarks>
        public const int ExtendedSize = 277;

        /// <summary>
        /// The header identifier for an <see cref="Id3v1Tag"/>.
        /// </summary>
        public const string HeaderIdentifier = "TAG";

        private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(HeaderIdentifier);

        /// <summary>
        /// The extended header identifier for an <see cref="Id3v1Tag"/>.
        /// </summary>
        /// <remarks>
        /// The extended header identifier is only used when <see cref="Id3v1Tag.UseExtendedTag"/> is set to true.
        /// </remarks>
        public const string ExtendedHeaderIdentifier = "TAG+";

        private static readonly byte[] ExtendedHeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(ExtendedHeaderIdentifier);

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v1Tag"/> class.
        /// </summary>
        public Id3v1Tag()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v1Tag"/> class.
        /// </summary>
        /// <param name="version">The <see cref="Id3v1Version"/>.</param>
        public Id3v1Tag(Id3v1Version version)
        {
            if (!IsValidVersion(version))
                throw new ArgumentOutOfRangeException("version");

            Version = version;
        }

        ////------------------------------------------------------------------------------------------------------------------------------
       
        /// <summary>
        /// Gets the <see cref="Id3v1Version"/>.
        /// </summary>
        public Id3v1Version Version { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Id3v1Tag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as Id3v1Tag);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v1Tag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="Id3v1Tag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Id3v1Tag tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            return (tag.Version == Version) && (tag.AlbumTitle == AlbumTitle) && (tag.AlbumYear == AlbumYear)
                   && (tag.Artist == Artist) && (tag.TrackComment == TrackComment) && (tag.Genre == Genre)
                   && (tag.TrackNumber == TrackNumber) && (tag.TrackTitle == TrackTitle)
                   && (tag.UseExtendedTag == UseExtendedTag) && (tag.TrackSpeed == TrackSpeed)
                   && (tag.ExtendedTrackGenre == ExtendedTrackGenre) && (tag.StartTime == StartTime)
                   && (tag.EndTime == EndTime);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return Version.GetHashCode() * 397;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

       /// <inheritdoc/>
        /// <remarks>
        /// The extended tags will only be written when <see cref="UseExtendedTag"/> is set to true.
        /// </remarks>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                byte[] byteValue;
                if (UseExtendedTag)
                {
                    // TAG+
                    buffer.Write(ExtendedHeaderIdentifierBytes);

                    // Title - write the last 60 bytes
                    byteValue = _encoding.GetBytes(GetExtendedString(_trackTitle ?? String.Empty, 30, 60, true));
                    buffer.Write(byteValue);
                    buffer.WritePadding(0x00, 60 - byteValue.Length);

                    // Artist - write the last 60 bytes
                    byteValue = _encoding.GetBytes(GetExtendedString(_artist ?? String.Empty, 30, 60, true));
                    buffer.Write(byteValue);
                    buffer.WritePadding(0x00, 60 - byteValue.Length);

                    // Album Title - write the last 60 bytes
                    byteValue = _encoding.GetBytes(GetExtendedString(_albumTitle ?? String.Empty, 30, 60, true));
                    buffer.Write(byteValue);
                    buffer.WritePadding(0x00, 60 - byteValue.Length);

                    // Track Speed
                    buffer.WriteByte((byte)_trackSpeed);

                    // Extended Track Genre
                    byteValue = _encoding.GetBytes(ExtendedTrackGenre ?? String.Empty);
                    buffer.Write(byteValue);
                    buffer.WritePadding(0x00, 30 - byteValue.Length);

                    // Start-time
                    long startTimeMinutes = (StartTime.Days * 24 * 60) + (StartTime.Hours * 60) + StartTime.Minutes;
                    long startTimeSeconds = StartTime.Seconds;
                    string startTime = startTimeMinutes.ToString("000") + ":" + startTimeSeconds.ToString("00");
                    byteValue = _encoding.GetBytes(GetTruncatedEncodedString(startTime, 6));
                    buffer.Write(byteValue);

                    // End-time
                    long endTimeMinutes = (EndTime.Days * 24 * 60) + (EndTime.Hours * 60) + EndTime.Minutes;
                    long endTimeSeconds = EndTime.Seconds;
                    string endTime = endTimeMinutes.ToString("000") + ":" + endTimeSeconds.ToString("00");
                    byteValue = _encoding.GetBytes(GetTruncatedEncodedString(endTime, 6));
                    buffer.Write(byteValue);
                }

                // TAG
                buffer.Write(HeaderIdentifierBytes);

                // Track Title
                byteValue = _encoding.GetBytes(GetTruncatedEncodedString(_trackTitle ?? String.Empty, 30));
                buffer.Write(byteValue);
                buffer.WritePadding(0x00, 30 - byteValue.Length);

                // Artist
                byteValue = _encoding.GetBytes(GetTruncatedEncodedString(_artist ?? String.Empty, 30));
                buffer.Write(byteValue);
                buffer.WritePadding(0x00, 30 - byteValue.Length);

                // Album Title
                byteValue = _encoding.GetBytes(GetTruncatedEncodedString(_albumTitle ?? String.Empty, 30));
                buffer.Write(byteValue);
                buffer.WritePadding(0x00, 30 - byteValue.Length);

                // Album Year
                byteValue = _encoding.GetBytes(AlbumYear ?? String.Empty);
                buffer.Write(byteValue);
                buffer.WritePadding(0x00, 4 - byteValue.Length);

                // Track comment
                byteValue = _encoding.GetBytes(TrackComment ?? String.Empty);
                buffer.Write(byteValue);
                buffer.WritePadding(0x00, TrackCommentLength - byteValue.Length);

                // Track Number
                if (Version >= Id3v1Version.Id3v11)
                {
                    buffer.WriteByte(0x00);
                    buffer.WriteByte(TrackNumber);
                }

                // Genre
                buffer.WriteByte((byte)Genre);

                return buffer.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Version.ToString();
        }
    }
}
