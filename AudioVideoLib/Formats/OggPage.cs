namespace AudioVideoLib.Formats;

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
    public long Size => EndOffset - StartOffset;

    public bool IsContinuation => (HeaderFlags & 0x01) != 0;

    public bool IsBeginningOfStream => (HeaderFlags & 0x02) != 0;

    public bool IsEndOfStream => (HeaderFlags & 0x04) != 0;
}
