/*
 * Date: 2012-11-17
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Linq;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an ID3v2 tag.
    /// </summary>
    /// <remarks>
    /// an <see cref="Id3v2Tag" /> contains a header, <see cref="Id3v2Frame" />s and optional <see cref="PaddingSize" />.
    /// </remarks>
    public sealed partial class Id3v2Tag
    {
        /// <summary>
        /// Gets the synchsafe value in LSB format.
        /// </summary>
        /// <param name="valueLsb">The value to synchsafe, in LSB format.</param>
        /// <returns>The value as a synchsafe value, in LSB format.</returns>
        public static short GetSynchsafeValue(short valueLsb)
        {
            int val = valueLsb;
            int res = 0, mask = 0x7F;
            while ((mask ^ 0x7FFF) != 0)
            {
                res = val & ~mask;
                res <<= 1;
                res |= val & mask;
                mask = ((mask + 1) << 8) - 1;
                val = res;
            }
            return (short)res;
        }

        /// <summary>
        /// Gets the synchsafe value in LSB format.
        /// </summary>
        /// <param name="valueLsb">The value to synchsafe, in LSB format.</param>
        /// <returns>The value as a synchsafe value, in LSB format.</returns>
        public static int GetSynchsafeValue(int valueLsb)
        {
            int res = 0, mask = 0x7F;
            while ((mask ^ 0x7FFFFFFF) != 0)
            {
                res = valueLsb & ~mask;
                res <<= 1;
                res |= valueLsb & mask;
                mask = ((mask + 1) << 8) - 1;
                valueLsb = res;
            }
            return res;
        }

        /// <summary>
        /// Gets the synchsafe value in LSB format.
        /// </summary>
        /// <param name="valueLsb">The value to synchsafe, in LSB format.</param>
        /// <returns>The value as a synchsafe value, in LSB format.</returns>
        public static long GetSynchsafeValue(long valueLsb)
        {
            long res = 0, mask = 0x7F;
            while ((mask ^ 0x7FFFFFFFFFFFFFFF) != 0)
            {
                res = valueLsb & ~mask;
                res <<= 1;
                res |= valueLsb & mask;
                mask = ((mask + 1) << 8) - 1;
                valueLsb = res;
            }
            return res;
        }

        /// <summary>
        /// Gets the unsynched value in LSB format.
        /// </summary>
        /// <param name="synchsafeDataLsb">The synchsafe data to unsynch, in LSB format.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The unsynched value, in LSB format.
        /// </returns>
        public static long GetUnsynchedValue(byte[] synchsafeDataLsb, int startIndex, int count)
        {
            if (synchsafeDataLsb == null)
                throw new ArgumentNullException("synchsafeDataLsb");

            if ((startIndex < 0) || (startIndex > synchsafeDataLsb.Length))
                throw new ArgumentOutOfRangeException("startIndex");

            if ((startIndex + count) > synchsafeDataLsb.Length)
                throw new ArgumentOutOfRangeException("count");

            long d = 0;
            for (int i = (startIndex + count - 1); i >= 0 ; i--)
            {
                d <<= 7;
                d |= (byte)(synchsafeDataLsb[i] & 0x7F);
            }
            return d;
        }

        /// <summary>
        /// Gets the unsynched data in LSB format.
        /// </summary>
        /// <param name="valueInLsb">The value in LSB.</param>
        /// <returns>
        /// The unsynched value, in LSB format.
        /// </returns>
        public static int GetUnsynchedValue(int valueInLsb)
        {
            return (int)GetUnsynchedValue(BitConverter.GetBytes(valueInLsb), 0, StreamBuffer.Int32Size);
        }

        /// <summary>
        /// Gets the unsynched data in LSB format.
        /// </summary>
        /// <param name="valueInLsb">The value in MSB.</param>
        /// <returns>
        /// The unsynched value, in LSB format.
        /// </returns>
        public static int GetUnsynchedValue(long valueInLsb)
        {
            return (int)GetUnsynchedValue(BitConverter.GetBytes(valueInLsb), 0, StreamBuffer.Int64Size);
        }

        /// <summary>
        /// Gets the unsynched data in LSB format.
        /// </summary>
        /// <param name="valueInLsb">The value in LSB.</param>
        /// <param name="bytes">The amount of bytes to unsynch.</param>
        /// <returns>
        /// The unsynched value, in LSB format.
        /// </returns>
        public static long GetUnsynchedValue(long valueInLsb, int bytes)
        {
            bytes = Math.Min(bytes, StreamBuffer.Int64Size);
            return GetUnsynchedValue(BitConverter.GetBytes(valueInLsb), 0, bytes);
        }

        /// <summary>
        /// Gets the synchronized data.
        /// </summary>
        /// <param name="data">The unsynchronized data to synchronize.</param>
        /// <param name="startIndex">The start index in the data.</param>
        /// <param name="dataLength">Length of the data.</param>
        /// <returns>
        /// The synchronized data.
        /// </returns>
        /// <remarks>
        /// This is not the same as unsynching a safe synched value; 
        /// use <see cref="GetUnsynchedValue(int)"/> for unsynching a safe synched value.
        /// </remarks>
        public static byte[] GetSynchronizedData(byte[] data, long startIndex, long dataLength)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            int bufferIndex = 0;
            byte[] buffer = new byte[dataLength];
            for (long dataIndex = startIndex; dataIndex < dataLength; dataIndex++)
            {
                if ((dataIndex + 1) < dataLength)
                {
                    if ((data[dataIndex] == 0xFF) && (data[dataIndex + 1] == 0x00))
                    {
                        buffer[bufferIndex++] = data[dataIndex++];
                        continue;
                    }
                }
                buffer[bufferIndex++] = data[dataIndex];
            }
            return buffer.Take(bufferIndex).ToArray();
        }

        /// <summary>
        /// Gets the unsynchronized data.
        /// </summary>
        /// <param name="data">The data to unsynchronize.</param>
        /// <param name="startIndex">The start index in the data.</param>
        /// <param name="dataLength">Length of the data.</param>
        /// <returns>The unsynchronized data.</returns>
        /// <remarks>
        /// This is not the same as safe synching a value; use <see cref="GetSynchsafeValue(long)"/> for safe synching a value.
        /// </remarks>
        public static byte[] GetUnsynchronizedData(byte[] data, long startIndex, long dataLength)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            int bufferIndex = 0;
            byte[] buffer = new byte[dataLength * 3];
            for (long index = startIndex; index < dataLength; index++)
            {
                buffer[bufferIndex++] = data[index];
                if ((data[index] == 0xFF) && (((index + 1) == dataLength) || ((data[index + 1] >= 0x07) || (data[index + 1] == 0x00))))
                    buffer[bufferIndex++] = 0x00;
            }
            return buffer.Take(bufferIndex).ToArray();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidVersion(Id3v2Version version)
        {
            return Enum.TryParse(version.ToString(), true, out version);
        }
    }
}
