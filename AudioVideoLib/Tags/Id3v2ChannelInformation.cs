/*
 * Date: 2011-08-27
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
    /// Channel information in an <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame.
    /// </summary>
    public class Id3v2ChannelInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ChannelInformation"/> class.
        /// </summary>
        /// <param name="channelType">Type of the channel.</param>
        /// <param name="volumeAdjustment">The volume adjustment.</param>
        /// <param name="bitsRepresentingPeak">The bits representing peak.</param>
        /// <param name="peakVolume">The peak volume.</param>
        public Id3v2ChannelInformation(Id3v2ChannelType channelType, float volumeAdjustment, byte bitsRepresentingPeak, long peakVolume)
        {
            ChannelType = channelType;
            VolumeAdjustment = volumeAdjustment;
            BitsRepresentingPeak = bitsRepresentingPeak;
            PeakVolume = peakVolume;
        }

        /// <summary>
        /// Gets the type of the channel.
        /// </summary>
        /// <value>
        /// The type of the channel.
        /// </value>
        public Id3v2ChannelType ChannelType { get; private set; }

        /// <summary>
        /// Gets the volume adjustment.
        /// </summary>
        /// <remarks>
        /// The volume adjustment is encoded as a fixed point decibel value, 16 bit signed integer representing (adjustment * 512), 
        /// giving +/- 64 dB with a precision of 0.001953125 dB. E.g. +2 dB is stored as 0x04 0x00 and -2 dB is 0xFC 0x00.
        /// </remarks>
        public float VolumeAdjustment { get; private set; }

        /// <summary>
        /// Gets the bits representing peak.
        /// </summary>
        /// <remarks>
        /// Bits representing peak can be any number between 0 and 255. 0 means that there is no peak volume field.
        /// </remarks>
        public byte BitsRepresentingPeak { get; private set; }

        /// <summary>
        /// Gets the peak volume.
        /// </summary>
        /// <remarks>
        /// The peak volume field is always padded to whole bytes, setting the most significant bits to zero.
        /// </remarks>
        public long PeakVolume { get; private set; }
    }
}
