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
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store information about an equalization band.
    /// </summary>
    public class Id3v2EqualisationBand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EqualisationBand"/> class.
        /// </summary>
        /// <param name="incrementDecrement">if set to <c>true</c> the band is increment; otherwise, the band is decrement.</param>
        /// <param name="frequency">The frequency.</param>
        /// <param name="adjustment">The adjustment.</param>
        public Id3v2EqualisationBand(bool incrementDecrement, short frequency, int adjustment)
        {
            if (adjustment == 0)
                throw new ArgumentOutOfRangeException("adjustment", "Adjustments with the value 0 should be omitted.");

            Increment = incrementDecrement;
            Frequency = frequency;
            Adjustment = adjustment;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether the band is either increment or decrement.
        /// </summary>
        /// <value>
        /// <c>true</c> if increment; otherwise, <c>false</c> for decrement.
        /// </value>
        /// <remarks>
        /// The increment/decrement bit is 1 for increment and 0 for decrement.
        /// </remarks>
        public bool Increment { get; private set; }

        /// <summary>
        /// Gets the frequency.
        /// </summary>
        /// <value>
        /// The frequency.
        /// </value>
        /// <remarks>
        /// Frequency has a range of 0 - 32767Hz.
        /// All frequencies don't have to be declared.
        /// A frequency should only be described once in the frame.
        /// </remarks>
        public short Frequency { get; private set; }

        /// <summary>
        /// Gets the adjustment.
        /// </summary>
        /// <value>
        /// The adjustment.
        /// </value>
        /// <remarks>
        /// Adjustments with the value 0x00 should be omitted.
        /// </remarks>
        public int Adjustment { get; private set; }
    }
}
