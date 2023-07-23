/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.id3.org/Id3v1
 *  http://en.wikipedia.org/wiki/ID3#Extended_tag
 *  http://lib313.sourceforge.net/id3v13.html
 */
using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v1 tag.
    /// </summary>
    public sealed class Id3v1TagReader : IAudioTagReader
    {
        private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(Id3v1Tag.HeaderIdentifier);

        private static readonly byte[] ExtendedHeaderIdentifierBytes = Encoding.ASCII.GetBytes(Id3v1Tag.ExtendedHeaderIdentifier);

        private Encoding _encoding = Encoding.Default;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> used to read the <see cref="Id3v1Tag"/> from a stream.
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

        /// <inheritdoc/>
        public IAudioTagOffset ReadFromStream(Stream stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
                throw new InvalidOperationException("stream can not be read");

            if (!stream.CanSeek)
                throw new InvalidOperationException("stream can not be seeked");

            StreamBuffer sb = stream as StreamBuffer ?? new StreamBuffer(stream);
            long startOffset = FindIdentifier(sb, tagOrigin);
            if (startOffset < 0)
                return null;

            Id3v1Version version = Id3v1Version.Id3v10;
            string extendedTrackTitle = String.Empty;
            string extendedArtist = String.Empty;
            string extendAlbumTitle = String.Empty;
            Id3v1TrackSpeed trackSpeed = default(Id3v1TrackSpeed);
            string extendedTrackGenre = String.Empty;
            TimeSpan startTime = default(TimeSpan);
            TimeSpan endTime = default(TimeSpan);
            bool useExtendedTag = false;
            if ((startOffset - Id3v1Tag.ExtendedSize) >= 0)
            {
                sb.Position = startOffset - Id3v1Tag.ExtendedSize;
                string extendedHeaderIdentifier = sb.ReadString(ExtendedHeaderIdentifierBytes.Length);
                if (String.Equals(extendedHeaderIdentifier, Id3v1Tag.ExtendedHeaderIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    startOffset = startOffset - ExtendedHeaderIdentifierBytes.Length;
                    extendedTrackTitle = sb.ReadString(60, _encoding);
                    extendedArtist = sb.ReadString(60, _encoding);
                    extendAlbumTitle = sb.ReadString(60, _encoding);
                    Id3v1TrackSpeed trackSpeedByte = (Id3v1TrackSpeed)sb.ReadByte();
                    trackSpeed = Id3v1Tag.IsValidTrackSpeed(trackSpeedByte) ? trackSpeedByte : Id3v1TrackSpeed.Unset;
                    extendedTrackGenre = sb.ReadString(30, _encoding);
                    startTime = GetTimeSpan(sb.ReadString(6, _encoding));
                    endTime = GetTimeSpan(sb.ReadString(6, _encoding));
                    useExtendedTag = true;
                }
                else
                    sb.Position = startOffset + HeaderIdentifierBytes.Length;
            }

            // Read the rest of the tag.
            string trackTitle = sb.ReadString(30, _encoding) + extendedTrackTitle;
            string artist = sb.ReadString(30, _encoding) + extendedArtist;
            string albumTitle = sb.ReadString(30, _encoding) + extendAlbumTitle;
            string albumYear = sb.ReadString(4);
            byte[] comment = new byte[30];
            int commentBytesRead = sb.Read(comment, 30);
            if ((commentBytesRead == 30) && (comment[28] == '\0') && (comment[29] != '\0'))
            {
                version = Id3v1Version.Id3v11;
            }
            Id3v1Genre genreByte = (Id3v1Genre)sb.ReadByte();
            Id3v1Genre genre = Id3v1Tag.IsValidGenre(genreByte) ? genreByte : Id3v1Genre.Unknown;

            // Set values
            Id3v1Tag tag = new Id3v1Tag(version);
            if (useExtendedTag)
            {
                tag.TrackSpeed = trackSpeed;
                tag.ExtendedTrackGenre = extendedTrackGenre;
                tag.StartTime = startTime;
                tag.EndTime = endTime;
                tag.UseExtendedTag = useExtendedTag;
            }
            tag.TrackTitle = trackTitle;
            tag.Artist = artist;
            tag.AlbumTitle = albumTitle;
            tag.AlbumYear = albumYear;
            tag.Genre = genre;
            if ((commentBytesRead == 30) && (comment[28] == '\0') && (comment[29] != '\0'))
            {
                tag.TrackNumber = comment[29];
                tag.TrackComment = _encoding.GetString(comment, 0, 28);
            }
            else
                tag.TrackComment = _encoding.GetString(comment);

            long endOffset = sb.Position;
            return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static long FindIdentifier(Stream stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            if (streamLength < Id3v1Tag.TotalSize)
                return -1;

            if (tagOrigin == TagOrigin.Start)
            {
                // Look for the tag at the current position
                long startPositionTag = Math.Max(startPosition, 0);
                long endPositionTag = Math.Min(startPosition + HeaderIdentifierBytes.Length, streamLength);
                long tagPosition = ReadIdentifier(stream, startPositionTag, endPositionTag);
                if (tagPosition >= 0)
                    return tagPosition;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                // Look for tag before the current position
                long startPositionTag = (startPosition - Id3v1Tag.TotalSize) >= 0
                                            ? startPosition - Id3v1Tag.TotalSize
                                            : Math.Max(startPosition - HeaderIdentifierBytes.Length, 0);
                long endPositionTag = Math.Min(startPositionTag + HeaderIdentifierBytes.Length, streamLength);
                long tagPosition = ReadIdentifier(stream, startPositionTag, endPositionTag);
                if (tagPosition >= 0)
                    return tagPosition;
            }
            return -1;
        }

        private static long ReadIdentifier(Stream sb, long startPosition, long endPosition)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            sb.Position = startPosition;
            while (startPosition < endPosition)
            {
                int y = 0;
                for (int b = sb.ReadByte(); b == HeaderIdentifierBytes[y++]; b = sb.ReadByte())
                {
                    startPosition++;
                    if (y == HeaderIdentifierBytes.Length)
                        return sb.Position - HeaderIdentifierBytes.Length;
                }
                startPosition++;
            }
            return -1;
        }

        private static TimeSpan GetTimeSpan(string time)
        {
            if (time == null)
                throw new ArgumentNullException("time");

            string[] timeValues = time.Split(new[] { ':' });
            int minutes, seconds = 0;
            Int32.TryParse(timeValues[0], out minutes);

            if (timeValues.Length > 1)
                Int32.TryParse(timeValues[1], out seconds);

            return new TimeSpan(0, 0, minutes, seconds);
        }
    }
}
