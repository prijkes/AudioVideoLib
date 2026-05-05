namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Summary of one WavPack metadata sub-block — the id-prefixed records embedded
/// inside a block's post-header payload. Layout per
/// <c>3rdparty/WavPack/src/open_utils.c:read_metadata_buff</c> (lines 713-754).
/// </summary>
/// <remarks>
/// Sub-block IDs are documented in <c>3rdparty/WavPack/include/wavpack.h:158-193</c>.
/// <c>ID_LARGE (0x80)</c> and <c>ID_ODD_SIZE (0x40)</c> are flag bits; the unique ID
/// occupies the lower 6 bits (mask <c>0x3F</c>). <c>ID_OPTIONAL_DATA (0x20)</c> is part
/// of that unique-ID space — for example <c>ID_RIFF_HEADER = 0x21 = ID_OPTIONAL_DATA | 1</c>.
/// Callers can compare <see cref="RawId"/> (or <see cref="UniqueId"/>) directly against
/// the named constants.
/// </remarks>
public sealed class WavPackSubBlock
{
    private readonly ISourceReader _source;

    internal WavPackSubBlock(ISourceReader source, byte rawId, long payloadOffset, int payloadLength)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        RawId = rawId;
        PayloadOffset = payloadOffset;
        PayloadLength = payloadLength;
    }

    /// <summary>Gets the raw id byte (including the <c>ID_LARGE</c> and <c>ID_ODD_SIZE</c> flag bits).</summary>
    public byte RawId { get; }

    /// <summary>Gets the unique id with the two flag bits cleared (bit 7, bit 6). The <c>ID_OPTIONAL_DATA</c> bit (0x20) is part of the unique-id space and is preserved.</summary>
    public byte UniqueId => (byte)(RawId & 0x3F);

    /// <summary>Gets a value indicating whether the <c>ID_OPTIONAL_DATA</c> band is set.</summary>
    public bool IsOptional => (RawId & 0x20) != 0;

    /// <summary>Gets a value indicating whether the <c>ID_LARGE</c> bit is set (i.e., the on-disk size header was 24-bit).</summary>
    public bool IsLargeSize => (RawId & 0x80) != 0;

    /// <summary>Gets the file offset (within the source stream) of the first payload byte.</summary>
    public long PayloadOffset { get; }

    /// <summary>Gets the logical payload length in bytes — already accounts for <c>ID_ODD_SIZE</c>.</summary>
    public int PayloadLength { get; }

    /// <summary>
    /// Reads the sub-block payload bytes from the source. The source must be alive —
    /// after the owning <c>WavPackStream</c> is disposed, this method throws.
    /// </summary>
    /// <returns>The freshly-read payload bytes (length <see cref="PayloadLength"/>).</returns>
    public byte[] ReadPayload()
    {
        var buf = new byte[PayloadLength];
        if (PayloadLength > 0)
        {
            _source.Read(PayloadOffset, buf);
        }

        return buf;
    }
}
