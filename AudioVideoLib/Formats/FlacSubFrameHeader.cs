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

    private void ReadHeader(BitStream bs, int headerByte)
    {
        ArgumentNullException.ThrowIfNull(bs);

        // RFC 9639 §11.25: subframe header is a SINGLE BYTE (already consumed by ReadSubFrame):
        //   bit 7 (MSB) = zero-pad (must be 0; ignored on read for compatibility)
        //   bits 6..1   = subframe type (6 bits)
        //   bit 0 (LSB) = wasted-bits-per-sample flag
        // If the wasted-bits flag is set, a unary-coded count follows (bit-packed):
        // zero bits terminated by a 1. WastedBitsPerSample = leading-zero-count + 1.
        Header = headerByte;

        // RFC 9639 §11.25: wasted-bits flag is bit 0 (LSB) of the subframe header byte.
        WastedBits = Header & 0x01;
        if (WastedBits > 0)
        {
            // Bit-aligned unary read on the BitStream: count of leading zeros before
            // the terminating 1, plus 1 for the terminator bit itself.
            WastedBits = bs.ReadUnaryInt() + 1;
        }

        // RFC 9639 §11.25: subframe type lives in bits 6..1 of the header byte.
        var type = (Header >> 1) & 0x3F;
        Type = type switch
        {
            0x00 => FlacSubFrameType.Constant,
            0x01 => FlacSubFrameType.Verbatim,
            >= 0x08 and <= 0x0C => FlacSubFrameType.Fixed,
            >= 0x20 => FlacSubFrameType.LinearPredictor,
            _ => FlacSubFrameType.Reserved,
        };
    }
}
