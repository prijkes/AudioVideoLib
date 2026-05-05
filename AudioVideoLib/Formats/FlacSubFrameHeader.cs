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

        // RFC 9639 §11.25: subframe header is a SINGLE BYTE
        //   bit 7 (MSB) = zero-pad (must be 0; ignored on read for compatibility)
        //   bits 6..1   = subframe type (6 bits)
        //   bit 0 (LSB) = wasted-bits-per-sample flag
        // If the wasted-bits flag is set, a unary-coded count follows: zero bits
        // terminated by a 1. WastedBitsPerSample = leading-zero-count + 1.
        Header = sb.ReadByte();

        WastedBits = Header & 0x01;
        if (WastedBits > 0)
        {
            // ReadUnaryInt returns the leading-zero-count; the wasted-bits-per-
            // sample value is that count + 1 (the terminating 1 represents one).
            WastedBits = sb.ReadUnaryInt() + 1;
        }

        var type = (Header >> 1) & 0x3F;
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
