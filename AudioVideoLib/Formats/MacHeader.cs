namespace AudioVideoLib.Formats;

/// <summary>
/// The Monkey's Audio per-stream header — fixed-size structure that follows the
/// <see cref="MacDescriptor"/>. Field layout mirrors <c>APE_HEADER</c> in
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:199-211</c>.
/// </summary>
public sealed class MacHeader
{
    /// <summary>
    /// <c>APE_FORMAT_FLAG_CREATE_WAV_HEADER</c> from
    /// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:84</c> — when set, the original WAV
    /// header was discarded at encode time and synthesised on decode (so
    /// <see cref="MacDescriptor.HeaderDataBytes"/> is zero).
    /// </summary>
    public const ushort CreateWavHeaderFlag = 1 << 5;

    internal MacHeader(
        ushort compressionLevel,
        ushort formatFlags,
        uint blocksPerFrame,
        uint finalFrameBlocks,
        uint totalFrames,
        ushort bitsPerSample,
        ushort channels,
        uint sampleRate)
    {
        CompressionLevel = compressionLevel;
        FormatFlags = formatFlags;
        BlocksPerFrame = blocksPerFrame;
        FinalFrameBlocks = finalFrameBlocks;
        TotalFrames = totalFrames;
        BitsPerSample = bitsPerSample;
        Channels = channels;
        SampleRate = sampleRate;
    }

    /// <summary>Gets the compression level (1000=fast, 2000=normal, 3000=high, 4000=extra-high, 5000=insane).</summary>
    public ushort CompressionLevel { get; }

    /// <summary>Gets the format flags. Bit 5 (<see cref="CreateWavHeaderFlag"/>) is the only one this walker interprets.</summary>
    public ushort FormatFlags { get; }

    /// <summary>Gets the number of audio blocks in a non-final frame.</summary>
    public uint BlocksPerFrame { get; }

    /// <summary>Gets the number of audio blocks in the final frame (≤ <see cref="BlocksPerFrame"/>).</summary>
    public uint FinalFrameBlocks { get; }

    /// <summary>Gets the total frame count.</summary>
    public uint TotalFrames { get; }

    /// <summary>Gets the bits per audio sample (typically 16 or 24; 32 for float).</summary>
    public ushort BitsPerSample { get; }

    /// <summary>Gets the channel count (1 or 2 typical; up to <c>APE_MAXIMUM_CHANNELS</c>).</summary>
    public ushort Channels { get; }

    /// <summary>Gets the sample rate in Hz.</summary>
    public uint SampleRate { get; }

    /// <summary>Gets a value indicating whether <see cref="CreateWavHeaderFlag"/> is set in <see cref="FormatFlags"/>.</summary>
    public bool CreatesWavHeaderOnDecode => (FormatFlags & CreateWavHeaderFlag) != 0;
}
