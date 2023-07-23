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
    /// Type of channels in an <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame.
    /// </summary>
    public enum Id3v2ChannelType
    {
        /// <summary>
        /// Other channel type.
        /// </summary>
        Other = 0x00,

        /// <summary>
        /// Master volume.
        /// </summary>
        MasterVolume = 0x01,

        /// <summary>
        /// Front right.
        /// </summary>
        FrontRight = 0x02,

        /// <summary>
        /// Front left.
        /// </summary>
        FrontLeft = 0x03,

        /// <summary>
        /// Back right.
        /// </summary>
        BackRight = 0x04,

        /// <summary>
        /// Back left.
        /// </summary>
        BackLeft = 0x05,

        /// <summary>
        /// Front center.
        /// </summary>
        FrontCenter = 0x06,

        /// <summary>
        /// Back center.
        /// </summary>
        BackCenter = 0x07,

        /// <summary>
        /// Subwoofer channel type.
        /// </summary>
        Subwoofer = 0x08
    }
}
