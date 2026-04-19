namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

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
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Version = version;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the <see cref="Id3v1Version"/>.
    /// </summary>
    public Id3v1Version Version { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Id3v1Tag);

    /// <inheritdoc/>
    public bool Equals(IAudioTag? other) => Equals(other as Id3v1Tag);

    /// <summary>
    /// Equals the specified <see cref="Id3v1Tag"/>.
    /// </summary>
    /// <param name="tag">The <see cref="Id3v1Tag"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    public bool Equals(Id3v1Tag? tag)
    {
        return tag is not null && (ReferenceEquals(this, tag) || ((tag.Version == Version) && (tag.AlbumTitle == AlbumTitle) && (tag.AlbumYear == AlbumYear)
               && (tag.Artist == Artist) && (tag.TrackComment == TrackComment) && (tag.Genre == Genre)
               && (tag.TrackNumber == TrackNumber) && (tag.TrackTitle == TrackTitle)
               && (tag.UseExtendedTag == UseExtendedTag) && (tag.TrackSpeed == TrackSpeed)
               && (tag.ExtendedTrackGenre == ExtendedTrackGenre) && (tag.StartTime == StartTime)
               && (tag.EndTime == EndTime)));
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
    /// Field bytes are written verbatim from the per-field raw byte storage, so the per-field
    /// <see cref="Encoding"/> chosen at parse (or set via <c>...Encoding</c> properties) round-trips
    /// faithfully.
    /// </remarks>
    public byte[] ToByteArray()
    {
        var buffer = new StreamBuffer();

        if (UseExtendedTag)
        {
            // TAG+
            buffer.Write(ExtendedHeaderIdentifierBytes);

            // Title - write the last 60 bytes (extended portion)
            WriteFixedField(buffer, SliceFrom(_titleBytes, 30, 60), 60);

            // Artist - write the last 60 bytes
            WriteFixedField(buffer, SliceFrom(_artistBytes, 30, 60), 60);

            // Album Title - write the last 60 bytes
            WriteFixedField(buffer, SliceFrom(_albumTitleBytes, 30, 60), 60);

            // Track Speed
            buffer.WriteByte((byte)TrackSpeed);

            // Extended Track Genre
            WriteFixedField(buffer, _extendedTrackGenreBytes, 30);

            // Start-time
            long startTimeMinutes = (StartTime.Days * 24 * 60) + (StartTime.Hours * 60) + StartTime.Minutes;
            long startTimeSeconds = StartTime.Seconds;
            var startTime = $"{startTimeMinutes:000}:{startTimeSeconds:00}";
            var startTimeBytes = EncodeAndTruncate(startTime, Encoding, 6);
            WriteFixedField(buffer, startTimeBytes, 6);

            // End-time
            long endTimeMinutes = (EndTime.Days * 24 * 60) + (EndTime.Hours * 60) + EndTime.Minutes;
            long endTimeSeconds = EndTime.Seconds;
            var endTime = $"{endTimeMinutes:000}:{endTimeSeconds:00}";
            var endTimeBytes = EncodeAndTruncate(endTime, Encoding, 6);
            WriteFixedField(buffer, endTimeBytes, 6);
        }

        // TAG
        buffer.Write(HeaderIdentifierBytes);

        // Track Title (first 30 bytes)
        WriteFixedField(buffer, SliceFrom(_titleBytes, 0, 30), 30);

        // Artist (first 30 bytes)
        WriteFixedField(buffer, SliceFrom(_artistBytes, 0, 30), 30);

        // Album Title (first 30 bytes)
        WriteFixedField(buffer, SliceFrom(_albumTitleBytes, 0, 30), 30);

        // Album Year
        WriteFixedField(buffer, _albumYearBytes, 4);

        // Track comment
        WriteFixedField(buffer, _trackCommentBytes, TrackCommentLength);

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

    private static void WriteFixedField(StreamBuffer buffer, byte[] bytes, int fixedLength)
    {
        var actual = System.Math.Min(bytes.Length, fixedLength);
        if (actual > 0)
        {
            buffer.Write(bytes, 0, actual);
        }

        if (actual < fixedLength)
        {
            buffer.WritePadding(0x00, fixedLength - actual);
        }
    }

    private static byte[] SliceFrom(byte[] source, int offset, int length)
    {
        if (source.Length <= offset)
        {
            return [];
        }

        var available = System.Math.Min(length, source.Length - offset);
        var slice = new byte[available];
        Array.Copy(source, offset, slice, 0, available);
        return slice;
    }

    /// <inheritdoc/>
    public override string ToString() => Version.ToString();
}
