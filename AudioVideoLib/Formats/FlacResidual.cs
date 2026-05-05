namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

public sealed class FlacResidual
{
    private int _values;

    // RFC 9639 §11.30: 2-bit coding method (MSB) + 4-bit partition order (next),
    // packed into the high 6 bits of the leading byte. Bits 1..0 belong to the next field.
    public FlacResidualCodingMethod CodingMethod => (FlacResidualCodingMethod)((_values >> 6) & 0x03);

    public int PartitionOrder => (_values >> 2) & 0x0F;

    public FlacRicePartition[] RicePartitions { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    public static FlacResidual Read(StreamBuffer sb, int blockSize, int order)
    {
        var residual = new FlacResidual { _values = sb.ReadByte() };
        var partitions = 1 << residual.PartitionOrder;
        residual.RicePartitions = new FlacRicePartition[partitions];
        for (var i = 0; i < partitions; i++)
        {
            residual.RicePartitions[i] = FlacRicePartition.Read(sb, i, residual.PartitionOrder, order, blockSize, residual.CodingMethod);
        }

        return residual;
    }
}
