namespace AudioVideoLib.Formats;

/// <summary>
/// Public class for MPEG audio frames.
/// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
/// </summary>
public sealed partial class MpaFrame
{
    // possible quantization per sub band table
    private class SubbandQuantization
    {

        /// <summary>
        /// Gets or sets the sub band limit.
        /// </summary>
        public int SubbandLimit { get; set; }

        /// <summary>
        /// Gets or sets the offsets.
        /// </summary>
        public int[] Offsets { get; set; } = new int[30];
    }
}
