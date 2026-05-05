namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a Rice-coded residual partition.
/// </summary>
public sealed class FlacRicePartition
{
    /// <summary>Gets the Rice parameter (RFC 9639 §11.30).</summary>
    public int RiceParameter { get; private set; }

    /// <summary>Gets the number of residual samples in this partition.</summary>
    public int Samples { get; private set; }

    /// <summary>
    /// Gets the per-sample bit width when this partition is in escape mode
    /// (Rice parameter == all-ones), or 0 otherwise.
    /// </summary>
    public int EncodingResidual { get; private set; }

    /// <summary>Gets the decoded residual samples.</summary>
    public int[] Residuals { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a Rice partition from the bit cursor.
    /// </summary>
    /// <param name="bs">The bit cursor.</param>
    /// <param name="partitionNumber">The 0-based partition index within the residual.</param>
    /// <param name="partitionOrder">The residual partition order (number of partitions = 2^partitionOrder).</param>
    /// <param name="predictOrder">The predictor order of the enclosing subframe.</param>
    /// <param name="blockSize">The frame block size (samples per subframe).</param>
    /// <param name="codingMethod">The residual coding method (selects 4- vs 5-bit parameter).</param>
    /// <returns>The parsed Rice partition.</returns>
    public static FlacRicePartition Read(BitStream bs, int partitionNumber, int partitionOrder, int predictOrder, int blockSize, FlacResidualCodingMethod codingMethod)
    {
        // RFC 9639 §11.30: PartitionedRice = 4-bit Rice parameter; PartitionedRice2 = 5-bit Rice parameter.
        var parameterWidth = codingMethod == FlacResidualCodingMethod.PartitionedRice ? 4 : 5;
        var escapeCode = codingMethod == FlacResidualCodingMethod.PartitionedRice ? 0xF : 0x1F;

        // RFC 9639 §11.30: per-partition sample count.
        //   partitionOrder == 0:                 blockSize - predictorOrder
        //   first partition (partitionNumber == 0): (blockSize >> partitionOrder) - predictorOrder
        //   otherwise:                            blockSize >> partitionOrder
        var samples = partitionOrder == 0
            ? blockSize - predictOrder
            : partitionNumber == 0 ? (blockSize >> partitionOrder) - predictOrder : blockSize >> partitionOrder;

        var partition = new FlacRicePartition
        {
            RiceParameter = bs.ReadInt32(parameterWidth),
            Samples = samples,
        };

        partition.Residuals = new int[partition.Samples];

        if (partition.RiceParameter < escapeCode)
        {
            // Standard Rice-coded samples: unary MSBs (count of leading 0-bits before
            // the terminating 1) followed by `parameter` bits of LSBs. The combined
            // magnitude is then folded to a signed value via zigzag decoding —
            // even values map to non-negative, odd to negative.
            for (var i = 0; i < partition.Samples; i++)
            {
                long msbs = bs.ReadUnaryInt();
                var lsbs = (uint)bs.ReadInt32(partition.RiceParameter);
                var value = (msbs << partition.RiceParameter) | lsbs;
                partition.Residuals[i] = (value & 0x01) == 0x01 ? -(int)(value >> 1) - 1 : (int)(value >> 1);
            }
        }
        else
        {
            // Escape mode: residuals are stored raw. RFC 9639 §11.30 specifies the
            // next 5 bits as the per-sample bit width (signed two's-complement).
            partition.EncodingResidual = bs.ReadSignedInt32(5);
            for (var i = 0; i < partition.Samples; i++)
            {
                partition.Residuals[i] = bs.ReadSignedInt32(partition.EncodingResidual);
            }
        }

        return partition;
    }
}
