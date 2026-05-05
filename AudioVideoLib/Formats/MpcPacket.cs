namespace AudioVideoLib.Formats;

/// <summary>
/// One unit of the Musepack bitstream — an SV7 frame or an SV8 keyed packet.
/// </summary>
/// <remarks>
/// SV7 uses "frames", SV8 uses keyed packets. We unify on "packet". For SV7,
/// <see cref="Key"/> is <c>null</c>; for SV8 it is the 2-character ASCII key
/// (e.g. <c>"SH"</c>, <c>"AP"</c>, <c>"RG"</c>, <c>"EI"</c>, <c>"SE"</c>,
/// <c>"ST"</c>, <c>"SO"</c>, <c>"CT"</c>). See
/// <c>3rdparty/musepack_src_r475/libmpcdec/mpc_demux.c</c> (the <c>KEY_*</c>
/// macros and <c>mpc_demux_header</c> at line 477) for the full set.
/// </remarks>
public sealed class MpcPacket
{
    internal MpcPacket(string? key, long startOffset, long length, ulong sampleCount)
    {
        Key = key;
        StartOffset = startOffset;
        Length = length;
        SampleCount = sampleCount;
    }

    /// <summary>Gets the SV8 2-character ASCII packet key, or <c>null</c> for SV7 frames.</summary>
    public string? Key { get; }

    /// <summary>Gets the byte offset of this packet from the start of the source stream.</summary>
    public long StartOffset { get; }

    /// <summary>Gets the total length of this packet in bytes (header plus payload).</summary>
    public long Length { get; }

    /// <summary>Gets the number of decoded audio samples this packet contributes (0 for non-audio packets).</summary>
    public ulong SampleCount { get; }
}
