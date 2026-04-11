/*
 * Date: 2012-11-26
 * Sources used:
 *  http://id3.org/Lyrics3v2
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// A list of pre-defined known <see cref="Lyrics3v2TextField"/> identifiers in a <see cref="Lyrics3v2Tag"/>.
    /// </summary>
    public enum Lyrics3v2TextFieldIdentifier
    {
        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.AdditionalInformation"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        AdditionalInformation,

        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.ExtendedAlbumName"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        ExtendedAlbumName,

        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.ExtendedArtistName"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        ExtendedArtistName,

        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.ExtendedTrackTitle"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        ExtendedTrackTitle,

        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.Genre"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        Genre,

        /// <summary>
        /// Identifier for the <see cref="Lyrics3v2Tag.LyricsAuthorName"/> <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        LyricsAuthorName
    }
}
