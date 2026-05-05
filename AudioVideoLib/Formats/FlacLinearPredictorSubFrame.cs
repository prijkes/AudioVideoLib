namespace AudioVideoLib.Formats;

using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Represents a linear-predictor (LPC) subframe.
/// </summary>
public sealed class FlacLinearPredictorSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded warm-up samples.
    /// </summary>
    /// <value>
    /// The unencoded warm-up samples.
    /// </value>
    public int[] UnencodedWarmUpSamples { get; private set; } = null!;

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
    public int[] UnencodedPredictorCoefficients { get; private set; } = null!;

    /// <summary>
    /// Gets the encoded residual.
    /// </summary>
    /// <value>
    /// The encoded residual.
    /// </value>
    public FlacResidual Residual { get; private set; } = null!;

    // RFC 9639 §11.25 + §11.29: subframe types 0b100000..0b111111 are LPC, with
    // predictor order encoded as (type bits 6..1 & 0x1F) + 1, range 1..32.
    private int Order => ((Header >> 1) & 0x1F) + 1; // RFC 9639 §11.29: LPC order = (subframe-type bits 6..1 & 0x1F) + 1

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>
    /// RFC 9639 §11.29: an LPC subframe is composed of:
    ///   - <c>order</c> warm-up samples of <c>sampleSize</c> bits each (signed);
    ///   - 4 bits of precision (stored value + 1 = real precision in bits);
    ///   - 5 bits of qlp coefficient shift (signed, two's-complement);
    ///   - <c>order</c> predictor coefficients of <c>precision</c> bits each (signed);
    ///   - the residual block.
    /// </remarks>
    protected override void Read(BitStream bs, int sampleSize, int blockSize)
    {
        var order = Order;
        UnencodedWarmUpSamples = new int[order];
        for (var i = 0; i < order; i++)
        {
            UnencodedWarmUpSamples[i] = bs.ReadSignedInt32(sampleSize);
        }

        // RFC 9639 §11.29: 4-bit precision; stored value + 1 = real precision.
        // The all-ones raw value 0b1111 is reserved — per spec, decoders MUST stop
        // decoding when they see it.
        var precisionRaw = bs.ReadInt32(4);
        if (precisionRaw == 0x0F)
        {
            throw new InvalidDataException("LPC predictor precision 0b1111 is reserved (RFC 9639 §11.29).");
        }

        QuantizedCoefficientsPrecision = precisionRaw + 1;

        // RFC 9639 §11.29: 5-bit signed (two's-complement) qlp shift.
        QuantizedCoefficientShift = bs.ReadSignedInt32(5);

        UnencodedPredictorCoefficients = new int[order];
        for (var i = 0; i < order; i++)
        {
            UnencodedPredictorCoefficients[i] = bs.ReadSignedInt32(QuantizedCoefficientsPrecision);
        }

        Residual = FlacResidual.Read(bs, blockSize, order);
    }
}
