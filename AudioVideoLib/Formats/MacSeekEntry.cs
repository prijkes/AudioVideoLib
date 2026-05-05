namespace AudioVideoLib.Formats;

/// <summary>
/// A single entry from the Monkey's Audio seek table — the absolute file offset of an APE frame's
/// first byte. The seek table is a packed array of 32-bit little-endian offsets; element count is
/// <see cref="MacDescriptor.SeekTableBytes"/> / 4. See
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:268</c>.
/// </summary>
public sealed class MacSeekEntry
{
    internal MacSeekEntry(int frameIndex, uint fileOffset)
    {
        FrameIndex = frameIndex;
        FileOffset = fileOffset;
    }

    /// <summary>Gets the index of the frame this entry refers to (0-based).</summary>
    public int FrameIndex { get; }

    /// <summary>Gets the absolute file offset of the frame's first byte.</summary>
    public uint FileOffset { get; }
}
