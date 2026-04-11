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
    /// Sign indicator in an <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
    /// </summary>
    public enum Id3v2ReplayGainSign
    {
        /// <summary>
        /// A positive sign.
        /// </summary>
        Positive = 0x00,

        /// <summary>
        /// A negative sign.
        /// </summary>
        Negative = 0x01
    }
}
