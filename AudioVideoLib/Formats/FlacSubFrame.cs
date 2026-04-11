/*
 * Date: 2013-03-22
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
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
        FlacFrame = flacFrame ?? throw new ArgumentNullException("flacFrame");
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
        return stream == null
            ? throw new ArgumentNullException("stream")
            : flacFrame == null
            ? throw new ArgumentNullException("flacFrame")
            : ReadSubFrame(stream as StreamBuffer ?? new StreamBuffer(stream), channel, flacFrame);
    }

    /// <summary>
    /// Returns the frame in a byte array.
    /// </summary>
    /// <returns>The frame in a byte array.</returns>
    public virtual byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.WriteBigEndianInt32(Header);
        sb.WriteUnaryInt(WastedBits);
        return sb.ToByteArray();
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
        if (sb == null)
        {
            throw new ArgumentNullException("sb");
        }

        if (flacFrame == null)
        {
            throw new ArgumentNullException("flacFrame");
        }

        var header = sb.ReadBigEndianInt32(false);
        var type = (header >> 1) & 0x7E;

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
        if (sb == null)
        {
            throw new ArgumentNullException("sb");
        }

        var sampleSize = FlacFrame.SampleSize;
        if ((((FlacFrame.ChannelAssignment == FlacChannelAssignment.LeftSide) || (FlacFrame.ChannelAssignment == FlacChannelAssignment.MidSide)) && (channel == 1)) || ((FlacFrame.ChannelAssignment == FlacChannelAssignment.RightSide) && (channel == 0)))
        {
            sampleSize++;
        }

        ReadHeader(sb);
        SampleSize = sampleSize - WastedBits;
        ////Read(sb);
    }
}
