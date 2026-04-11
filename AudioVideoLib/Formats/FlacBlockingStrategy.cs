/*
 * Date: 2013-02-18
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// 
    /// </summary>
    public enum FlacBlockingStrategy
    {
        /// <summary>
        /// Fixed-blocksize stream; frame header encodes the frame number.
        /// </summary>
        FixedBlocksize = 0,

        /// <summary>
        /// Fixed variable-blocksize stream; frame header encodes the sample number.
        /// </summary>
        VariableBlocksize = 1
    }
}
