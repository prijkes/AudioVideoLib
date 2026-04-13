namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public sealed class FlacVerbatimSubFrame : FlacSubFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlacVerbatimSubFrame"/> class.
    /// </summary>
    /// <param name="flacFrame">The <see cref="FlacFrame"/>.</param>
    public FlacVerbatimSubFrame(FlacFrame flacFrame) : base(flacFrame)
    {
        ArgumentNullException.ThrowIfNull(flacFrame);
    }

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
