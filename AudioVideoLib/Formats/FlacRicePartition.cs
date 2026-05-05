namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a Rice-coded residual partition.
/// </summary>
public sealed class FlacRicePartition
{
    private int _riceParameter;

    public int Samples { get; private set; }

    public int EncodingResidual { get; private set; }

    public int[] Residuals { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    public static FlacRicePartition Read(StreamBuffer sb, int partitionNumber, int partitionOrder, int predictOrder, int blockSize, FlacResidualCodingMethod codingMethod)
    {
        var ricePartition = new FlacRicePartition { _riceParameter = sb.ReadBigEndianInt32() };
        var riceParameter = ricePartition._riceParameter & (codingMethod == FlacResidualCodingMethod.PartitionedRice ? 0x1F : 0xF);
        ricePartition.Samples = partitionOrder == 0
            ? blockSize - predictOrder
            : partitionNumber == 0 ? (blockSize >> partitionOrder) - predictOrder : blockSize >> partitionOrder;

        ricePartition.Residuals = new int[ricePartition.Samples];
        if (riceParameter < 0xF || (codingMethod == FlacResidualCodingMethod.PartitionedRice2 && riceParameter < 0x1F))
        {
            for (var i = 0; i < ricePartition.Samples; i++)
            {
                long msbs = sb.ReadUnaryInt();
                var lsbs = sb.ReadBigEndianInt32() & (0xFFFFFFFF >> (32 - riceParameter));
                var value = (msbs << riceParameter) | lsbs;
                ricePartition.Residuals[i] = (value & 0x01) == 0x01 ? -(int)(value >> 1) - 1 : (int)(value >> 1);
            }
        }
        else
        {
            // residuals in unencoded form, sample size read from the next 5
            // bits in the stream.
            _ = sb.ReadBigEndianInt32();
            for (var i = 0; i < ricePartition.Samples; i++)
            {
                ricePartition.Residuals[i] = sb.ReadBigEndianInt32();
            }
        }
        return ricePartition;
    }
}
