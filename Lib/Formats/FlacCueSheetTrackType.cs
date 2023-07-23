/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// The track type. This corresponds to the CD-DA Q-channel control bit 3.
    /// </summary>
    public enum FlacCueSheetTrackType
    {
        /// <summary>
        /// Audio.
        /// </summary>
        Audio = 0,

        /// <summary>
        /// Non-audio.
        /// </summary>
        NonAudio = 1
    }
}
