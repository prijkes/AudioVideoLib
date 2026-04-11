/*
 * Date: 2011-08-28
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
    /// Adjustment point information for each adjustment point in an <see cref="Id3v2Equalisation2Frame"/> frame.
    /// </summary>
    public class Id3v2AdjustmentPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2AdjustmentPoint"/> class.
        /// </summary>
        /// <param name="frequency">The frequency.</param>
        /// <param name="volumeAdjustment">The volume adjustment.</param>
        public Id3v2AdjustmentPoint(short frequency, short volumeAdjustment)
        {
            Frequency = frequency;
            VolumeAdjustment = volumeAdjustment;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the frequency.
        /// </summary>
        /// <remarks>
        /// The frequency is stored in units of 1/2 Hz, giving it a range from 0 to 32767 Hz.
        /// </remarks>
        public short Frequency { get; private set; }

        /// <summary>
        /// Gets the volume adjustment.
        /// </summary>
        /// <remarks>
        /// The volume adjustment is encoded as a fixed point decibel value, 
        /// 16 bit signed integer representing (adjustment * 512), giving +/- 64 dB with a precision of 0.001953125 dB.
        /// E.g. +2 dB is stored as 0x04 0x00 and -2 dB is 0xFC 0x00.
        /// </remarks>
        public short VolumeAdjustment { get; private set; }
    }
}
