/*
 * Date: 2013-02-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// The residual coding methods.
    /// </summary>
    public enum FlacResidualCodingMethod
    {
        /// <summary>
        /// The partitioned rice coding with 4-bit rice parameter.
        /// </summary>
        PartitionedRice = 0x00,

        /// <summary>
        /// The partitioned rice coding with 5-bit rice parameter.
        /// </summary>
        PartitionedRice2 = 0x01,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved1 = 0x02,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved2 = 0x03
    }
}
