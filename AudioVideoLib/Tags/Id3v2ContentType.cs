namespace AudioVideoLib.Tags;

/// <summary>
/// Possible values the content type can be.
/// </summary>
public enum Id3v2ContentType
{
    /// <summary>
    /// Indicates Other content type.
    /// </summary>
    Other = 0x00,

    /// <summary>
    /// Indicates Lyrics.
    /// </summary>
    Lyrics = 0x01,

    /// <summary>
    /// Indicates Text transcription.
    /// </summary>
    TextTranscription = 0x02,

    /// <summary>
    /// Indicates a Movement/part name (e.g. "Adagio").
    /// </summary>
    MovementName = 0x03,

    /// <summary>
    /// Indicates Events (e.g. "Don Did enters the stage").
    /// </summary>
    Events = 0x04,

    /// <summary>
    /// Indicates Chord (e.g. "Bb F").
    /// </summary>
    Chord = 0x05,

    /// <summary>
    /// Indicates Trivia/'pop up' information.
    /// </summary>
    /// <remarks>
    /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
    /// </remarks>
    Trivia = 0x06,

    /// <summary>
    /// Indicates URLs to webpages.
    /// </summary>
    /// <remarks>
    /// This value has been added as of <see cref="Id3v2Version.Id3v240"/> and should not be used for earlier versions.
    /// </remarks>
    WebpagesUrls = 0x07,

    /// <summary>
    /// Indicates URLs to images.
    /// </summary>
    /// <remarks>
    /// This value has been added as of <see cref="Id3v2Version.Id3v240"/> and should not be used for earlier versions.
    /// </remarks>
    ImagesUrls = 0x08
}
