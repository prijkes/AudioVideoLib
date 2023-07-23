/*
 * Date: 2013-03-02
 * Author: NullFX
 * Sources used: 
 *  http://www.sanity-free.com/146/crc8_implementation_in_csharp.html
 */

using System;
using System.Linq;

namespace AudioVideoLib.Cryptography
{
    /// <summary>
    /// Calculates a 8-bit Cyclic Redundancy Checksum (CRC).
    /// </summary>
    public static class Crc8
    {
        private const byte Polynomial = 0xD5;

        private static readonly byte[] Crc8Table = new byte[256];

        static Crc8()
        {
            unchecked
            {
                for (int i = 0; i < 256; ++i)
                {
                    int crc = i;
                    for (int j = 0; j < 8; ++j)
                    {
                        if ((crc & 0x80) != 0)
                        {
                            crc = (crc << 1) ^ Polynomial;
                        }
                        else
                        {
                            crc <<= 1;
                        }
                    }
                    Crc8Table[i] = (byte)crc;
                }
            }
        }
        
        /// <summary>
        /// Returns the CRC8 Checksum of a byte array.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>CRC8 Checksum.</returns>
        public static byte Calculate(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            unchecked
            {
                return data.Aggregate<byte, byte>(0, (current, b) => Crc8Table[current ^ b]);
            }
        }
    }
}
