namespace AudioVideoLib.Formats;

/// <summary>
/// A single chunk inside an AIFF or AIFF-C container.
/// </summary>
/// <param name="Id">The four-character chunk identifier (e.g. <c>"COMM"</c>, <c>"SSND"</c>).</param>
/// <param name="StartOffset">Offset of the chunk header (the id) from the start of the stream.</param>
/// <param name="EndOffset">Offset immediately past the chunk's data (padding byte excluded).</param>
/// <param name="Data">The chunk's raw payload. Empty for chunks the walker deliberately skipped.</param>
public sealed record AiffChunk(string Id, long StartOffset, long EndOffset, byte[] Data)
{
    /// <summary>
    /// Gets the chunk's total length in bytes, including its 8-byte header.
    /// </summary>
    public long Size => EndOffset - StartOffset;
}
