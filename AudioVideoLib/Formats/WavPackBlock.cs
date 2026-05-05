namespace AudioVideoLib.Formats;

using System.Collections.Generic;

/// <summary>
/// One WavPack block: 32-byte <see cref="WavPackBlockHeader"/> followed by a
/// concatenation of <see cref="WavPackSubBlock"/> records.
/// </summary>
public sealed class WavPackBlock
{
    internal WavPackBlock(
        WavPackBlockHeader header,
        long startOffset,
        long length,
        IReadOnlyList<WavPackSubBlock> subBlocks)
    {
        Header = header;
        StartOffset = startOffset;
        Length = length;
        SubBlocks = subBlocks;
    }

    /// <summary>Gets the decoded 32-byte preamble.</summary>
    public WavPackBlockHeader Header { get; }

    /// <summary>Gets the file offset of the first byte of this block (the <c>'w'</c> of <c>wvpk</c>).</summary>
    public long StartOffset { get; }

    /// <summary>
    /// Gets the total on-disk block length in bytes — equals <c>Header.CkSize + 8</c> per the
    /// <c>ckSize</c>-excludes-leading-8 convention noted at <c>wavpack.h:62-67</c>.
    /// </summary>
    public long Length { get; }

    /// <summary>Gets the sub-block summaries in file order.</summary>
    public IReadOnlyList<WavPackSubBlock> SubBlocks { get; }
}
