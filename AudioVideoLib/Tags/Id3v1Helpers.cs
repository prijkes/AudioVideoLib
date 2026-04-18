namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class to store an Id3v1 tag.
/// </summary>
public sealed partial class Id3v1Tag
{
    /// <summary>
    /// Determines whether the specified version is valid.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    /// <c>true</c> if the specified <param name="version">version</param> is valid; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidVersion(Id3v1Version version) => Enum.TryParse(version.ToString(), out Id3v1Version _);

    /// <summary>
    /// Determines whether the specified genre is valid.
    /// </summary>
    /// <param name="genre">The genre.</param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="genre">genre</paramref> is valid; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidGenre(Id3v1Genre genre) => Enum.TryParse(genre.ToString(), out Id3v1Genre _);

    /// <summary>
    /// Determines whether the specified track is valid.
    /// </summary>
    /// <param name="trackSpeed">The track speed.</param>
    /// <returns></returns>
    public static bool IsValidTrackSpeed(Id3v1TrackSpeed trackSpeed) => Enum.TryParse(trackSpeed.ToString(), out Id3v1TrackSpeed _);

    ////------------------------------------------------------------------------------------------------------------------------------

    private string GetTruncatedEncodedString(string value, int maxBytesAllowed) =>
        StreamBuffer.GetTruncatedEncodedString(value, Encoding, maxBytesAllowed);

    private string GetExtendedString(string? value, int maxLengthNormal, int maxLengthExtended, bool onlyLastPart = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var firstPart = GetTruncatedEncodedString(value, maxLengthNormal);
        if (!UseExtendedTag || (firstPart.Length == value.Length))
        {
            return onlyLastPart ? string.Empty : firstPart;
        }

        var secondPart = GetTruncatedEncodedString(value[firstPart.Length..], maxLengthExtended);
        return onlyLastPart ? secondPart : firstPart + secondPart;
    }
}
