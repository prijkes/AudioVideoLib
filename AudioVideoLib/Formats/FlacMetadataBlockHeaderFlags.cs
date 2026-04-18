namespace AudioVideoLib.Formats;

public partial class FlacMetadataBlock
{
    /// <summary>
    /// Field flags.
    /// </summary>
    private readonly struct HeaderFlags
    {
        /// <summary>
        /// The last block bit.
        /// </summary>
        public const int IsLastBlock = 0x80;

        /// <summary>
        /// The block type bit.
        /// </summary>
        public const int BlockType = 0x7F;
    }
}
