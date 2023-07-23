/*
 * Date: 2011-08-27
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Type of <see cref="Id3v2KeyEvent"/>.
    /// </summary>
    public enum Id3v2KeyEventType
    {
        /// <summary>
        /// Padding (has no meaning).
        /// </summary>
        Padding = 0x00,

        /// <summary>
        /// End of initial silence.
        /// </summary>
        EndOfInitialSilence = 0x01,

        /// <summary>
        /// Intro start.
        /// </summary>
        IntroStart = 0x02,

        /// <summary>
        /// Main part start.
        /// </summary>
        MainPartStart = 0x03,

        /// <summary>
        /// Outro start.
        /// </summary>
        OutroStart = 0x04,

        /// <summary>
        /// Outro end.
        /// </summary>
        OutroEnd = 0x05,

        /// <summary>
        /// Verse begins.
        /// </summary>
        VerseBegins = 0x06,

        /// <summary>
        /// Refrain begins.
        /// </summary>
        RefrainBegins = 0x07,

        /// <summary>
        /// The interlude.
        /// </summary>
        Interlude = 0x08,

        /// <summary>
        /// Theme start.
        /// </summary>
        ThemeStart = 0x09,

        /// <summary>
        /// A variation.
        /// </summary>
        Variation = 0x0A,

        /// <summary>
        /// Key change.
        /// </summary>
        KeyChange = 0x0B,

        /// <summary>
        /// Time change.
        /// </summary>
        TimeChange = 0x0C,

        /// <summary>
        /// Unwanted noise (Snap, Crackle &amp; Pop).
        /// </summary>
        UnwantedNoise = 0x0D,

        /// <summary>
        /// Sustained noise.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        SustainedNoise = 0x0E,

        /// <summary>
        /// Sustained noise end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        SustainedNoiseEnd = 0x0F,

        /// <summary>
        /// Intro end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        IntroEnd = 0x10,

        /// <summary>
        /// Main part end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        MainPartEnd = 0x11,

        /// <summary>
        /// Verse end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        VerseEnd = 0x12,

        /// <summary>
        /// Refrain end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        RefrainEnd = 0x13,

        /// <summary>
        /// Theme end
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v230"/> and should not be used for earlier versions.
        /// </remarks>
        ThemeEnd = 0x14,

        /// <summary>
        /// The profanity.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v240"/> and should not be used for earlier versions.
        /// </remarks>
        Profanity = 0x15,

        /// <summary>
        /// Profanity end.
        /// </summary>
        /// <remarks>
        /// This value has been added as of <see cref="Id3v2Version.Id3v240"/> and should not be used for earlier versions.
        /// </remarks>
        ProfanityEnd = 0x16,

        /// <summary>
        /// Audio end (start of silence).
        /// </summary>
        AudioEnd = 0xFD,

        /// <summary>
        /// Audio file ends.
        /// </summary>
        AudioFileEnds = 0xFE,
    }
}
