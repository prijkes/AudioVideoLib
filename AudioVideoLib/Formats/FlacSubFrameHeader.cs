namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// 
/// </summary>
public partial class FlacSubFrame
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the sub frame type.
    /// </summary>
    /// <value>
    /// The sub frame type.
    /// </value>
    public FlacSubFrameType Type { get; private set; }

    /// <summary>
    /// Gets the true sample size.
    /// </summary>
    /// <value>
    /// The true sample size.
    /// </value>
    /// <remarks>
    /// This is the sub frame's sample size (as determined from the <see cref="FlacFrame"/>'s sample size, 
    /// amended by the <see cref="FlacFrame.ChannelAssignment"/>)
    /// and minus the <see cref="WastedBits">number of wasted bits</see>.
    /// </remarks>
    public int SampleSize { get; private set; }

    /// <summary>
    /// Gets the wasted bits-per-sample.
    /// </summary>
    /// <value>
    /// The wasted bits-per-sample.
    /// </value>
    public int WastedBits { get; private set; }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <value>
    /// The header.
    /// </value>
    protected int Header { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    private void ReadHeader(StreamBuffer sb)
    {
        ArgumentNullException.ThrowIfNull(sb);

        Header = sb.ReadBigEndianInt32();

        WastedBits = Header & 0x01;
        if (WastedBits > 0)
        {
            WastedBits = sb.ReadUnaryInt();
        }

        var type = (Header >> 1) & 0x7E;
        Type = type switch
        {
            0x00 => FlacSubFrameType.Constant,
            0x01 => FlacSubFrameType.Verbatim,
            _ => type is >= 0x08 and <= 0x0C
                                ? FlacSubFrameType.Fixed
                                : type >= 0x20 ? FlacSubFrameType.LinearPredictor : FlacSubFrameType.Reserved,
        };
    }
}
