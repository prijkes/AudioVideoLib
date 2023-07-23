/*
 * Date: 2010-05-19
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.id3.org/Id3v1
 *  http://en.wikipedia.org/wiki/ID3#Extended_tag
 *  http://lib313.sourceforge.net/id3v13.html
 */

using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v1 tag.
    /// </summary>
    public sealed partial class Id3v1Tag
    {
        /// <summary>
        /// Determines whether the specified version is valid.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        /// <c>true</c> if the specified <param name="version">version</param> is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidVersion(Id3v1Version version)
        {
            return Enum.TryParse(version.ToString(), true, out version);
        }

        /// <summary>
        /// Determines whether the specified genre is valid.
        /// </summary>
        /// <param name="genre">The genre.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="genre">genre</paramref> is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidGenre(Id3v1Genre genre)
        {
            return Enum.TryParse(genre.ToString(), true, out genre);
        }

        /// <summary>
        /// Determines whether the specified track is valid.
        /// </summary>
        /// <param name="trackSpeed">The track speed.</param>
        /// <returns></returns>
        public static bool IsValidTrackSpeed(Id3v1TrackSpeed trackSpeed)
        {
            return Enum.TryParse(trackSpeed.ToString(), true, out trackSpeed);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private string GetTruncatedEncodedString(string value, int maxBytesAllowed)
        {
            return StreamBuffer.GetTruncatedEncodedString(value, _encoding, maxBytesAllowed);
        }

        private string GetExtendedString(string value, int maxLengthNormal, int maxLengthExtended, bool onlyLastPart = false)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            string firstPart = GetTruncatedEncodedString(value, maxLengthNormal);
            if (!UseExtendedTag || (firstPart.Length == value.Length))
                return onlyLastPart ? String.Empty : firstPart;

            string secondPart = GetTruncatedEncodedString(value.Substring(firstPart.Length), maxLengthExtended);
            return onlyLastPart ? secondPart : firstPart + secondPart;
        }
    }
}
