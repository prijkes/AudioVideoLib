namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Class used to store an <see cref="ApeTag"/> item.
/// </summary>
public partial class ApeItem
{
    /// <summary>
    /// Determines whether the key is a valid item key.
    /// </summary>
    /// <param name="itemKey">The item key.</param>
    /// <returns>
    /// <c>true</c> if they key is a valid item key; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// An item key must be at least 2 chars and not more than 256 chars.
    /// The key may not be one of the following: ID3, TAG, OggS or MP+
    /// </remarks>
    /// According to the specs, no char may be below a space or beyond a ~; but UTF8 encoded key's have been found in the wild, thus UTF8 should be allowed.
    public static bool IsValidItemKey(string itemKey)
    {
        ArgumentNullException.ThrowIfNull(itemKey);

        string[] invalidItemKeys = ["ID3", "TAG", "OggS", "MP+"];

        // An item key must be at least 2 chars and not more than 255 bytes.
        // The key may also not be in the list of not allowed keys.
        return (itemKey.Length >= MinKeyLengthCharacters) && (Encoding.UTF8.GetByteCount(itemKey) <= MaxKeyLengthBytes)
               && !invalidItemKeys.Contains(itemKey);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static ApeItemType GetItemType(int flags)
    {
        return (ApeItemType)((flags & HeaderFlags.ItemType) >> 1);
    }

    private static bool IsValidItemType(ApeItemType itemType)
    {
        return Enum.TryParse(itemType.ToString(), true, out itemType) && Enum.IsDefined(typeof(ApeItemType), itemType);
    }

    private static bool AreValidFlags(int flags)
    {
        var itemType = GetItemType(flags);

        // Flags - Bit 28...3: Undefined, must be zero
        return ((flags & 0x1FFFFFF8) == 0) && IsValidItemType(itemType);
    }

    /*
     * Code Points                  First Byte      Second Byte   Third Byte    Fourth Byte
     * U+0000..U+007F          00..7F
     * U+0080..U+07FF          C2..DF         80..BF
     * U+0800..U+0FFF          E0                A0..BF             80..BF
     * U+1000..U+CFFF          E1..EC          80..BF             80..BF
     * U+D000..U+D7FF         ED                80..9F            80..BF
     * U+E000..U+FFFF           EE..EF           80..BF            80..BF
     * U+10000..U+3FFFF       F0                90..BF             80..BF         80..BF
     * U+40000..U+FFFFF       F1..F3          80..BF             80..BF          80..BF
     * U+100000..U+10FFFF   F4               80..8F             80..BF          80..BF
    */
    private static bool IsValidUtf8(IList<byte> utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);

        for (var i = 0; i < utf8Bytes.Count; i++)
        {
            // Code Points                  First Byte
            // U+0000..U+007F          00..7F
            var firstByte = utf8Bytes[i];
            if (firstByte <= 0x7F)
            {
                continue;
            }

            if (++i == utf8Bytes.Count)
            {
                return false;
            }

            // Code Points                  First Byte      Second Byte
            // U+0080..U+07FF          C2..DF         80..BF
            var secondByte = utf8Bytes[i];
            if (firstByte is >= 0xC2 and <= 0xDf)
            {
                if (secondByte is >= 0x80 and <= 0xBF)
                {
                    continue;
                }

                return false;
            }

            if (++i == utf8Bytes.Count)
            {
                return false;
            }

            // Code Points                  First Byte      Second Byte   Third Byte
            // U+0800..U+0FFF          E0                A0..BF             80..BF
            // U+1000..U+CFFF          E1..EC          80..BF             80..BF
            // U+D000..U+D7FF         ED                80..9F            80..BF
            // U+E000..U+FFFF           EE..EF           80..BF            80..BF
            var thirdByte = utf8Bytes[i];
            if (thirdByte is < 0x80 or > 0xBF)
            {
                return false;
            }

            if (firstByte == 0xE0)
            {
                if (secondByte is >= 0xA0 and <= 0xBF)
                {
                    continue;
                }

                return false;
            }

            if (firstByte is (>= 0xE1 and <= 0xEC) or (>= 0xEE and <= 0xEF))
            {
                if (secondByte is >= 0x80 and <= 0xBF)
                {
                    continue;
                }

                return false;
            }

            if (firstByte == 0xED)
            {
                if (secondByte is >= 0x80 and <= 0x9F)
                {
                    continue;
                }

                return false;
            }

            if (++i == utf8Bytes.Count)
            {
                return false;
            }

            // Code Points                  First Byte      Second Byte   Third Byte    Fourth Byte
            // U+10000..U+3FFFF       F0                90..BF             80..BF          80..BF
            // U+40000..U+FFFFF       F1..F3          80..BF             80..BF          80..BF
            // U+100000..U+10FFFF   F4                80..8F             80..BF          80..BF
            var fourthByte = utf8Bytes[i];
            if (fourthByte is < 0x80 or > 0xBF)
            {
                return false;
            }

            if ((firstByte == 0xF0) && (secondByte >= 0x90) && (secondByte <= 0xBF))
            {
                continue;
            }

            if ((firstByte >= 0xF1) && (firstByte <= 0xF3) && (secondByte >= 0x80) && (secondByte <= 0xBF))
            {
                continue;
            }

            if ((firstByte == 0xF4) && (secondByte >= 0x80) && (secondByte <= 0x8F))
            {
                continue;
            }

            return false;
        }
        return true;
    }
}
