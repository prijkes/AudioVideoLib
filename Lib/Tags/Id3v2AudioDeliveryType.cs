/*
 * Date: 2011-11-05
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
    /// Describes how audio is delivered when bought.
    /// </summary>
    public enum Id3v2AudioDeliveryType
    {
        /// <summary>
        /// Other Type
        /// </summary>
        Other = 0x00,

        /// <summary>
        /// Standard CD album with other songs
        /// </summary>
        StandardCd = 0x01,

        /// <summary>
        /// Compressed audio on CD
        /// </summary>
        CompressedAudioCd = 0x02,

        /// <summary>
        /// File over the Internet
        /// </summary>
        InternetFile = 0x03,

        /// <summary>
        /// Stream over the Internet
        /// </summary>
        InternetStream = 0x04,

        /// <summary>
        /// As note sheets
        /// </summary>
        NoteSheets = 0x05,

        /// <summary>
        /// As note sheets in a book with other sheets
        /// </summary>
        NoteSheetsCompilation = 0x06,

        /// <summary>
        /// Music on other media
        /// </summary>
        MusicMedia = 0x07,

        /// <summary>
        /// Non-musical merchandise
        /// </summary>
        NonMusicalMerchandise = 0x08
    }
}
