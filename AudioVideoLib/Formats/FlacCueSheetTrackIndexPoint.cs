/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Track index point.
    /// </summary>
    public class FlacCueSheetTrackIndexPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlacCueSheetTrackIndexPoint"/> class.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="indexPointNumber">The index point number.</param>
        /// <param name="reserved">The reserved.</param>
        public FlacCueSheetTrackIndexPoint(long offset, int indexPointNumber, byte[] reserved)
        {
            Offset = offset;
            IndexPointNumber = indexPointNumber;
            Reserved = reserved;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the offset in samples, relative to the track offset, of the index point.
        /// </summary>
        /// <value>
        /// The offset in samples, relative to the track offset, of the index point.
        /// </value>
        /// <remarks>
        /// For CD-DA, the offset must be evenly divisible by 588 samples (588 samples = 44100 samples/sec * 1/75th of a sec).
        /// Note that the offset is from the beginning of the track, not the beginning of the audio data.
        /// </remarks>
        public long Offset { get; private set; }

        /// <summary>
        /// Gets the index point number.
        /// </summary>
        /// <value>
        /// The index point number.
        /// </value>
        /// <remarks>
        /// For CD-DA, an index number of 0 corresponds to the track pre-gap.
        /// The first index in a track must have a number of 0 or 1, and subsequently, index numbers must increase by 1.
        /// Index numbers must be unique within a track.
        /// </remarks>
        public int IndexPointNumber { get; private set; }

        /// <summary>
        /// Gets or sets the reserved.
        /// </summary>
        /// <value>
        /// The reserved.
        /// </value>
        /// <remarks>
        /// All bits must be set to zero.
        /// </remarks>
        public byte[] Reserved { get; private set; }
    }
}
