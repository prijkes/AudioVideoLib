namespace AudioVideoLib.Formats;

/// <summary>
/// Public class for MPEG audio frames.
/// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
/// </summary>
public sealed partial class MpaFrame
{
    // possible quantization per sub band table
    private sealed class SubbandQuantization
    {
        /// <summary>
        /// Gets the sub band limit.
        /// </summary>
        public int SubbandLimit { get; init; }

        /// <summary>
        /// Gets the offsets.
        /// </summary>
        public int[] Offsets { get; init; } = new int[30];
    }
}
