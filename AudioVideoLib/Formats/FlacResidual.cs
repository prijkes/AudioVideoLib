namespace AudioVideoLib.Formats;

using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Represents a FLAC residual block carried inside a Fixed or LPC subframe.
/// </summary>
public sealed class FlacResidual
{
    /// <summary>Gets the residual coding method (RFC 9639 §11.30, 2 bits).</summary>
    public FlacResidualCodingMethod CodingMethod { get; private set; }

    /// <summary>Gets the partition order (RFC 9639 §11.30, 4 bits).</summary>
    public int PartitionOrder { get; private set; }

    /// <summary>Gets the Rice partitions that constitute this residual block.</summary>
    public FlacRicePartition[] RicePartitions { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a residual block from the bit cursor.
    /// </summary>
    /// <param name="bs">The bit cursor.</param>
    /// <param name="blockSize">The frame block size (samples per subframe).</param>
    /// <param name="order">The predictor order of the enclosing subframe.</param>
    /// <returns>The parsed residual.</returns>
    /// <remarks>
    /// RFC 9639 §11.30: 2-bit coding method followed by a 4-bit partition order;
    /// then <c>2^partitionOrder</c> Rice partitions in stream order.
    /// </remarks>
    public static FlacResidual Read(BitStream bs, int blockSize, int order)
    {
        // RFC 9639 §11.30: 2-bit residual coding method (0=PartitionedRice, 1=PartitionedRice2, 2-3 reserved).
        var codingMethod = bs.ReadInt32(2);
        if (codingMethod >= 2)
        {
            throw new InvalidDataException($"Reserved residual coding method {codingMethod} (RFC 9639 §11.30).");
        }

        var residual = new FlacResidual
        {
            CodingMethod = (FlacResidualCodingMethod)codingMethod,

            // RFC 9639 §11.30: 4-bit partition order (number of partitions = 2^partitionOrder).
            PartitionOrder = bs.ReadInt32(4),
        };

        var partitions = 1 << residual.PartitionOrder;
        residual.RicePartitions = new FlacRicePartition[partitions];
        for (var i = 0; i < partitions; i++)
        {
            residual.RicePartitions[i] = FlacRicePartition.Read(bs, i, residual.PartitionOrder, order, blockSize, residual.CodingMethod);
        }

        return residual;
    }
}
