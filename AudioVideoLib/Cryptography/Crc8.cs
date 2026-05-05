namespace AudioVideoLib.Cryptography;

using System;

/// <summary>
/// Calculates a 8-bit Cyclic Redundancy Checksum (CRC).
/// </summary>
public static class Crc8
{
    private const byte Polynomial = 0x07;

    private static readonly byte[] Crc8Table = new byte[256];

    static Crc8()
    {
        unchecked
        {
            for (var i = 0; i < 256; ++i)
            {
                var crc = i;
                for (var j = 0; j < 8; ++j)
                {
                    crc = ((crc & 0x80) != 0) ? (crc << 1) ^ Polynomial : crc << 1;
                }
                Crc8Table[i] = (byte)crc;
            }
        }
    }

    /// <summary>
    /// Returns the CRC8 Checksum of a byte span.
    /// </summary>
    /// <param name="data">The byte span.</param>
    /// <returns>CRC8 Checksum.</returns>
    public static byte Calculate(ReadOnlySpan<byte> data)
    {
        unchecked
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc = Crc8Table[crc ^ b];
            }
            return crc;
        }
    }
}
