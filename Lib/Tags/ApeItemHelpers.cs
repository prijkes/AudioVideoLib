/*
 * Date: 2013-12-07
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 *  http://stackoverflow.com/questions/6555015/check-for-invalid-utf8
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioVideoLib.Tags
{
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
            if (itemKey == null)
                throw new ArgumentNullException("itemKey");

            string[] invalidItemKeys = { "ID3", "TAG", "OggS", "MP+" };

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
            ApeItemType itemType = GetItemType(flags);

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
            if (utf8Bytes == null)
                throw new ArgumentNullException("utf8Bytes");

            for (int i = 0; i < utf8Bytes.Count; i++)
            {
                // Code Points                  First Byte
                // U+0000..U+007F          00..7F
                byte firstByte = utf8Bytes[i];
                if (firstByte <= 0x7F)
                    continue;

                if (++i == utf8Bytes.Count)
                    return false;

                // Code Points                  First Byte      Second Byte
                // U+0080..U+07FF          C2..DF         80..BF
                byte secondByte = utf8Bytes[i];
                if ((firstByte >= 0xC2) && (firstByte <= 0xDf))
                {
                    if ((secondByte >= 0x80) && (secondByte <= 0xBF))
                        continue;

                   return false;
                }

                if (++i == utf8Bytes.Count)
                    return false;

                // Code Points                  First Byte      Second Byte   Third Byte
                // U+0800..U+0FFF          E0                A0..BF             80..BF
                // U+1000..U+CFFF          E1..EC          80..BF             80..BF
                // U+D000..U+D7FF         ED                80..9F            80..BF
                // U+E000..U+FFFF           EE..EF           80..BF            80..BF
                byte thirdByte = utf8Bytes[i];
                if ((thirdByte < 0x80) || (thirdByte > 0xBF))
                    return false;

                if (firstByte == 0xE0)
                {
                    if ((secondByte >= 0xA0) && (secondByte <= 0xBF))
                        continue;

                    return false;
                }

                if (((firstByte >= 0xE1) && (firstByte <= 0xEC)) || ((firstByte >= 0xEE) && (firstByte <= 0xEF)))
                {
                    if ((secondByte >= 0x80) && (secondByte <= 0xBF))
                        continue;

                    return false;
                }

                if (firstByte == 0xED)
                {
                    if ((secondByte >= 0x80) && (secondByte <= 0x9F))
                        continue;

                    return false;
                }

                if (++i == utf8Bytes.Count)
                    return false;

                // Code Points                  First Byte      Second Byte   Third Byte    Fourth Byte
                // U+10000..U+3FFFF       F0                90..BF             80..BF          80..BF
                // U+40000..U+FFFFF       F1..F3          80..BF             80..BF          80..BF
                // U+100000..U+10FFFF   F4                80..8F             80..BF          80..BF
                byte fourthByte = utf8Bytes[i];
                if ((fourthByte < 0x80) || (fourthByte > 0xBF))
                    return false;

                if ((firstByte == 0xF0) && (secondByte >= 0x90) && (secondByte <= 0xBF))
                    continue;

                if ((firstByte >= 0xF1) && (firstByte <= 0xF3) && (secondByte >= 0x80) && (secondByte <= 0xBF))
                    continue;

                if ((firstByte == 0xF4) && (secondByte >= 0x80) && (secondByte <= 0x8F))
                    continue;

                return false;
            }
            return true;
        }
    }
}
