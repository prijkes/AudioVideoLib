/*
 * Date: 2012-12-27
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://web.archive.org/web/20080415005443/http://replaygain.hydrogenaudio.org/rg_data_format.html
 *  http://web.archive.org/web/20081223230059/http://replaygain.hydrogenaudio.org/file_format_id3v2.html
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Originator code in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
    /// </summary>
    public enum Id3v2OriginatorCode
    {
        /// <summary>
        /// The replay gain is unspecified.
        /// </summary>
        Unspecified = 0x00,

        /// <summary>
        /// The replay gain is pre-set by the artist/producer/mastering engineer.
        /// </summary>
        PreSetByProducer = 0x01,

        /// <summary>
        /// The replay gain is set by user.
        /// </summary>
        SetByUser = 0x02,

        /// <summary>
        /// The replay gain is determined automatically.
        /// </summary>
        DeterminedAutomatically = 0x03,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved1 = 0x04,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved2 = 0x05,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved3 = 0x06
    }
}
