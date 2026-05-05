namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;

/// <summary>
/// The Monkey's Audio file descriptor — the first structure in every APE file (3.98+).
/// Field layout mirrors <c>APE_DESCRIPTOR</c> in
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/MACLib.h:179-194</c>.
/// </summary>
/// <remarks>
/// All multi-byte values are little-endian on disk. This model holds the host-endian
/// representation; the walker is responsible for the conversion at parse time.
/// </remarks>
public sealed class MacDescriptor
{
    internal MacDescriptor(
        string id,
        ushort version,
        uint descriptorBytes,
        uint headerBytes,
        uint seekTableBytes,
        uint headerDataBytes,
        uint apeFrameDataBytes,
        uint apeFrameDataBytesHigh,
        uint terminatingDataBytes,
        ReadOnlySpan<byte> fileMd5)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (fileMd5.Length != 16)
        {
            throw new ArgumentException("FileMd5 must be exactly 16 bytes.", nameof(fileMd5));
        }

        Id = id;
        Version = version;
        DescriptorBytes = descriptorBytes;
        HeaderBytes = headerBytes;
        SeekTableBytes = seekTableBytes;
        HeaderDataBytes = headerDataBytes;
        ApeFrameDataBytes = apeFrameDataBytes;
        ApeFrameDataBytesHigh = apeFrameDataBytesHigh;
        TerminatingDataBytes = terminatingDataBytes;
        FileMd5 = fileMd5.ToArray();
    }

    /// <summary>Gets the 4-byte magic — either <c>"MAC "</c> (integer) or <c>"MACF"</c> (float).</summary>
    public string Id { get; }

    /// <summary>Gets the version number scaled by 1000 (3.99 → 3990, 4.10 → 4100, etc.).</summary>
    public ushort Version { get; }

    /// <summary>Gets the total bytes of the descriptor (allows future expansion).</summary>
    public uint DescriptorBytes { get; }

    /// <summary>Gets the bytes occupied by the <see cref="MacHeader"/> region that follows the descriptor.</summary>
    public uint HeaderBytes { get; }

    /// <summary>Gets the bytes occupied by the seek table.</summary>
    public uint SeekTableBytes { get; }

    /// <summary>
    /// Gets the bytes occupied by the original-file WAV header data (zero when
    /// <c>APE_FORMAT_FLAG_CREATE_WAV_HEADER</c> is set in <see cref="MacHeader.FormatFlags"/>).
    /// </summary>
    public uint HeaderDataBytes { get; }

    /// <summary>Gets the low 32 bits of the APE frame-data byte count.</summary>
    public uint ApeFrameDataBytes { get; }

    /// <summary>Gets the high 32 bits of the APE frame-data byte count (for files &gt; 4 GiB).</summary>
    public uint ApeFrameDataBytesHigh { get; }

    /// <summary>Gets the bytes of terminating data after the audio (e.g., trailing WAV chunk junk, but not tag data).</summary>
    public uint TerminatingDataBytes { get; }

    /// <summary>Gets the 16-byte MD5 of the original file content (see SDK notes — usage is non-trivial).</summary>
    public IReadOnlyList<byte> FileMd5 { get; }

    /// <summary>Gets the combined 64-bit total of <see cref="ApeFrameDataBytes"/> and <see cref="ApeFrameDataBytesHigh"/>.</summary>
    public long TotalApeFrameDataBytes => ((long)ApeFrameDataBytesHigh << 32) | ApeFrameDataBytes;
}
