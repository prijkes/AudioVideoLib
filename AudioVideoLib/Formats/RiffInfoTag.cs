namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// A parsed WAV <c>LIST</c> chunk with form-type <c>INFO</c>: the RIFF text-metadata block.
/// </summary>
/// <remarks>
/// The standard four-character codes (<c>INAM</c>, <c>IART</c>, <c>IPRD</c>, <c>ICRD</c>, <c>ICMT</c>,
/// <c>IGNR</c>, <c>ITRK</c>, <c>IENG</c>, <c>ISFT</c>, <c>ICOP</c>) are surfaced as named properties.
/// All other sub-chunks fall through to <see cref="Items"/>.
/// </remarks>
public sealed class RiffInfoTag
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);

    /// <summary>Gets the title (<c>INAM</c>).</summary>
    public string? Title => GetValue("INAM");

    /// <summary>Gets the artist (<c>IART</c>).</summary>
    public string? Artist => GetValue("IART");

    /// <summary>Gets the product / album (<c>IPRD</c>).</summary>
    public string? Product => GetValue("IPRD");

    /// <summary>Gets the creation date (<c>ICRD</c>).</summary>
    public string? CreationDate => GetValue("ICRD");

    /// <summary>Gets the comment (<c>ICMT</c>).</summary>
    public string? Comment => GetValue("ICMT");

    /// <summary>Gets the genre (<c>IGNR</c>).</summary>
    public string? Genre => GetValue("IGNR");

    /// <summary>Gets the track number (<c>ITRK</c>).</summary>
    public string? Track => GetValue("ITRK");

    /// <summary>Gets the engineer (<c>IENG</c>).</summary>
    public string? Engineer => GetValue("IENG");

    /// <summary>Gets the software (<c>ISFT</c>).</summary>
    public string? Software => GetValue("ISFT");

    /// <summary>Gets the copyright (<c>ICOP</c>).</summary>
    public string? Copyright => GetValue("ICOP");

    /// <summary>
    /// Gets the full set of parsed key/value items, keyed by four-character chunk id.
    /// </summary>
    public IReadOnlyDictionary<string, string> Items => _items;

    /// <summary>
    /// Sets or replaces a single item by its four-character id. Use <c>null</c> to remove.
    /// </summary>
    /// <param name="fourCc">A four-character chunk id (e.g. <c>"INAM"</c>).</param>
    /// <param name="value">The value, or <c>null</c> to remove.</param>
    public void SetItem(string fourCc, string? value)
    {
        ArgumentNullException.ThrowIfNull(fourCc);
        if (fourCc.Length != 4)
        {
            throw new ArgumentException("Four-CC must be exactly 4 characters.", nameof(fourCc));
        }

        if (value is null)
        {
            _items.Remove(fourCc);
        }
        else
        {
            _items[fourCc] = value;
        }
    }

    /// <summary>
    /// Parses a <c>LIST</c> chunk payload (form-type <c>INFO</c> already consumed) into a tag,
    /// or returns <c>null</c> for malformed input.
    /// </summary>
    /// <param name="payload">The raw payload bytes following the 4-byte form-type field.</param>
    public static RiffInfoTag? Parse(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var tag = new RiffInfoTag();
        var pos = 0;
        while (pos + 8 <= payload.Length)
        {
            var id = Latin1.GetString(payload, pos, 4);
            var size = (uint)(payload[pos + 4] | (payload[pos + 5] << 8) | (payload[pos + 6] << 16) | (payload[pos + 7] << 24));
            pos += 8;

            if (size > (uint)(payload.Length - pos))
            {
                return tag._items.Count > 0 ? tag : null;
            }

            var rawLen = (int)size;
            var trimLen = rawLen;
            while (trimLen > 0 && payload[pos + trimLen - 1] == 0)
            {
                trimLen--;
            }

            tag._items[id] = Latin1.GetString(payload, pos, trimLen);
            pos += rawLen;
            if ((rawLen & 1) != 0 && pos < payload.Length)
            {
                pos++;
            }
        }

        return tag;
    }

    /// <summary>
    /// Parses a full WAV <c>LIST</c> chunk payload — including the leading 4-byte form-type — into
    /// a tag, or returns <c>null</c> if the form-type is not <c>INFO</c> or input is malformed.
    /// </summary>
    /// <param name="listPayload">The raw <c>LIST</c> chunk payload (starts with the 4-byte form-type).</param>
    public static RiffInfoTag? FromListPayload(byte[] listPayload)
    {
        ArgumentNullException.ThrowIfNull(listPayload);
        if (listPayload.Length < 4)
        {
            return null;
        }

        var formType = Latin1.GetString(listPayload, 0, 4);
        if (formType != "INFO")
        {
            return null;
        }

        var inner = new byte[listPayload.Length - 4];
        Array.Copy(listPayload, 4, inner, 0, inner.Length);
        return Parse(inner);
    }

    /// <summary>
    /// Serialises the tag's sub-chunks (without the enclosing <c>LIST</c> header or form-type).
    /// Sub-chunk values are written as null-terminated ASCII and word-aligned.
    /// </summary>
    public byte[] ToByteArray()
    {
        using var ms = new MemoryStream();
        foreach (var kvp in _items)
        {
            var idBytes = Encoding.ASCII.GetBytes(kvp.Key.PadRight(4)[..4]);
            var valueBytes = Encoding.ASCII.GetBytes(kvp.Value ?? string.Empty);
            var size = (uint)(valueBytes.Length + 1);
            ms.Write(idBytes, 0, 4);
            ms.WriteByte((byte)(size & 0xFF));
            ms.WriteByte((byte)((size >> 8) & 0xFF));
            ms.WriteByte((byte)((size >> 16) & 0xFF));
            ms.WriteByte((byte)((size >> 24) & 0xFF));
            ms.Write(valueBytes, 0, valueBytes.Length);
            ms.WriteByte(0);
            if ((size & 1) != 0)
            {
                ms.WriteByte(0);
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Serialises the tag wrapped in a full <c>LIST INFO</c> chunk (8-byte header + form-type + items).
    /// </summary>
    public byte[] ToListChunkBytes()
    {
        var inner = ToByteArray();
        var payload = new byte[4 + inner.Length];
        Encoding.ASCII.GetBytes("INFO", 0, 4, payload, 0);
        Array.Copy(inner, 0, payload, 4, inner.Length);

        var size = (uint)payload.Length;
        var pad = (size & 1) == 1 ? 1 : 0;
        var result = new byte[8 + payload.Length + pad];
        Encoding.ASCII.GetBytes("LIST", 0, 4, result, 0);
        result[4] = (byte)(size & 0xFF);
        result[5] = (byte)((size >> 8) & 0xFF);
        result[6] = (byte)((size >> 16) & 0xFF);
        result[7] = (byte)((size >> 24) & 0xFF);
        Array.Copy(payload, 0, result, 8, payload.Length);
        return result;
    }

    private string? GetValue(string fourCc) => _items.TryGetValue(fourCc, out var v) ? v : null;
}
