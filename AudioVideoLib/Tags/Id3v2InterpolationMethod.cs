/*
 * Date: 2011-11-05
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// The 'interpolation method' describes which method is preferred when an interpolation between the adjustment point that follows.
    /// </summary>
    public enum Id3v2InterpolationMethod
    {
        /// <summary>
        /// No interpolation is made. A jump from one adjustment level to another occurs in the middle between two adjustment points.
        /// </summary>
        Band = 0x00,

        /// <summary>
        /// Interpolation between adjustment points is linear.
        /// </summary>
        Linear = 0x01
    }
}
