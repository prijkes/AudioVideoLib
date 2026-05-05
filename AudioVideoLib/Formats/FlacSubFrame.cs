namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio sub frames.
/// </summary>
public partial class FlacSubFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlacSubFrame"/> class.
    /// </summary>
    /// <param name="flacFrame">The FLAC frame.</param>
    protected FlacSubFrame(FlacFrame flacFrame)
    {
        ArgumentNullException.ThrowIfNull(flacFrame);
        FlacFrame = flacFrame;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the FLAC frame.
    /// </summary>
    /// <value>
    /// The FLAC frame.
    /// </value>
    public FlacFrame FlacFrame { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a <see cref="FlacSubFrame" /> from a bit-cursor over the source stream.
    /// </summary>
    /// <param name="bs">The bit cursor positioned at the subframe header.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="flacFrame">The FLAC frame.</param>
    /// <returns>
    /// The parsed sub-frame, or <c>null</c> if the subframe header carries a reserved
    /// type per RFC 9639 §11.25 (spec §7 strict-rejection).
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="bs"/> is null.</exception>
    public static FlacSubFrame? ReadFrame(BitStream bs, int channel, FlacFrame flacFrame)
    {
        ArgumentNullException.ThrowIfNull(bs);
        ArgumentNullException.ThrowIfNull(flacFrame);
        return ReadSubFrame(bs, channel, flacFrame);
    }

    /// <summary>
    /// Reads the subframe payload from the bit-cursor.
    /// </summary>
    /// <param name="bs">The bit cursor.</param>
    /// <param name="sampleSize">Size of the sample, in bits.</param>
    /// <param name="blockSize">Size of the block (samples per subframe).</param>
    protected virtual void Read(BitStream bs, int sampleSize, int blockSize)
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    // RFC 9639 §11.25: subframe types 0x02-0x07 and 0x0D-0x1F are reserved.
    // Per spec §7 strict-rejection rule, decoders MUST reject reserved types
    // outright rather than relying on a downstream CRC check (~1-in-65k collision risk).
    private static bool IsReservedType(int type) =>
        type is (>= 0x02 and <= 0x07) or (>= 0x0D and <= 0x1F);

    private static FlacSubFrame? ReadSubFrame(BitStream bs, int channel, FlacFrame flacFrame)
    {
        ArgumentNullException.ThrowIfNull(bs);
        ArgumentNullException.ThrowIfNull(flacFrame);

        // Subframe header byte (RFC 9639 §11.25): 1 zero-pad bit + 6-bit type + 1-bit wasted-bits flag.
        // Read the byte once (bit-aligned), classify the type, and dispatch to the
        // correct concrete subframe. The dispatched instance re-uses the captured
        // header byte via FlacFrame-scoped state set in ReadHeader.
        var headerByte = bs.ReadInt32(8);
        var type = (headerByte >> 1) & 0x3F;

        if (IsReservedType(type))
        {
            return null;
        }

        FlacSubFrame frame = type switch
        {
            0x00 => new FlacConstantSubFrame(flacFrame),
            0x01 => new FlacVerbatimSubFrame(flacFrame),
            >= 0x08 and <= 0x0C => new FlacFixedSubFrame(flacFrame),
            _ => new FlacLinearPredictorSubFrame(flacFrame), // type >= 0x20
        };
        frame.ReadSubFrame(bs, channel, headerByte);
        return frame;
    }

    private void ReadSubFrame(BitStream bs, int channel, int headerByte)
    {
        ArgumentNullException.ThrowIfNull(bs);

        var sampleSize = FlacFrame.SampleSize;
        if (((FlacFrame.ChannelAssignment is FlacChannelAssignment.LeftSide or FlacChannelAssignment.MidSide) && channel == 1) || (FlacFrame.ChannelAssignment == FlacChannelAssignment.RightSide && channel == 0))
        {
            sampleSize++;
        }

        ReadHeader(bs, headerByte);
        SampleSize = sampleSize - WastedBits;
        Read(bs, SampleSize, FlacFrame.BlockSize);
    }
}
