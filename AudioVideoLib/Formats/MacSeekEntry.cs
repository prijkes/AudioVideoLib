namespace AudioVideoLib.Formats;

/// <summary>
/// A single entry from the Monkey's Audio seek table — the absolute file offset of an APE frame's
/// first byte. The seek table is a packed array of 32-bit little-endian offsets; element count is
/// <see cref="MacDescriptor.SeekTableBytes"/> / 4. See
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APEHeader.cpp:268</c>.
/// </summary>
/// <remarks>
/// The on-disk offsets are 32-bit, but for files larger than 4 GiB the upstream encoder
/// allows them to wrap. <see cref="MacStream"/> applies the same wrap-correction as
/// <c>Convert32BitSeekTable</c> (<c>APEHeader.cpp:116-131</c>) — accumulating
/// <c>0x1_0000_0000</c> each time the raw value decreases — so <see cref="FileOffset"/>
/// is widened to <see cref="long"/> and represents the corrected absolute offset.
/// </remarks>
public sealed class MacSeekEntry
{
    internal MacSeekEntry(int frameIndex, long fileOffset)
    {
        FrameIndex = frameIndex;
        FileOffset = fileOffset;
    }

    /// <summary>Gets the index of the frame this entry refers to (0-based).</summary>
    public int FrameIndex { get; }

    /// <summary>Gets the absolute file offset of the frame's first byte (post wrap-correction).</summary>
    public long FileOffset { get; }
}
