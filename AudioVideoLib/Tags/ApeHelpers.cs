/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */
using System;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public partial class ApeTag
    {
        /// <summary>
        /// Determines whether the value is a valid ISBN-10 number.
        /// </summary>
        /// <param name="isbn10">The ISBN-10.</param>
        /// <returns>
        ///   <c>true</c> if the value is valid a ISBN-10 number; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidIsbn10(string isbn10)
        {
            if (isbn10 == null)
                throw new ArgumentNullException("isbn10");

            // 3-341-00488-X
            if (isbn10.Length == 13)
                isbn10 = isbn10.Replace("-", String.Empty);

            // 3341004807
            if (isbn10.Length != 10)
                return false;

            int checksum = isbn10.Last();
            int i, position, sum = 0;
            for (i = 0, position = 1; i < isbn10.Length - 1; i++, position++)
                sum += isbn10[i] * position;

            return (sum % 11) == checksum;
        }
    }
}
