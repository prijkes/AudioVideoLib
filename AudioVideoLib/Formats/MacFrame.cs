namespace AudioVideoLib.Formats;

/// <summary>
/// A single Monkey's Audio frame — a contiguous byte range in the source file plus the
/// number of audio blocks it encodes. Frame boundaries are derived from the seek table; the
/// final frame's length is bounded by <see cref="MacDescriptor.TotalApeFrameDataBytes"/>.
/// </summary>
public sealed class MacFrame
{
    internal MacFrame(long startOffset, long length, uint blockCount)
    {
        StartOffset = startOffset;
        Length = length;
        BlockCount = blockCount;
    }

    /// <summary>Gets the absolute file offset of the frame's first byte.</summary>
    public long StartOffset { get; }

    /// <summary>Gets the length of the frame in bytes.</summary>
    public long Length { get; }

    /// <summary>Gets the number of audio blocks the frame encodes.</summary>
    /// <remarks>
    /// For non-final frames this equals <see cref="MacHeader.BlocksPerFrame"/>; for the final
    /// frame it equals <see cref="MacHeader.FinalFrameBlocks"/>.
    /// </remarks>
    public uint BlockCount { get; }
}
