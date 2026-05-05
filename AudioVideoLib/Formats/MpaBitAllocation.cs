namespace AudioVideoLib.Formats;

/// <summary>
/// Public class for MPEG audio frames.
/// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
/// </summary>
public sealed partial class MpaFrame
{
    private sealed class BitAllocation
    {
        /// <summary>
        /// Gets the number of bits allocated.
        /// </summary>
        public short BitsAllocated { get; init; }

        /// <summary>
        /// Gets the offset of the bits.
        /// </summary>
        public short Offset { get; init; }
    }
}
