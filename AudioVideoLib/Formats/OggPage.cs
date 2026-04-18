namespace AudioVideoLib.Formats;

/// <summary>
/// A single page within an Ogg bitstream (RFC 3533).
/// </summary>
/// <param name="StartOffset">Offset of the page header (the <c>"OggS"</c> magic) from the start of the stream.</param>
/// <param name="EndOffset">Offset immediately past the page payload.</param>
/// <param name="Version">Ogg page structure version. Always 0 for the current spec.</param>
/// <param name="HeaderFlags">Page header type flags (continuation / BOS / EOS bits).</param>
/// <param name="GranulePosition">Codec-specific position marker — typically a sample count for Vorbis/Opus.</param>
/// <param name="SerialNumber">Logical bitstream identifier; pages sharing a serial number belong to the same stream.</param>
/// <param name="SequenceNumber">Monotonically increasing page counter within the logical bitstream.</param>
/// <param name="Checksum">CRC-32 checksum from the page header (not recomputed by the walker).</param>
/// <param name="SegmentCount">Number of entries in the page's segment table.</param>
/// <param name="PayloadSize">Total size of the page payload, in bytes.</param>
public sealed record OggPage(
    long StartOffset,
    long EndOffset,
    int Version,
    byte HeaderFlags,
    long GranulePosition,
    int SerialNumber,
    int SequenceNumber,
    int Checksum,
    int SegmentCount,
    int PayloadSize)
{
    /// <summary>
    /// Gets the page's total length in bytes, including its header and segment table.
    /// </summary>
    public long Size => EndOffset - StartOffset;

    /// <summary>
    /// Gets a value indicating whether this page continues a packet started on a previous page.
    /// </summary>
    public bool IsContinuation => (HeaderFlags & 0x01) != 0;

    /// <summary>
    /// Gets a value indicating whether this page is the beginning of a logical bitstream.
    /// </summary>
    public bool IsBeginningOfStream => (HeaderFlags & 0x02) != 0;

    /// <summary>
    /// Gets a value indicating whether this page is the end of a logical bitstream.
    /// </summary>
    public bool IsEndOfStream => (HeaderFlags & 0x04) != 0;
}
