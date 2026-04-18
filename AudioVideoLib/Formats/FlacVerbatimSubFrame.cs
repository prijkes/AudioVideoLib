namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public sealed class FlacVerbatimSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded subblock.
    /// </summary>
    /// <value>
    /// The unencoded subblock.
    /// </value>
    public int[] UnencodedSubblocks { get; private set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the specified stream buffer.
    /// </summary>
    /// <param name="sb">The stream buffer.</param>
    /// <param name="sampeSize">Size of the sample.</param>
    /// <param name="blockSize">Size of the block.</param>
    protected override void Read(StreamBuffer sb, int sampeSize, int blockSize)
    {
        UnencodedSubblocks = new int[blockSize];
        for (var i = 0; i < blockSize; i++)
        {
            UnencodedSubblocks[i] = sb.ReadBigEndianInt32();
        }
    }
}
