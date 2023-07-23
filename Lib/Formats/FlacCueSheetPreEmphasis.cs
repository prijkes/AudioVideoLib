/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// The pre-emphasis flag. This corresponds to the CD-DA Q-channel control bit 5.
    /// </summary>
    public enum FlacCueSheetPreEmphasis
    {
        /// <summary>
        /// No pre-emphasis.
        /// </summary>
        NoPreEmphasis = 0,

        /// <summary>
        /// Pre-emphasis.
        /// </summary>
        PreEmphasis = 1
    }
}
