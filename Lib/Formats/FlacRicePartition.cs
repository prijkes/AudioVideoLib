/*
 * Date: 2013-03-03
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Represents a Rice-coded residual partition.
    /// </summary>
    public class FlacRicePartition
    {
        private int _riceParameter;

        private FlacResidualCodingMethod _codingMethod;

        public int Samples { get; private set; }

        public int EncodingResidual { get; private set; }

        public int[] Residuals { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        public static FlacRicePartition Read(StreamBuffer sb, int partitionNumber, int partitionOrder, int predictOrder, int blockSize, FlacResidualCodingMethod codingMethod)
        {
            FlacRicePartition ricePartition = new FlacRicePartition { _riceParameter = sb.ReadBigEndianInt32(), _codingMethod = codingMethod };
            int riceParameter = ricePartition._riceParameter & ((codingMethod == FlacResidualCodingMethod.PartitionedRice) ? 0x1F : 0xF);
            if (partitionOrder == 0)
                ricePartition.Samples = blockSize - predictOrder;
            else if (partitionNumber == 0)
                ricePartition.Samples = (blockSize >> partitionOrder) - predictOrder;
            else
                ricePartition.Samples = blockSize >> partitionOrder;

            ricePartition.Residuals = new int[ricePartition.Samples];
            if ((riceParameter < 0xF) || ((codingMethod == FlacResidualCodingMethod.PartitionedRice2) && (riceParameter < 0x1F)))
            {
                for (int i = 0; i < ricePartition.Samples; i++)
                {
                    long msbs = sb.ReadUnaryInt();
                    long lsbs = sb.ReadBigEndianInt32() & (0xFFFFFFFF >> (32 - riceParameter));
                    long value = (msbs << riceParameter) | lsbs;
                    ricePartition.Residuals[i] = ((value & 0x01) == 0x01) ? -((int)(value >> 1)) - 1 : (int)(value >> 1);
                }
            }
            else
            {
                // residuals in unencoded form, sample size read from the next 5
                // bits in the stream.
                int size = sb.ReadBigEndianInt32();
                for (int i = 0; i < ricePartition.Samples; i++)
                    ricePartition.Residuals[i] = sb.ReadBigEndianInt32();
            }
            return ricePartition;
        }

        //public byte[] ToByteArrray()
        //{
        //    using (StreamBuffer sb = new StreamBuffer())
        //    {
        //        sb.WriteBigEndianInt32(_riceParameter);
        //        int riceParameter = _riceParameter & ((_codingMethod == FlacResidualCodingMethod.PartitionedRice) ? 0x1F : 0xF);
        //        if ((riceParameter < 0xF) || ((_codingMethod == FlacResidualCodingMethod.PartitionedRice2) && (riceParameter < 0x1F)))
        //        {
        //            for ()
        //        }
        //        else
        //        {
                    
        //        }
        //        return sb.ToByteArray();
        //    }
        //}
    }
}
