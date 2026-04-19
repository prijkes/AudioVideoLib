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
            field = value ?? throw new ArgumentNullException(nameof(value));
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
        var extendedTitleBytes = System.Array.Empty<byte>();
        var extendedArtistBytes = System.Array.Empty<byte>();
        var extendedAlbumTitleBytes = System.Array.Empty<byte>();
        var extendedGenreBytes = System.Array.Empty<byte>();
        Id3v1TrackSpeed trackSpeed = default;
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
                extendedTitleBytes = ReadFixedBytes(sb, 60);
                extendedArtistBytes = ReadFixedBytes(sb, 60);
                extendedAlbumTitleBytes = ReadFixedBytes(sb, 60);
                var trackSpeedByte = (Id3v1TrackSpeed)sb.ReadByte();
                trackSpeed = Id3v1Tag.IsValidTrackSpeed(trackSpeedByte) ? trackSpeedByte : Id3v1TrackSpeed.Unset;
                extendedGenreBytes = ReadFixedBytes(sb, 30);
                startTime = GetTimeSpan(sb.ReadString(6, Encoding));
                endTime = GetTimeSpan(sb.ReadString(6, Encoding));
                useExtendedTag = true;
            }
            else
            {
                sb.Position = startOffset + HeaderIdentifierBytes.Length;
            }
        }

        // Read the standard 128-byte block as raw bytes per field.
        var titleBytes = ReadFixedBytes(sb, 30);
        var artistBytes = ReadFixedBytes(sb, 30);
        var albumTitleBytes = ReadFixedBytes(sb, 30);
        var yearBytes = ReadFixedBytes(sb, 4);
        var commentRaw = ReadFixedBytes(sb, 30);
        byte trackNumber = 0;
        byte[] commentBytes;
        if (commentRaw.Length == 30 && commentRaw[28] == 0x00 && commentRaw[29] != 0x00)
        {
            version = Id3v1Version.Id3v11;
            trackNumber = commentRaw[29];
            commentBytes = new byte[28];
            Array.Copy(commentRaw, commentBytes, 28);
        }
        else
        {
            commentBytes = commentRaw;
        }

        var genreByte = (Id3v1Genre)sb.ReadByte();
        var genre = Id3v1Tag.IsValidGenre(genreByte) ? genreByte : Id3v1Genre.Unknown;

        var tag = new Id3v1Tag(version) { Encoding = Encoding };

        if (useExtendedTag)
        {
            tag.TrackSpeed = trackSpeed;
            tag.StartTime = startTime;
            tag.EndTime = endTime;
            tag.UseExtendedTag = useExtendedTag;
        }

        var fullTitleBytes = ConcatBytes(titleBytes, extendedTitleBytes);
        var fullArtistBytes = ConcatBytes(artistBytes, extendedArtistBytes);
        var fullAlbumTitleBytes = ConcatBytes(albumTitleBytes, extendedAlbumTitleBytes);

        tag.TrackTitleEncoding = SniffEncoding(fullTitleBytes);
        tag.ArtistEncoding = SniffEncoding(fullArtistBytes);
        tag.AlbumTitleEncoding = SniffEncoding(fullAlbumTitleBytes);
        tag.AlbumYearEncoding = SniffEncoding(yearBytes);
        tag.TrackCommentEncoding = SniffEncoding(commentBytes);
        if (useExtendedTag)
        {
            tag.ExtendedTrackGenreEncoding = SniffEncoding(extendedGenreBytes);
        }

        tag.TrackTitleRawBytes = fullTitleBytes;
        tag.ArtistRawBytes = fullArtistBytes;
        tag.AlbumTitleRawBytes = fullAlbumTitleBytes;
        tag.AlbumYearRawBytes = yearBytes;
        tag.TrackCommentRawBytes = commentBytes;
        if (useExtendedTag)
        {
            tag.ExtendedTrackGenreRawBytes = extendedGenreBytes;
        }

        tag.Genre = genre;
        if (version == Id3v1Version.Id3v11)
        {
            tag.TrackNumber = trackNumber;
        }

        var endOffset = sb.Position;
        return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
    }

    private static byte[] ReadFixedBytes(Stream sb, int count)
    {
        var buffer = new byte[count];
        var read = sb.Read(buffer, 0, count);
        if (read == count)
        {
            return buffer;
        }

        var trimmed = new byte[read];
        Array.Copy(buffer, trimmed, read);
        return trimmed;
    }

    private static byte[] ConcatBytes(byte[] a, byte[] b)
    {
        if (b.Length == 0)
        {
            return a;
        }

        var combined = new byte[a.Length + b.Length];
        Array.Copy(a, combined, a.Length);
        Array.Copy(b, 0, combined, a.Length, b.Length);
        return combined;
    }

    /// <summary>
    /// If the bytes parse cleanly as UTF-8 *and* contain at least one multi-byte
    /// sequence (so the choice is meaningful), return UTF-8 as the per-field encoding.
    /// Otherwise return null so the field falls back to the tag-level default.
    /// </summary>
    private static Encoding? SniffEncoding(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        // Strip trailing zero padding so it doesn't confuse the multi-byte test.
        var len = bytes.Length;
        while (len > 0 && bytes[len - 1] == 0)
        {
            len--;
        }

        if (len == 0)
        {
            return null;
        }

        var hasNonAscii = false;
        for (var i = 0; i < len; i++)
        {
            if (bytes[i] >= 0x80)
            {
                hasNonAscii = true;
                break;
            }
        }

        if (!hasNonAscii)
        {
            // Pure ASCII — every encoding decodes identically; no override needed.
            return null;
        }

        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = strict.GetString(bytes, 0, len);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
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

        var timeValues = time.Split(':');
        int.TryParse(timeValues[0], out var minutes);

        var seconds = 0;
        if (timeValues.Length > 1)
        {
            int.TryParse(timeValues[1], out seconds);
        }

        return new TimeSpan(0, 0, minutes, seconds);
    }
}
