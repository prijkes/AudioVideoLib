namespace AudioVideoLib.Formats;

/// <summary>
/// A single chunk inside a DSD audio container — either Sony DSF (<c>.dsf</c>) or Philips DFF (<c>.dff</c>).
/// </summary>
/// <param name="Id">The four-character chunk identifier (e.g. <c>"DSD "</c>, <c>"fmt "</c>, <c>"data"</c>, <c>"FRM8"</c>, <c>"FVER"</c>, <c>"PROP"</c>).</param>
/// <param name="StartOffset">Offset of the chunk header (the id) from the start of the stream.</param>
/// <param name="EndOffset">Offset immediately past the chunk's payload (DFF padding byte excluded).</param>
/// <param name="Data">The chunk's raw payload. Empty for chunks the walker deliberately skipped.</param>
public sealed record DsdChunk(string Id, long StartOffset, long EndOffset, byte[] Data)
{
    /// <summary>
    /// Gets the chunk's total length in bytes, including its header.
    /// </summary>
    public long Size => EndOffset - StartOffset;
}
