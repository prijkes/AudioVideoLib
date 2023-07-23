/*
 * Date: 2013-03-02
 * Author: NullFX
 * Sources used: 
 *  http://www.sanity-free.com/134/standard_crc_16_in_csharp.html
 */

using System;
using System.Linq;

namespace AudioVideoLib.Cryptography
{
    /// <summary>
    /// Calculates a 16-bit Cyclic Redundancy Checksum (CRC).
    /// </summary>
    public static class Crc16
    {
        private const int Polynomial = 0xA001;

        private static readonly int[] Crc16Table = new int[256];

        static Crc16()
        {
            for (int i = 0; i < Crc16Table.Length; ++i)
            {
                int value = 0;
                int temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                        value = (value >> 1) ^ Polynomial;
                    else
                        value >>= 1;

                    temp >>= 1;
                }
                Crc16Table[i] = value;
            }
        }

        /// <summary>
        /// Returns the CRC16 Checksum of a byte array.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>CRC16 Checksum.</returns>
        public static int Calculate(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            unchecked
            {
                int[] crc = { 0x00 };
                foreach (byte index in data.Select(t => (byte)(crc[0] ^ t)))
                    crc[0] = (crc[0] >> 8) ^ Crc16Table[index];

                return crc[0];
            }
        }
    }
}
