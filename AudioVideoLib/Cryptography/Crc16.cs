namespace AudioVideoLib.Cryptography;

using System;

/// <summary>
/// Calculates a 16-bit Cyclic Redundancy Checksum (CRC).
/// </summary>
public static class Crc16
{
    private const int Polynomial = 0xA001;

    private static readonly int[] Crc16Table = new int[256];

    static Crc16()
    {
        for (var i = 0; i < Crc16Table.Length; ++i)
        {
            var value = 0;
            var temp = i;
            for (byte j = 0; j < 8; ++j)
            {
                if (((value ^ temp) & 0x0001) != 0)
                {
                    value = (value >> 1) ^ Polynomial;
                }
                else
                {
                    value >>= 1;
                }

                temp >>= 1;
            }
            Crc16Table[i] = value;
        }
    }

    /// <summary>
    /// Returns the CRC16 Checksum of a byte span.
    /// </summary>
    /// <param name="data">The byte span.</param>
    /// <returns>CRC16 Checksum.</returns>
    public static int Calculate(ReadOnlySpan<byte> data)
    {
        unchecked
        {
            var crc = 0;
            foreach (var b in data)
            {
                var index = (byte)(crc ^ b);
                crc = (crc >> 8) ^ Crc16Table[index];
            }

            return crc;
        }
    }
}
