namespace AudioVideoLib.Formats;

using System;
using System.Buffers.Binary;

/// <summary>
/// 32-byte WavPack block header preamble. Layout per
/// <c>3rdparty/WavPack/include/wavpack.h:69-76</c> (<c>WavpackHeader</c> struct,
/// format <c>"4LS2LLLLL"</c>).
/// </summary>
/// <remarks>
/// WavPack files are a flat concatenation of blocks; every block begins with this
/// header. The walker reads the header to discover block size (so it can seek to
/// the next block) and to interpret the flags bitfield (sample rate index, channel
/// count, bytes-per-sample). The 40-bit <see cref="BlockIndex"/> and
/// <see cref="TotalSamples"/> fields combine the low 32-bit field with the high
/// 8-bit "u8" extension byte, matching the <c>GET_BLOCK_INDEX</c> /
/// <c>GET_TOTAL_SAMPLES</c> macros in <c>wavpack.h:82-95</c>.
/// </remarks>
public sealed class WavPackBlockHeader
{
    /// <summary>Size of the on-disk header in bytes (always 32).</summary>
    public const int Size = 32;

    internal WavPackBlockHeader(
        string ckId,
        uint ckSize,
        ushort version,
        byte blockIndexHigh,
        byte totalSamplesHigh,
        uint totalSamplesLow,
        uint blockIndexLow,
        uint blockSamples,
        uint flags,
        uint crc)
    {
        CkId = ckId;
        CkSize = ckSize;
        Version = version;
        BlockIndexHigh = blockIndexHigh;
        TotalSamplesHigh = totalSamplesHigh;
        TotalSamplesLow = totalSamplesLow;
        BlockIndexLow = blockIndexLow;
        BlockSamples = blockSamples;
        Flags = flags;
        Crc = crc;
    }

    /// <summary>Gets the four-byte chunk ID; always <c>"wvpk"</c> for valid blocks.</summary>
    public string CkId { get; }

    /// <summary>
    /// Gets the block payload size in bytes — i.e., <c>ckSize</c>. The total on-disk block size
    /// is <c>CkSize + 8</c> because <c>ckSize</c> excludes the leading 8 bytes of the
    /// header (per the WavPack file-format documentation referenced in
    /// <c>wavpack.h:62-67</c>).
    /// </summary>
    public uint CkSize { get; }

    /// <summary>Gets the stream version. Library handles 0x402..0x410 per <c>wavpack.h:146-147</c>.</summary>
    public ushort Version { get; }

    /// <summary>Gets the high 8 bits of the 40-bit block index.</summary>
    public byte BlockIndexHigh { get; }

    /// <summary>Gets the high 8 bits of the 40-bit total-samples field.</summary>
    public byte TotalSamplesHigh { get; }

    /// <summary>
    /// Gets the low 32 bits of total samples; combined with <see cref="TotalSamplesHigh"/> per
    /// <c>GET_TOTAL_SAMPLES</c> (<c>wavpack.h:94</c>). A value of <c>0xFFFFFFFF</c> in
    /// the low 32 bits signals "unknown" — surfaced as <see cref="TotalSamples"/> == -1.
    /// </summary>
    public uint TotalSamplesLow { get; }

    /// <summary>Gets the low 32 bits of the block-index field.</summary>
    public uint BlockIndexLow { get; }

    /// <summary>Gets the number of audio samples encoded in this block.</summary>
    public uint BlockSamples { get; }

    /// <summary>Gets the flags bitfield. See <c>wavpack.h:108-141</c> for bit definitions.</summary>
    public uint Flags { get; }

    /// <summary>Gets the block CRC.</summary>
    public uint Crc { get; }

    /// <summary>Gets the combined 40-bit block index per <c>GET_BLOCK_INDEX</c>.</summary>
    public long BlockIndex => BlockIndexLow + ((long)BlockIndexHigh << 32);

    /// <summary>
    /// Gets the combined 40-bit total-samples value, or <c>-1</c> if unknown
    /// (low 32 bits == <c>0xFFFFFFFF</c>) per <c>GET_TOTAL_SAMPLES</c>.
    /// </summary>
    public long TotalSamples => (TotalSamplesLow == 0xFFFFFFFFu)
        ? -1L
        : ((long)TotalSamplesLow + ((long)TotalSamplesHigh << 32) - TotalSamplesHigh);

