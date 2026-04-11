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
    /// Name code in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
    /// </summary>
    public enum Id3v2NameCode
    {
        /// <summary>
        /// Not set.
        /// </summary>
        NotSet = 0x00,

        /// <summary>
        /// Radio Gain Adjustment.
        /// </summary>
        RadioGainAdjustment = 0x01,

        /// <summary>
        /// Audiophile Gain Adjustment.
        /// </summary>
        AudiophileGainAdjustment = 0x02,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved1 = 0x03,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved2 = 0x04,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved3 = 0x05,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved4 = 0x06,

        /// <summary>
        /// Reserved value for future use.
        /// </summary>
        Reserved5 = 0x07
    }
}
