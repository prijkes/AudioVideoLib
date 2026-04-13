namespace AudioVideoLib.Tags;

/// <summary>
/// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> image encoding restrictions.
/// </summary>
public enum Id3v2ImageEncodingRestriction
{
    /// <summary>
    /// No restrictions.
    /// </summary>
    NoRestrictions = 0x00,

    /// <summary>
    /// Images are encoded only with PNG [PNG] or JPEG [JFIF].
    /// </summary>
    ImageRestricted = 0x01
}
