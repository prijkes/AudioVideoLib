namespace AudioVideoLib.Cryptography;

using System;

/// <summary>
/// Calculates a 16-bit Cyclic Redundancy Checksum (CRC) using polynomial
/// <c>0x8005</c>, MSB-first, init <c>0</c>, no reflection, no XOR-out.
/// </summary>
/// <remarks>
/// This is the CRC-16 used by FLAC frame footers (RFC 9639 §11.1). It is
/// distinct from the more common CRC-16/IBM-ARC (which is the same polynomial
/// reflected). Do not confuse with CRC-16/CCITT-FALSE.
/// </remarks>
public static class Crc16
{
    private const int Polynomial = 0x8005;

    private static readonly int[] Crc16Table = BuildTable();

    /// <summary>
    /// Returns the CRC-16 checksum of a byte span.
    /// </summary>
    /// <param name="data">The byte span.</param>
    /// <returns>CRC-16 checksum (low 16 bits of the returned int).</returns>
    public static int Calculate(ReadOnlySpan<byte> data)
    {
        var crc = 0;
        foreach (var b in data)
        {
            var index = ((crc >> 8) ^ b) & 0xFF;
            crc = ((crc << 8) ^ Crc16Table[index]) & 0xFFFF;
        }

        return crc;
    }

    private static int[] BuildTable()
    {
        var table = new int[256];
        for (var i = 0; i < 256; ++i)
        {
            var value = i << 8;
            for (var j = 0; j < 8; ++j)
            {
                value = ((value & 0x8000) != 0)
                    ? ((value << 1) ^ Polynomial) & 0xFFFF
                    : (value << 1) & 0xFFFF;
            }

            table[i] = value;
        }

        return table;
    }
}
