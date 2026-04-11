/*
 * Date: 2011-10-22
 * Sources used:
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Enumerator to indicate the APE tag version.
    /// </summary>
    public enum ApeVersion
    {
        /// <summary>
        /// Indicates version 1.
        /// </summary>
        Version1 = 1,

        /// <summary>
        /// Indicates version 2.
        /// </summary>
        Version2 = 2,
    }
}
