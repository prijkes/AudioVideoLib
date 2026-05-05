namespace AudioVideoLib.Formats;

using System;
using System.IO;

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
    /// Reads a <see cref="FlacSubFrame" /> from a <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="flacFrame">The FLAC frame.</param>
    /// <returns>
    /// true if found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static FlacSubFrame ReadFrame(Stream stream, int channel, FlacFrame flacFrame)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(flacFrame);
        return ReadSubFrame(stream as StreamBuffer ?? new StreamBuffer(stream), channel, flacFrame);
    }

    /// <summary>
    /// Reads the specified stream buffer.
    /// </summary>
    /// <param name="sb">The stream buffer.</param>
    /// <param name="sampeSize">Size of the sample.</param>
    /// <param name="blockSize">Size of the block.</param>
    protected virtual void Read(StreamBuffer sb, int sampeSize, int blockSize)
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static FlacSubFrame ReadSubFrame(StreamBuffer sb, int channel, FlacFrame flacFrame)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(flacFrame);

        // Subframe header byte (RFC 9639 §11.25): 1 zero-pad bit + 6-bit type + 1-bit wasted-bits flag.
        // Peek the first byte (not 4) and extract bits 1..6.
        var headerByte = sb.PeekByte();
        var type = (headerByte >> 1) & 0x3F;

        // Reserved subframe types (RFC 9639 §11.25): 0x02-0x07, 0x0D-0x1F. We surface
        // them as a plain FlacSubFrame whose Read is a no-op; the frame-level CRC-16
        // check then rejects the frame per spec §7 strict-rejection.
        var frame = type switch
        {
            0x00 => new FlacConstantSubFrame(flacFrame),
            0x01 => new FlacVerbatimSubFrame(flacFrame),
            _ => type is >= 0x08 and <= 0x0C
                                ? new FlacFixedSubFrame(flacFrame)
                                : type >= 0x20 ? new FlacLinearPredictorSubFrame(flacFrame) : new FlacSubFrame(flacFrame),
        };
        frame.ReadSubFrame(sb, channel);
        return frame;
    }

    private void ReadSubFrame(StreamBuffer sb, int channel)
    {
        ArgumentNullException.ThrowIfNull(sb);

        var sampleSize = FlacFrame.SampleSize;
        if (((FlacFrame.ChannelAssignment is FlacChannelAssignment.LeftSide or FlacChannelAssignment.MidSide) && channel == 1) || (FlacFrame.ChannelAssignment == FlacChannelAssignment.RightSide && channel == 0))
        {
            sampleSize++;
        }

        ReadHeader(sb);
        SampleSize = sampleSize - WastedBits;
        Read(sb, SampleSize, FlacFrame.BlockSize);
    }
}
