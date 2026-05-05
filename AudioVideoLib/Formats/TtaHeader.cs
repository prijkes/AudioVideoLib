namespace AudioVideoLib.Formats;

using System;

/// <summary>
/// Fixed 22-byte TTA1 header at the start of a TrueAudio stream (after any leading ID3v2 tag).
/// </summary>
/// <remarks>
/// Mirrors the <c>TTA_info</c> struct in <c>3rdparty/libtta-c-2.3/libtta.h</c>, plus the
/// 4-byte trailing CRC32 that <c>read_tta_header</c> in <c>libtta.c</c> consumes immediately
/// after the five info fields. All multi-byte integers are little-endian.
/// </remarks>
public sealed class TtaHeader
{
    /// <summary>Size of the fixed header on disk, in bytes (4-byte magic + 18 bytes of info + 4-byte CRC).</summary>
    public const int FixedSize = 22;

    /// <summary>The four-byte magic at offset 0: <c>T T A 1</c>.</summary>
    public static ReadOnlySpan<byte> Magic => "TTA1"u8;

    internal TtaHeader(
        ushort format,
        ushort numChannels,
        ushort bitsPerSample,
        uint sampleRate,
        uint totalSamples,
        uint headerCrc32)
    {
        Format = format;
        NumChannels = numChannels;
        BitsPerSample = bitsPerSample;
        SampleRate = sampleRate;
        TotalSamples = totalSamples;
        HeaderCrc32 = headerCrc32;
    }

    /// <summary>Audio format code: <c>1 = simple</c>, <c>2 = encrypted</c> (libtta <c>TTA_FORMAT_*</c>).</summary>
    public ushort Format { get; }

    /// <summary>Channel count (libtta caps at <c>MAX_NCH = 6</c>).</summary>
    public ushort NumChannels { get; }

    /// <summary>Bits per sample (libtta range: <c>MIN_BPS = 16</c> through <c>MAX_BPS = 24</c>).</summary>
    public ushort BitsPerSample { get; }

    /// <summary>Sample rate in Hz (libtta <c>sps</c>).</summary>
    public uint SampleRate { get; }

    /// <summary>Total decoded sample count across the whole stream.</summary>
    public uint TotalSamples { get; }

    /// <summary>CRC32 stored at offsets [18..22) of the fixed header.</summary>
    public uint HeaderCrc32 { get; }
}
