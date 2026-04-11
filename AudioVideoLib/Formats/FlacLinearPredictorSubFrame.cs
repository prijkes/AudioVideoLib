/*
 * Date: 2013-03-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public sealed class FlacLinearPredictorSubFrame : FlacSubFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlacLinearPredictorSubFrame"/> class.
        /// </summary>
        /// <param name="flacFrame">The <see cref="FlacFrame"/>.</param>
        public FlacLinearPredictorSubFrame(FlacFrame flacFrame) : base(flacFrame)
        {
            if (flacFrame == null)
                throw new ArgumentNullException("flacFrame");
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the unencoded warm-up samples.
        /// </summary>
        /// <value>
        /// The unencoded warm-up samples.
        /// </value>
        public int[] UnencodedWarmUpSamples { get; private set; }

        /// <summary>
        /// Gets the quantized linear predictor coefficients' precision, in bits.
        /// </summary>
        /// <value>
        /// The quantized linear predictor coefficients' precision, in bits.
        /// </value>
        public int QuantizedCoefficientsPrecision { get; private set; }

        /// <summary>
        /// Gets the Quantized linear predictor coefficient shift needed, in bits.
        /// </summary>
        /// <value>
        /// The Quantized linear predictor coefficient shift needed, in bits.
        /// </value>
        public int QuantizedCoefficientShift { get; private set; }

        /// <summary>
        /// Gets the unencoded predictor coefficients.
        /// </summary>
        /// <value>
        /// The unencoded predictor coefficients.
        /// </value>
        public int[] UnencodedPredictorCoefficients { get; private set; }

        /// <summary>
        /// Gets the encoded residual.
        /// </summary>
        /// <value>
        /// The encoded residual.
        /// </value>
        public FlacResidual Residual { get; private set; }

        private int Order
        {
            get
            {
                return ((Header >> 1) & 0x1F) + 1;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the specified stream buffer.
        /// </summary>
        /// <param name="sb">The stream buffer.</param>
        /// <param name="sampeSize">Size of the sample.</param>
        /// <param name="blockSize">Size of the block.</param>
        protected override void Read(StreamBuffer sb, int sampeSize, int blockSize)
        {
            UnencodedWarmUpSamples = new int[Order];
            for (int i = 0; i < Order; i++)
                UnencodedWarmUpSamples[i] = sb.ReadBigEndianInt32();

            int other = sb.ReadBigEndianInt16();
            QuantizedCoefficientsPrecision = (other & 0xF) + 1;
            QuantizedCoefficientShift = (other >> 4) & 0x1F;

            UnencodedPredictorCoefficients = new int[Order];
            for (int i = 0; i < Order; i++)
                UnencodedPredictorCoefficients[i] = sb.ReadBigEndianInt32();

            Residual = FlacResidual.Read(sb, blockSize, Order);
        }
    }
}
