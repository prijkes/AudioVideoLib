namespace AudioVideoLib.Formats;

/// <summary>
/// A single TTA audio frame, located by a byte range in the source stream.
/// </summary>
/// <remarks>
/// Per <c>3rdparty/libtta-c-2.3/libtta.c</c>: every frame except the last carries
/// <c>FrameLengthSamples = 256 * SampleRate / 245</c> samples; the last frame holds the
/// remainder (<c>TotalSamples mod FrameLengthSamples</c>, or the standard length if that
/// modulus is zero).
/// </remarks>
public sealed class TtaFrame
{
    internal TtaFrame(long startOffset, long length, uint sampleCount)
    {
        StartOffset = startOffset;
        Length = length;
        SampleCount = sampleCount;
    }

    /// <summary>Byte offset of the frame's first byte, relative to the source stream's origin.</summary>
    public long StartOffset { get; }

    /// <summary>Frame length in bytes (from the seek table).</summary>
    public long Length { get; }

    /// <summary>Decoded sample count for this frame.</summary>
    public uint SampleCount { get; }
}
