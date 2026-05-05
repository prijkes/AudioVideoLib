namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public sealed class FlacStreamInfoMetadataBlock : FlacMetadataBlock
{
    private long _samplesChannelRate;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override FlacMetadataBlockType BlockType => FlacMetadataBlockType.StreamInfo;

    /// <inheritdoc/>
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            stream.WriteBigEndianInt16((short)MinimumBlockSize);
            stream.WriteBigEndianInt16((short)MaximumBlockSize);
            stream.WriteBigEndianBytes(MinimumFrameSize, 3);
            stream.WriteBigEndianBytes(MaximumFrameSize, 3);
            stream.WriteBigEndianInt64(_samplesChannelRate);
            stream.Write(MD5);
            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            MinimumBlockSize = stream.ReadBigEndianInt16();
            MaximumBlockSize = stream.ReadBigEndianInt16();
            MinimumFrameSize = stream.ReadBigEndianInt(3);
            MaximumFrameSize = stream.ReadBigEndianInt(3);
            _samplesChannelRate = stream.ReadBigEndianInt64();
            MD5 = new byte[16];
            stream.Read(MD5, MD5.Length);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the minimum block size (in samples) used in the stream.
    /// </summary>
    /// <value>
    /// The minimum block size (in samples) used in the stream.
    /// </value>
    public int MinimumBlockSize { get; private set; }

    /// <summary>
    /// Gets the maximum block size (in samples) used in the stream.
    /// </summary>
    /// <value>
    /// The maximum block size (in samples) used in the stream.
    /// </value>
    /// <remarks>
    /// (Minimum blocksize == maximum blocksize) implies a fixed-blocksize stream.
    /// </remarks>
    public int MaximumBlockSize { get; private set; }

    /// <summary>
    /// Gets the minimum frame size (in bytes) used in the stream. May be 0 to imply the value is not known.
    /// </summary>
    /// <value>
    /// The minimum frame size (in bytes) used in the stream. May be 0 to imply the value is not known.
    /// </value>
    public int MinimumFrameSize { get; private set; }

    /// <summary>
    /// Gets the maximum frame size (in bytes) used in the stream. May be 0 to imply the value is not known.
    /// </summary>
    /// <value>
    /// The maximum frame size (in bytes) used in the stream. May be 0 to imply the value is not known.
    /// </value>
    public int MaximumFrameSize { get; private set; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    /// <value>
    /// The sample rate in Hz.
    /// </value>
    /// <remarks>
    /// The maximum sample rate is limited by the structure of frame headers to 655350Hz.
    /// Also, a value of 0 is invalid.
    /// </remarks>
    // Per RFC 9639 §8.2: 20-bit sample-rate field at bits 44..63 of the 64-bit word.
    public int SampleRate => (int)((_samplesChannelRate >> 44) & 0xFFFFF);

    /// <summary>
    /// Gets the number of channels. FLAC supports from 1 to 8 channels.
    /// </summary>
    /// <value>
    /// The the number of channels. FLAC supports from 1 to 8 channels.
    /// </value>
    // Per RFC 9639 §8.2: 3-bit channels-1 field at bits 41..43 of the 64-bit word.
    public int Channels => (int)(((_samplesChannelRate >> 41) & 0x07) + 1);

    /// <summary>
    /// Gets the bits per sample.
    /// </summary>
    /// <value>
    /// The bits per sample.
    /// </value>
    // Per RFC 9639 §8.2: 5-bit (bits-per-sample - 1) field at bits 36..40 of the 64-bit word.
    public int BitsPerSample => (int)((_samplesChannelRate >> 36) & 0x1F) + 1;

    /// <summary>
    /// Gets the total samples in the stream.
    /// </summary>
    /// <value>
    /// The total samples in the stream.
    /// </value>
    /// <remarks>
    /// 'Samples' means inter-channel sample, i.e. one second of 44.1Khz audio will have 44100 samples regardless of the number of channels.
    /// A value of zero here means the number of total samples is unknown.
    /// </remarks>
    public long TotalSamples => _samplesChannelRate & 0xFFFFFFFFF;

    /// <summary>
    /// Gets the MD5 signature of the unencoded audio data.
    /// </summary>
    /// <value>
    /// The MD5 signature of the unencoded audio data.
    /// </value>
    /// <remarks>
    /// The MD5 signature allows the decoder to determine if an error exists in the audio data even when the error does not result in an invalid bitstream.
    /// </remarks>
    public byte[] MD5 { get; private set; } = null!;
}
