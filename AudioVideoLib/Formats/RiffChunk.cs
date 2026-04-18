namespace AudioVideoLib.Formats;

/// <summary>
/// A single RIFF chunk inside a WAV or other RIFF container.
/// </summary>
public sealed record RiffChunk(string Id, long StartOffset, long EndOffset, byte[] Data)
{
    public long Size => EndOffset - StartOffset;
}
