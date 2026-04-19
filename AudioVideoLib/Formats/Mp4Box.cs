namespace AudioVideoLib.Formats;

/// <summary>
/// A single ISO/IEC 14496-12 box (atom) discovered while walking an MP4 / M4A container.
/// </summary>
/// <param name="Type">
/// The four-character atom type (e.g. <c>"moov"</c>, <c>"udta"</c>, <c>"©nam"</c>, <c>"covr"</c>).
/// Surrogate pair <c>©</c> in iTunes atoms is encoded as the single byte <c>0xA9</c>; this property
/// keeps the original 4-character string after Latin-1 decoding so callers can match by literal.
/// </param>
/// <param name="StartOffset">Offset of the atom header (its size field) from the start of the stream.</param>
/// <param name="EndOffset">Offset immediately past the atom payload.</param>
/// <param name="HeaderSize">Size of the atom header in bytes (8 for 32-bit size, 16 for 64-bit extended size).</param>
public sealed record Mp4Box(string Type, long StartOffset, long EndOffset, int HeaderSize)
{
    /// <summary>
    /// Gets the atom's total length in bytes, including its header.
    /// </summary>
    public long Size => EndOffset - StartOffset;

    /// <summary>
    /// Gets the offset of the atom payload (the byte immediately after the header).
    /// </summary>
    public long PayloadOffset => StartOffset + HeaderSize;

    /// <summary>
    /// Gets the size of the atom payload in bytes (total size minus header).
    /// </summary>
    public long PayloadSize => Size - HeaderSize;
}