    /// <summary>Gets the bytes per sample (1-4) decoded from the <c>BYTES_STORED</c> field — <c>(flags &amp; 3) + 1</c>.</summary>
    public int BytesPerSample => (int)(Flags & 0x3u) + 1;

    /// <summary>Gets a value indicating whether the block represents mono audio (<c>MONO_FLAG</c>, <c>wavpack.h:111</c>).</summary>
    public bool IsMono => (Flags & 0x4u) != 0;

    /// <summary>Gets a value indicating whether the block is from a hybrid-mode encoding (<c>HYBRID_FLAG</c>, <c>wavpack.h:112</c>).</summary>
    public bool IsHybrid => (Flags & 0x8u) != 0;

    /// <summary>Gets a value indicating whether the block stores IEEE 32-bit float samples (<c>FLOAT_DATA</c>, <c>wavpack.h:116</c>).</summary>
    public bool IsFloat => (Flags & 0x80u) != 0;

    /// <summary>Gets a value indicating whether this is the initial block of a multichannel segment (<c>INITIAL_BLOCK</c>).</summary>
    public bool IsInitialBlock => (Flags & 0x800u) != 0;

    /// <summary>Gets a value indicating whether this is the final block of a multichannel segment (<c>FINAL_BLOCK</c>).</summary>
    public bool IsFinalBlock => (Flags & 0x1000u) != 0;

    /// <summary>Gets a value indicating whether the block is encoded DSD (<c>DSD_FLAG</c>, <c>wavpack.h:141</c>).</summary>
    public bool IsDsd => (Flags & 0x80000000u) != 0;

    /// <summary>
    /// Gets the sample rate in Hz, or <c>0</c> if the rate index is the reserved "non-standard" value (15).
    /// Decoded from the <c>SRATE_MASK</c> bits at <c>SRATE_LSB == 23</c> per
    /// <c>wavpack.h:131-132</c>; index table from <c>3rdparty/WavPack/src/common_utils.c:31-32</c>.
    /// </summary>
    public int SampleRate
    {
        get
        {
            var idx = (int)((Flags >> 23) & 0xFu);
            return idx switch
            {
                0 => 6000,
                1 => 8000,
                2 => 9600,
                3 => 11025,
                4 => 12000,
                5 => 16000,
                6 => 22050,
                7 => 24000,
                8 => 32000,
                9 => 44100,
                10 => 48000,
                11 => 64000,
                12 => 88200,
                13 => 96000,
                14 => 192000,
                _ => 0, // index 15 = "non-standard rate"; carried in an ID_SAMPLE_RATE sub-block.
            };
        }
    }

    /// <summary>
    /// Parses the 32-byte preamble. Returns <c>null</c> if <paramref name="span"/> is
    /// shorter than <see cref="Size"/> or its first four bytes are not the ASCII
    /// magic <c>"wvpk"</c>.
    /// </summary>
    /// <param name="span">The candidate header bytes.</param>
    /// <returns>A populated <see cref="WavPackBlockHeader"/>, or <c>null</c> on rejection.</returns>
    /// <remarks>
    /// Ports the LE field-decoding portion of
    /// <c>3rdparty/WavPack/src/open_utils.c:read_next_header</c> (lines 951-984).
    /// The brute-force resync loop in that function is the caller's responsibility;
    /// this method only validates the four magic bytes and decodes the struct.
    /// </remarks>
    public static WavPackBlockHeader? Parse(ReadOnlySpan<byte> span)
    {
        return ((span.Length < Size) ||
                (span[0] != (byte)'w') || (span[1] != (byte)'v') ||
                (span[2] != (byte)'p') || (span[3] != (byte)'k'))
            ? null
            : new WavPackBlockHeader(
                ckId: "wvpk",
                ckSize: BinaryPrimitives.ReadUInt32LittleEndian(span[4..8]),
                version: BinaryPrimitives.ReadUInt16LittleEndian(span[8..10]),
                blockIndexHigh: span[10],
                totalSamplesHigh: span[11],
                totalSamplesLow: BinaryPrimitives.ReadUInt32LittleEndian(span[12..16]),
                blockIndexLow: BinaryPrimitives.ReadUInt32LittleEndian(span[16..20]),
                blockSamples: BinaryPrimitives.ReadUInt32LittleEndian(span[20..24]),
                flags: BinaryPrimitives.ReadUInt32LittleEndian(span[24..28]),
                crc: BinaryPrimitives.ReadUInt32LittleEndian(span[28..32]));
    }
}
