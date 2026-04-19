namespace AudioVideoLib.Formats;

using System;
using System.Text;

/// <summary>
/// Identifies how the payload of an <c>ilst</c> item's <c>data</c> sub-atom is encoded.
/// </summary>
/// <remarks>
/// Values follow the type codes defined by the QuickTime / iTunes metadata format. Only the
/// values needed for audio metadata are listed; unknown codes are reported verbatim.
/// </remarks>
public enum Mp4DataType
{
    /// <summary>Reserved / implicit type — payload format is implied by the parent atom name.</summary>
    Implicit = 0,

    /// <summary>UTF-8 text without null terminator.</summary>
    Utf8 = 1,

    /// <summary>UTF-16 text without null terminator.</summary>
    Utf16 = 2,

    /// <summary>JPEG image.</summary>
    Jpeg = 13,

    /// <summary>PNG image.</summary>
    Png = 14,

    /// <summary>Big-endian signed integer (1, 2, 4, or 8 bytes).</summary>
    BeSignedInt = 21,

    /// <summary>Big-endian unsigned integer (1, 2, 4, or 8 bytes).</summary>
    BeUnsignedInt = 22,
}

/// <summary>
/// A single child of an <c>ilst</c> atom (one iTunes metadata field).
/// </summary>
/// <remarks>
/// Each ilst child wraps a single <c>data</c> sub-atom that carries:
/// 4-byte type code, 4-byte locale (skipped), then the payload. Free-form
/// <c>----</c> items also carry <c>mean</c> and <c>name</c> sub-atoms that
/// identify the namespace and key.
/// </remarks>
public sealed class Mp4MetaItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Mp4MetaItem"/> class.
    /// </summary>
    /// <param name="atomType">The four-character atom type (e.g. <c>"©nam"</c>, <c>"trkn"</c>, <c>"covr"</c>, <c>"----"</c>).</param>
    /// <param name="dataType">The <c>data</c> sub-atom's type code.</param>
    /// <param name="payload">The raw payload bytes from the <c>data</c> sub-atom (after the 8-byte data-atom header).</param>
    /// <param name="mean">For free-form <c>----</c> items, the contents of the <c>mean</c> sub-atom; otherwise <c>null</c>.</param>
    /// <param name="name">For free-form <c>----</c> items, the contents of the <c>name</c> sub-atom; otherwise <c>null</c>.</param>
    public Mp4MetaItem(string atomType, Mp4DataType dataType, byte[] payload, string? mean = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(atomType);
        ArgumentNullException.ThrowIfNull(payload);

        AtomType = atomType;
        DataType = dataType;
        Payload = payload;
        Mean = mean;
        Name = name;
    }

    /// <summary>Gets the four-character atom type.</summary>
    public string AtomType { get; }

    /// <summary>Gets the <c>data</c> sub-atom's type code.</summary>
    public Mp4DataType DataType { get; }

    /// <summary>Gets the raw payload bytes.</summary>
    public byte[] Payload { get; }

    /// <summary>Gets the <c>mean</c> string for free-form items (e.g. <c>"com.apple.iTunes"</c>); <c>null</c> for standard items.</summary>
    public string? Mean { get; }

    /// <summary>Gets the <c>name</c> string for free-form items (e.g. <c>"MusicBrainz Track Id"</c>); <c>null</c> for standard items.</summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the payload decoded as a UTF-8 string, regardless of the declared type.
    /// </summary>
    public string AsUtf8String() => Encoding.UTF8.GetString(Payload);

    /// <summary>
    /// Decodes the payload as a big-endian integer of width 1, 2, 4, or 8 bytes.
    /// </summary>
    /// <returns>The decoded value, or 0 if the payload length is unsupported.</returns>
    public long AsInteger()
    {
        var p = Payload;
        return p.Length switch
        {
            1 => p[0],
            2 => (p[0] << 8) | p[1],
            4 => ((long)p[0] << 24) | ((long)p[1] << 16) | ((long)p[2] << 8) | p[3],
            8 => ((long)p[0] << 56) | ((long)p[1] << 48) | ((long)p[2] << 40) | ((long)p[3] << 32)
                 | ((long)p[4] << 24) | ((long)p[5] << 16) | ((long)p[6] << 8) | p[7],
            _ => 0,
        };
    }

    /// <summary>
    /// Decodes a <c>trkn</c> / <c>disk</c>-style payload: 8 bytes of <c>reserved/index/total/reserved</c> as four big-endian int16 values.
    /// </summary>
    /// <returns>The (index, total) pair, or (0, 0) if the payload is too short.</returns>
    public (int Index, int Total) AsIndexTotalPair()
    {
        var p = Payload;
        if (p.Length < 6)
        {
            return (0, 0);
        }

        var index = (p[2] << 8) | p[3];
        var total = (p[4] << 8) | p[5];
        return (index, total);
    }

    /// <summary>
    /// Builds a payload encoding the <c>trkn</c> / <c>disk</c> layout: 4 × big-endian int16 (<c>reserved/index/total/reserved</c>).
    /// </summary>
    /// <param name="index">The track or disc number.</param>
    /// <param name="total">The total number of tracks or discs.</param>
    public static byte[] BuildIndexTotalPayload(int index, int total) =>
    [
        0, 0,
        (byte)((index >> 8) & 0xFF), (byte)(index & 0xFF),
        (byte)((total >> 8) & 0xFF), (byte)(total & 0xFF),
        0, 0,
    ];
}
