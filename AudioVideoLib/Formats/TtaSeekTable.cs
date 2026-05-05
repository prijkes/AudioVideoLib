namespace AudioVideoLib.Formats;

using System.Collections.Generic;

/// <summary>
/// The per-frame compressed-size table that follows the TTA1 fixed header.
/// </summary>
/// <remarks>
/// Mirrors the body of <c>tta_decoder_read_seek_table</c> in
/// <c>3rdparty/libtta-c-2.3/libtta.c</c>: one little-endian uint32 per frame giving the
/// compressed length in bytes, followed by one trailing uint32 CRC32 over the table.
/// </remarks>
public sealed class TtaSeekTable
{
    internal TtaSeekTable(IReadOnlyList<uint> frameSizes, uint crc32)
    {
        FrameSizes = frameSizes;
        Crc32 = crc32;
    }

    /// <summary>Compressed byte length of each frame, in file order.</summary>
    public IReadOnlyList<uint> FrameSizes { get; }

    /// <summary>CRC32 immediately following the per-frame size array.</summary>
    public uint Crc32 { get; }

    /// <summary>Total size of the seek-table region on disk (entries + trailing CRC).</summary>
    public int Size => (FrameSizes.Count + 1) * 4;
}
