/*
 * Date: 2013-02-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System.Linq;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    public class FlacResidual
    {
        private int _values;

        public FlacResidualCodingMethod CodingMethod
        {
            get
            {
                return (FlacResidualCodingMethod)(_values & 0x03);
            }
        }

        public int PartitionOrder
        {
            get
            {
                return ((_values >> 4) & 0x0F);
            }
        }

        public FlacRicePartition[] RicePartitions { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        public static FlacResidual Read(StreamBuffer sb, int blockSize, int order)
        {
            FlacResidual residual = new FlacResidual { _values = sb.ReadByte() };
            int partitions = 1 << residual.PartitionOrder;
            residual.RicePartitions = new FlacRicePartition[partitions];
            for (int i = 0; i < partitions; i++)
                residual.RicePartitions[i] = FlacRicePartition.Read(sb, i, residual.PartitionOrder, order, blockSize, residual.CodingMethod);

            return residual;
        }

        ////public byte[] ToByteArray()
        ////{
        ////    using (StreamBuffer sb = new StreamBuffer())
        ////    {
        ////        sb.WriteByte((byte)_values);
        ////        foreach (byte[] data in RicePartitions.Select(r => r.ToByteArrray()))
        ////            sb.Write(data);

        ////        return sb.ToByteArray();
        ////    }
        ////}
    }
}
