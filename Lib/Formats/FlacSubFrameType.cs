/*
 * Date: 2013-02-23
 * Sources used:
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Picture types.
    /// </summary>
    public enum FlacSubFrameType
    {
        /// <summary>
        /// The constant
        /// </summary>
        Constant,

        /// <summary>
        /// The verbatim
        /// </summary>
        Verbatim,

        /// <summary>
        /// The reserved
        /// </summary>
        Reserved,

        /// <summary>
        /// The fixed
        /// </summary>
        Fixed,

        /// <summary>
        /// The linear predictor
        /// </summary>
        LinearPredictor
    }
}
