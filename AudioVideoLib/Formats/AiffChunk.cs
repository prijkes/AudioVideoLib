namespace AudioVideoLib.Formats;

public sealed record AiffChunk(string Id, long StartOffset, long EndOffset, byte[] Data)
{
    public long Size => EndOffset - StartOffset;
}
