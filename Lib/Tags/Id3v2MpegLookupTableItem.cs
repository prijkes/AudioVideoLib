/*
 * Date: 2011-08-27
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// An MPEG lookup table item in the <see cref="Id3v2MpegLocationLookupTableFrame"/>.
    /// </summary>
    public class Id3v2MpegLookupTableItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2MpegLookupTableItem"/> class.
        /// </summary>
        /// <param name="deviationInBytes">The deviation in bytes.</param>
        /// <param name="devationInMilliseconds">The deviation in milliseconds.</param>
        public Id3v2MpegLookupTableItem(byte deviationInBytes, byte devationInMilliseconds)
        {
            DeviationInBytes = deviationInBytes;
            DeviationInMilliseconds = devationInMilliseconds;
        }

        /// <summary>
        /// Gets the deviation in bytes.
        /// </summary>
        /// <remarks>
        /// Each reference consists of two parts; a certain number of bits, as defined in bits for bytes deviation,
        /// that describes the difference between what is said in bytes between reference and the reality and a certain number of bits,
        /// as defined in bits for milliseconds deviation,
        /// that describes the difference between what is said in milliseconds between reference and the reality.
        /// The number of bits in every reference, i.e. bits for bytes deviation + bits for milliseconds deviation,
        /// must be a multiple of four.
        /// </remarks>
        public byte DeviationInBytes { get; private set; }

        /// <summary>
        /// Gets the deviation in milliseconds.
        /// </summary>
        /// <remarks>
        /// Each reference consists of two parts; a certain number of bits, as defined in bits for bytes deviation, 
        /// that describes the difference between what is said in bytes between reference and the reality and a certain number of bits, 
        /// as defined in bits for milliseconds deviation, 
        /// that describes the difference between what is said in milliseconds between reference and the reality.
        /// The number of bits in every reference, i.e. bits for bytes deviation + bits for milliseconds deviation, 
        /// must be a multiple of four.
        /// </remarks>
        public byte DeviationInMilliseconds { get; private set; }
    }
}
