/*
 * Date: 2011-09-05
 * Author: judwhite
 * Sources used: 
 *  https://github.com/judwhite/IdSharp
 */

using System;
using System.IO;
using System.Linq;

namespace AudioVideoLib.Cryptography
{
    /// <summary>
    /// Calculates a 32-bit Cyclic Redundancy Checksum (CRC) using the
    /// same polynomial used by Zip.
    /// </summary>
    public static class Crc32
    {
        private const int BufferSize = 1024;

        // This is the official polynomial used by CRC32 in PKZip.
        // Often the polynomial is shown reversed as 0x04C11DB7.
        private const long Polynomial = 0xEDB88320;

        private static readonly long[] Crc32Table = new long[256];

        static Crc32()
        {
            unchecked
            {
                for (long i = 0; i < 256; i++)
                {
                    long crc = i;
                    for (long j = 8; j > 0; j--)
                    {
                        if ((crc & 0x01) == 0x01)
                            crc = (crc >> 0x01) ^ Polynomial;
                        else
                            crc >>= 0x01;
                    }
                    Crc32Table[i] = crc;
                }
            }
        }

        /// <summary>
        /// Returns the CRC32 Checksum of a byte array as a four byte 32-bit signed integer).
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// CRC32 Checksum as a four byte 32-bit signed integer.
        /// </returns>
        public static int Calculate(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            unchecked
            {
                long crc32Result = buffer.Aggregate<byte, long>(0xFFFFFFFF, (current, t) => (current >> 8) ^ Crc32Table[t ^ (current & 0x000000FF)]);
                return (int)~crc32Result;
            }
        }
    }
}
