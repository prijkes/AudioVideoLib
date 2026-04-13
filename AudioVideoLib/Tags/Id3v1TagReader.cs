namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

/// <summary>
/// Class to store an Id3v1 tag.
/// </summary>
public sealed class Id3v1TagReader : IAudioTagReader
{
    private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(Id3v1Tag.HeaderIdentifier);

    private static readonly byte[] ExtendedHeaderIdentifierBytes = Encoding.ASCII.GetBytes(Id3v1Tag.ExtendedHeaderIdentifier);

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the <see cref="Encoding"/> used to read the <see cref="Id3v1Tag"/> from a stream.
    /// </summary>
    public Encoding Encoding
    {
        get;

        set
        {
            field = value ?? throw new ArgumentNullException("value");
        }
    } = Encoding.Default;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("stream can not be read");
        }

        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("stream can not be seeked");
        }

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var startOffset = FindIdentifier(sb, tagOrigin);
        if (startOffset < 0)
        {
            return null;
        }

        var version = Id3v1Version.Id3v10;
        var extendedTrackTitle = string.Empty;
        var extendedArtist = string.Empty;
        var extendAlbumTitle = string.Empty;
        Id3v1TrackSpeed trackSpeed = default;
        var extendedTrackGenre = string.Empty;
        TimeSpan startTime = default;
        TimeSpan endTime = default;
        var useExtendedTag = false;
        if ((startOffset - Id3v1Tag.ExtendedSize) >= 0)
        {
            sb.Position = startOffset - Id3v1Tag.ExtendedSize;
            var extendedHeaderIdentifier = sb.ReadString(ExtendedHeaderIdentifierBytes.Length);
            if (string.Equals(extendedHeaderIdentifier, Id3v1Tag.ExtendedHeaderIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                startOffset -= ExtendedHeaderIdentifierBytes.Length;
                extendedTrackTitle = sb.ReadString(60, Encoding);
                extendedArtist = sb.ReadString(60, Encoding);
                extendAlbumTitle = sb.ReadString(60, Encoding);
                var trackSpeedByte = (Id3v1TrackSpeed)sb.ReadByte();
                trackSpeed = Id3v1Tag.IsValidTrackSpeed(trackSpeedByte) ? trackSpeedByte : Id3v1TrackSpeed.Unset;
                extendedTrackGenre = sb.ReadString(30, Encoding);
                startTime = GetTimeSpan(sb.ReadString(6, Encoding));
                endTime = GetTimeSpan(sb.ReadString(6, Encoding));
                useExtendedTag = true;
            }
            else
            {
                sb.Position = startOffset + HeaderIdentifierBytes.Length;
            }
        }

        // Read the rest of the tag.
        var trackTitle = sb.ReadString(30, Encoding) + extendedTrackTitle;
        var artist = sb.ReadString(30, Encoding) + extendedArtist;
        var albumTitle = sb.ReadString(30, Encoding) + extendAlbumTitle;
        var albumYear = sb.ReadString(4);
        var comment = new byte[30];
        var commentBytesRead = sb.Read(comment, 30);
        if ((commentBytesRead == 30) && (comment[28] == '\0') && (comment[29] != '\0'))
        {
            version = Id3v1Version.Id3v11;
        }
        var genreByte = (Id3v1Genre)sb.ReadByte();
        var genre = Id3v1Tag.IsValidGenre(genreByte) ? genreByte : Id3v1Genre.Unknown;

        // Set values
        var tag = new Id3v1Tag(version);
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
            tag.TrackComment = Encoding.GetString(comment, 0, 28);
        }
        else
        {
            tag.TrackComment = Encoding.GetString(comment);
        }

        var endOffset = sb.Position;
        return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static long FindIdentifier(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var startPosition = stream.Position;
        var streamLength = stream.Length;
        if (streamLength < Id3v1Tag.TotalSize)
        {
            return -1;
        }

        if (tagOrigin == TagOrigin.Start)
        {
            // Look for the tag at the current position
            var startPositionTag = Math.Max(startPosition, 0);
            var endPositionTag = Math.Min(startPosition + HeaderIdentifierBytes.Length, streamLength);
            var tagPosition = ReadIdentifier(stream, startPositionTag, endPositionTag);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }
        }
        else if (tagOrigin == TagOrigin.End)
        {
            // Look for tag before the current position
            var startPositionTag = (startPosition - Id3v1Tag.TotalSize) >= 0
                                        ? startPosition - Id3v1Tag.TotalSize
                                        : Math.Max(startPosition - HeaderIdentifierBytes.Length, 0);
            var endPositionTag = Math.Min(startPositionTag + HeaderIdentifierBytes.Length, streamLength);
            var tagPosition = ReadIdentifier(stream, startPositionTag, endPositionTag);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }
        }
        return -1;
    }

    private static long ReadIdentifier(Stream sb, long startPosition, long endPosition)
    {
        ArgumentNullException.ThrowIfNull(sb);

        sb.Position = startPosition;
        while (startPosition < endPosition)
        {
            var y = 0;
            for (var b = sb.ReadByte(); b == HeaderIdentifierBytes[y++]; b = sb.ReadByte())
            {
                startPosition++;
                if (y == HeaderIdentifierBytes.Length)
                {
                    return sb.Position - HeaderIdentifierBytes.Length;
                }
            }
            startPosition++;
        }
        return -1;
    }

    private static TimeSpan GetTimeSpan(string time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var timeValues = time.Split([':']);
        int minutes, seconds = 0;
        int.TryParse(timeValues[0], out minutes);

        if (timeValues.Length > 1)
        {
            int.TryParse(timeValues[1], out seconds);
        }

        return new TimeSpan(0, 0, minutes, seconds);
    }
}
