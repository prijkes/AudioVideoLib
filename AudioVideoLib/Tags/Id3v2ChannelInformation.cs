namespace AudioVideoLib.Tags;

/// <summary>
/// Channel information in an <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2ChannelInformation"/> class.
/// </remarks>
/// <param name="channelType">Type of the channel.</param>
/// <param name="volumeAdjustment">The volume adjustment.</param>
/// <param name="bitsRepresentingPeak">The bits representing peak.</param>
/// <param name="peakVolume">The peak volume.</param>
public class Id3v2ChannelInformation(Id3v2ChannelType channelType, float volumeAdjustment, byte bitsRepresentingPeak, long peakVolume)
{

    /// <summary>
    /// Gets the type of the channel.
    /// </summary>
    /// <value>
    /// The type of the channel.
    /// </value>
    public Id3v2ChannelType ChannelType { get; private set; } = channelType;

    /// <summary>
    /// Gets the volume adjustment.
    /// </summary>
    /// <remarks>
    /// The volume adjustment is encoded as a fixed point decibel value, 16 bit signed integer representing (adjustment * 512), 
    /// giving +/- 64 dB with a precision of 0.001953125 dB. E.g. +2 dB is stored as 0x04 0x00 and -2 dB is 0xFC 0x00.
    /// </remarks>
    public float VolumeAdjustment { get; private set; } = volumeAdjustment;

    /// <summary>
    /// Gets the bits representing peak.
    /// </summary>
    /// <remarks>
    /// Bits representing peak can be any number between 0 and 255. 0 means that there is no peak volume field.
    /// </remarks>
    public byte BitsRepresentingPeak { get; private set; } = bitsRepresentingPeak;

    /// <summary>
    /// Gets the peak volume.
    /// </summary>
    /// <remarks>
    /// The peak volume field is always padded to whole bytes, setting the most significant bits to zero.
    /// </remarks>
    public long PeakVolume { get; private set; } = peakVolume;
}
