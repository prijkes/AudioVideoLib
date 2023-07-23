/*
 * Date: 2013-02-02
 * Sources used: 
 *  http://xiph.org/flac/format.html#seekpoint
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// 
    /// </summary>
    public class FlacSeekPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlacSeekPoint" /> class.
        /// </summary>
        /// <param name="sampleNumber">The sample number.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="samples">The samples.</param>
        public FlacSeekPoint(long sampleNumber, long offset, int samples)
        {
            SampleNumber = sampleNumber;
            Offset = offset;
            Samples = samples;
        }
        
        /// <summary>
        /// Gets or sets the sample number of first sample.
        /// </summary>
        /// <value>
        /// The sample number, or 0xFFFFFFFFFFFFFFFF for a placeholder point.
        /// </value>
        public long SampleNumber { get; private set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public long Offset { get; private set; }

        /// <summary>
        /// Gets or sets the number of samples.
        /// </summary>
        /// <value>
        /// The number of samples.
        /// </value>
        public int Samples { get; private set; }
    }
}
