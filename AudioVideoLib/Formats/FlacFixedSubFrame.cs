namespace AudioVideoLib.Formats;

using System;

/// <summary>
/// Represents a fixed-predictor subframe.
/// </summary>
public sealed class FlacFixedSubFrame : FlacSubFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlacFixedSubFrame"/> class.
    /// </summary>
    /// <param name="flacFrame">The flac frame.</param>
    public FlacFixedSubFrame(FlacFrame flacFrame) : base(flacFrame)
    {
        ArgumentNullException.ThrowIfNull(flacFrame);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded warm-up samples.
    /// </summary>
    /// <value>
    /// The unencoded warm-up samples.
    /// </value>
    public int[] UnencodedWarmUpSamples { get; private set; } = null!;

    /// <summary>
    /// Gets the residual.
    /// </summary>
    /// <value>
    /// The residual.
    /// </value>
    public FlacResidual Residual { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    //public override byte[] ToByteArray()
    //{
    //    using (StreamBuffer sb = new StreamBuffer())
    //    {
    //        sb.Write(base.ToByteArray());
    //        for (int i = 0; i < Order; i ++)
    //            sb.WriteBigEndianBytes(UnencodedWarmUpSamples[i], SampleSize / 8);

    //        sb.Write(Residual.ToByteArray());
    //        return sb.ToByteArray();
    //    }
    //}

    //protected override void Read(StreamBuffer sb)
    //{
    //    UnencodedWarmUpSamples = new int[Order];
    //    for (int i = 0; i < Order; i++)
    //        UnencodedWarmUpSamples[i] = sb.ReadInt(SampleSize * 8);

    //    Residual = FlacResidual.Read(sb, FlacFrame.BlockSize, Order);
    //}
}
