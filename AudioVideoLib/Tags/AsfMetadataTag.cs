namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Discriminator for the value types carried by an ASF Extended Content Description / Metadata /
/// Metadata Library item.
/// </summary>
public enum AsfDataType
{
    /// <summary>UTF-16LE string, null-terminated on disk.</summary>
    UnicodeString = 0,

    /// <summary>Opaque byte array.</summary>
    Bytes = 1,

    /// <summary>32-bit Boolean. Stored as a 4-byte little-endian DWORD on disk.</summary>
    Bool = 2,

    /// <summary>32-bit unsigned little-endian integer.</summary>
    Dword = 3,

    /// <summary>64-bit unsigned little-endian integer.</summary>
    Qword = 4,

    /// <summary>16-bit unsigned little-endian integer.</summary>
    Word = 5,
}

/// <summary>
/// A typed value carried by an ASF Extended Content Description, Metadata, or Metadata Library item.
/// Acts as a tagged union over the six ASF data types.
/// </summary>
public sealed record AsfTypedValue
{
    private readonly string? _stringValue;
    private readonly byte[]? _bytesValue;
    private readonly ulong _scalar;

    private AsfTypedValue(AsfDataType type, string? s, byte[]? b, ulong scalar)
    {
        Type = type;
        _stringValue = s;
        _bytesValue = b;
        _scalar = scalar;
    }

    /// <summary>
    /// Gets the ASF data type carried by this value.
    /// </summary>
    public AsfDataType Type { get; }

    /// <summary>
    /// Creates a Unicode string value.
    /// </summary>
    /// <param name="value">The string. <c>null</c> is treated as an empty string.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromString(string? value) => new(AsfDataType.UnicodeString, value ?? string.Empty, null, 0);

    /// <summary>
    /// Creates a byte-array value.
    /// </summary>
    /// <param name="value">The bytes. <c>null</c> is treated as an empty array.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromBytes(byte[]? value) => new(AsfDataType.Bytes, null, value ?? [], 0);

    /// <summary>
    /// Creates a 32-bit Boolean value.
    /// </summary>
    /// <param name="value">The Boolean.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromBool(bool value) => new(AsfDataType.Bool, null, null, value ? 1UL : 0UL);

    /// <summary>
    /// Creates a 32-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The DWORD.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromDword(uint value) => new(AsfDataType.Dword, null, null, value);

    /// <summary>
    /// Creates a 64-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The QWORD.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromQword(ulong value) => new(AsfDataType.Qword, null, null, value);

    /// <summary>
    /// Creates a 16-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The WORD.</param>
    /// <returns>A new <see cref="AsfTypedValue"/>.</returns>
    public static AsfTypedValue FromWord(ushort value) => new(AsfDataType.Word, null, null, value);

    /// <summary>
    /// Gets the string value when <see cref="Type"/> is <see cref="AsfDataType.UnicodeString"/>; otherwise <c>null</c>.
    /// </summary>
    public string? AsString => Type == AsfDataType.UnicodeString ? _stringValue : null;

    /// <summary>
    /// Gets the byte-array value when <see cref="Type"/> is <see cref="AsfDataType.Bytes"/>; otherwise <c>null</c>.
    /// </summary>
    public byte[]? AsBytes => Type == AsfDataType.Bytes ? _bytesValue : null;

    /// <summary>
    /// Gets the Boolean value when <see cref="Type"/> is <see cref="AsfDataType.Bool"/>; otherwise <c>false</c>.
    /// </summary>
    public bool AsBool => Type == AsfDataType.Bool && _scalar != 0;

    /// <summary>
    /// Gets the DWORD value when <see cref="Type"/> is <see cref="AsfDataType.Dword"/>; otherwise 0.
    /// </summary>
    public uint AsDword => Type == AsfDataType.Dword ? (uint)_scalar : 0;

    /// <summary>
    /// Gets the QWORD value when <see cref="Type"/> is <see cref="AsfDataType.Qword"/>; otherwise 0.
    /// </summary>
    public ulong AsQword => Type == AsfDataType.Qword ? _scalar : 0;

    /// <summary>
    /// Gets the WORD value when <see cref="Type"/> is <see cref="AsfDataType.Word"/>; otherwise 0.
    /// </summary>
    public ushort AsWord => Type == AsfDataType.Word ? (ushort)_scalar : (ushort)0;

    /// <summary>
    /// Serialises the value to its on-disk byte representation (no length prefix).
    /// </summary>
    /// <returns>The on-disk bytes of the value.</returns>
    public byte[] ToByteArray() => Type switch
    {
        AsfDataType.UnicodeString => AsfMetadataTag.EncodeUnicodeString(_stringValue ?? string.Empty),
        AsfDataType.Bytes => _bytesValue is null ? [] : (byte[])_bytesValue.Clone(),
        AsfDataType.Bool => AsfMetadataTag.U32Le((uint)_scalar),
        AsfDataType.Dword => AsfMetadataTag.U32Le((uint)_scalar),
        AsfDataType.Qword => AsfMetadataTag.U64Le(_scalar),
        AsfDataType.Word => AsfMetadataTag.U16Le((ushort)_scalar),
        _ => [],
    };
}

/// <summary>
/// A single Metadata Object (MO) or Metadata Library Object (MLO) item.
/// </summary>
/// <param name="LanguageListIndex">
/// Language list index (always 0 for the Metadata Object; may be non-zero for the Metadata Library Object).
/// Stored on disk in the "reserved / language list index" WORD that precedes the stream number.
/// </param>
/// <param name="StreamNumber">
/// The 0-127 stream number this item is associated with, or 0 to mean "any stream".
/// </param>
/// <param name="Name">The item name (UTF-16LE on disk).</param>
/// <param name="Value">The typed value.</param>
public sealed record AsfMetadataItem(ushort LanguageListIndex, ushort StreamNumber, string Name, AsfTypedValue Value);

/// <summary>
/// Strongly-typed model of the metadata that can appear inside an ASF Header Object: the fixed-schema
/// Content Description Object (CDO), the Extended Content Description Object (ECDO) of typed
/// key/value pairs, and the Metadata Object (MO) / Metadata Library Object (MLO) collections.
/// </summary>
public sealed class AsfMetadataTag
{
    /// <summary>
    /// ASF Header Object GUID: <c>75B22630-668E-11CF-A6D9-00AA0062CE6C</c>.
    /// </summary>
    public static readonly Guid HeaderObjectGuid = new("75B22630-668E-11CF-A6D9-00AA0062CE6C");

    /// <summary>
    /// File Properties Object GUID: <c>8CABDCA1-A947-11CF-8EE4-00C00C205365</c>.
    /// </summary>
    public static readonly Guid FilePropertiesObjectGuid = new("8CABDCA1-A947-11CF-8EE4-00C00C205365");

    /// <summary>
    /// Stream Properties Object GUID: <c>B7DC0791-A9B7-11CF-8EE6-00C00C205365</c>.
    /// </summary>
    public static readonly Guid StreamPropertiesObjectGuid = new("B7DC0791-A9B7-11CF-8EE6-00C00C205365");

    /// <summary>
    /// Content Description Object GUID: <c>75B22633-668E-11CF-A6D9-00AA0062CE6C</c>.
    /// </summary>
    public static readonly Guid ContentDescriptionObjectGuid = new("75B22633-668E-11CF-A6D9-00AA0062CE6C");

    /// <summary>
    /// Extended Content Description Object GUID: <c>D2D0A440-E307-11D2-97F0-00A0C95EA850</c>.
    /// </summary>
    public static readonly Guid ExtendedContentDescriptionObjectGuid = new("D2D0A440-E307-11D2-97F0-00A0C95EA850");

    /// <summary>
    /// Header Extension Object GUID: <c>5FBF03B5-A92E-11CF-8EE3-00C00C205365</c>.
    /// </summary>
    public static readonly Guid HeaderExtensionObjectGuid = new("5FBF03B5-A92E-11CF-8EE3-00C00C205365");

    /// <summary>
    /// Metadata Object GUID: <c>C5F8CBEA-5BAF-4877-8467-AA8C44FA4CCA</c>.
    /// </summary>
    public static readonly Guid MetadataObjectGuid = new("C5F8CBEA-5BAF-4877-8467-AA8C44FA4CCA");

    /// <summary>
    /// Metadata Library Object GUID: <c>44231C94-9498-49D1-A141-1D134E457054</c>.
    /// </summary>
    public static readonly Guid MetadataLibraryObjectGuid = new("44231C94-9498-49D1-A141-1D134E457054");

    private readonly List<KeyValuePair<string, AsfTypedValue>> _extendedItems = [];
    private readonly List<AsfMetadataItem> _metadataItems = [];
    private readonly List<AsfMetadataItem> _metadataLibraryItems = [];

    /// <summary>Gets or sets the Title field from the Content Description Object.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the Author field from the Content Description Object.</summary>
    public string? Author { get; set; }

    /// <summary>Gets or sets the Copyright field from the Content Description Object.</summary>
    public string? Copyright { get; set; }

    /// <summary>Gets or sets the Description field from the Content Description Object.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the Rating field from the Content Description Object.</summary>
    public string? Rating { get; set; }

    /// <summary>
    /// Gets a value indicating whether any field of the Content Description Object is populated.
    /// </summary>
    public bool HasContentDescription =>
        Title is not null || Author is not null || Copyright is not null || Description is not null || Rating is not null;

    /// <summary>
    /// Gets the parsed Extended Content Description Object items as an ordered list of name/value pairs.
    /// Duplicate names are preserved in file order.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, AsfTypedValue>> ExtendedItems => _extendedItems;

    /// <summary>
    /// Gets the Extended Content Description Object items as a dictionary keyed by name. When a name
    /// appears more than once the first occurrence wins; use <see cref="ExtendedItems"/> for the full list.
    /// </summary>
    public IReadOnlyDictionary<string, AsfTypedValue> Extended
    {
        get
        {
            var d = new Dictionary<string, AsfTypedValue>(StringComparer.Ordinal);
            foreach (var kvp in _extendedItems)
            {
                d.TryAdd(kvp.Key, kvp.Value);
            }

            return d;
        }
    }

    /// <summary>
    /// Gets the parsed Metadata Object items.
    /// </summary>
    public IReadOnlyList<AsfMetadataItem> MetadataItems => _metadataItems;

    /// <summary>
    /// Gets the parsed Metadata Library Object items.
    /// </summary>
    public IReadOnlyList<AsfMetadataItem> MetadataLibraryItems => _metadataLibraryItems;

    /// <summary>
    /// Adds an item to the Extended Content Description Object collection.
    /// </summary>
    /// <param name="name">The item name.</param>
    /// <param name="value">The typed value.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
    public void AddExtended(string name, AsfTypedValue value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        _extendedItems.Add(new KeyValuePair<string, AsfTypedValue>(name, value));
    }

    /// <summary>
    /// Adds an item to the Metadata Object collection.
    /// </summary>
    /// <param name="item">The metadata item.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
    public void AddMetadata(AsfMetadataItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _metadataItems.Add(item);
    }

    /// <summary>
    /// Adds an item to the Metadata Library Object collection.
    /// </summary>
    /// <param name="item">The metadata library item.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
    public void AddMetadataLibrary(AsfMetadataItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _metadataLibraryItems.Add(item);
    }

    /// <summary>
    /// Parses a Content Description Object payload (after the 24-byte object header) into this tag.
    /// Sets <see cref="Title"/>, <see cref="Author"/>, <see cref="Copyright"/>, <see cref="Description"/>,
    /// and <see cref="Rating"/>. Returns silently when the payload is too short to hold the five length WORDs.
    /// </summary>
    /// <param name="payload">The CDO payload bytes.</param>
    public void ParseContentDescription(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length < 10)
        {
            return;
        }

        var titleLen = ReadU16(payload, 0);
        var authorLen = ReadU16(payload, 2);
        var copyrightLen = ReadU16(payload, 4);
        var descLen = ReadU16(payload, 6);
        var ratingLen = ReadU16(payload, 8);

        var pos = 10;
        Title = ReadFixedUnicode(payload, ref pos, titleLen);
        Author = ReadFixedUnicode(payload, ref pos, authorLen);
        Copyright = ReadFixedUnicode(payload, ref pos, copyrightLen);
        Description = ReadFixedUnicode(payload, ref pos, descLen);
        Rating = ReadFixedUnicode(payload, ref pos, ratingLen);
    }

    /// <summary>
    /// Parses an Extended Content Description Object payload (after the 24-byte object header).
    /// Items are appended to <see cref="ExtendedItems"/> in file order. Truncated payloads stop the
    /// walk early without throwing.
    /// </summary>
    /// <param name="payload">The ECDO payload bytes.</param>
    public void ParseExtendedContentDescription(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length < 2)
        {
            return;
        }

        int count = ReadU16(payload, 0);
        var pos = 2;
        for (var i = 0; i < count; i++)
        {
            if (pos + 2 > payload.Length)
            {
                return;
            }

            int nameLen = ReadU16(payload, pos);
            pos += 2;
            if (pos + nameLen > payload.Length)
            {
                return;
            }

            var name = DecodeUnicodeStripped(payload, pos, nameLen);
            pos += nameLen;
            if (pos + 4 > payload.Length)
            {
                return;
            }

            int dataType = ReadU16(payload, pos);
            int valueLen = ReadU16(payload, pos + 2);
            pos += 4;
            if (pos + valueLen > payload.Length)
            {
                return;
            }

            var value = DecodeTypedValue(dataType, payload, pos, valueLen);
            pos += valueLen;
            _extendedItems.Add(new KeyValuePair<string, AsfTypedValue>(name, value));
        }
    }

    /// <summary>
    /// Parses a Metadata Object or Metadata Library Object payload (after the 24-byte object header).
    /// </summary>
    /// <param name="payload">The MO / MLO payload bytes.</param>
    /// <param name="library">
    /// <c>true</c> to append parsed items to <see cref="MetadataLibraryItems"/>; <c>false</c> for
    /// <see cref="MetadataItems"/>. Both objects share the same on-disk shape.
    /// </param>
    public void ParseMetadata(byte[] payload, bool library)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length < 2)
        {
            return;
        }

        int count = ReadU16(payload, 0);
        var pos = 2;
        var sink = library ? _metadataLibraryItems : _metadataItems;
        for (var i = 0; i < count; i++)
        {
            // 12 bytes: WORD reserved/lang + WORD streamNum + WORD nameLen + WORD dataType + DWORD valueLen.
            if (pos + 12 > payload.Length)
            {
                return;
            }

            var langIndex = ReadU16(payload, pos);
            var streamNum = ReadU16(payload, pos + 2);
            int nameLen = ReadU16(payload, pos + 4);
            int dataType = ReadU16(payload, pos + 6);
            var valueLenU32 = ReadU32(payload, pos + 8);
            pos += 12;

            // Bound-check: name + value must fit in remainder.
            if (valueLenU32 > (uint)(payload.Length - pos - nameLen))
            {
                return;
            }

            var valueLen = (int)valueLenU32;
            if (pos + nameLen > payload.Length)
            {
                return;
            }

            var name = DecodeUnicodeStripped(payload, pos, nameLen);
            pos += nameLen;
            if (pos + valueLen > payload.Length)
            {
                return;
            }

            var value = DecodeTypedValue(dataType, payload, pos, valueLen);
            pos += valueLen;
            sink.Add(new AsfMetadataItem(langIndex, streamNum, name, value));
        }
    }

    /// <summary>
    /// Serialises the tag's four metadata-bearing objects to byte arrays in canonical order:
    /// CDO, ECDO, MO, MLO. Each entry is a complete ASF object (24-byte header + payload). Objects
    /// whose source collection is empty are omitted.
    /// </summary>
    /// <returns>An array of object byte arrays, possibly empty.</returns>
    public byte[][] ToByteArrays()
    {
        var result = new List<byte[]>(4);
        if (HasContentDescription)
        {
            result.Add(WrapObject(ContentDescriptionObjectGuid, BuildContentDescriptionPayload()));
        }

        if (_extendedItems.Count > 0)
        {
            result.Add(WrapObject(ExtendedContentDescriptionObjectGuid, BuildExtendedContentDescriptionPayload()));
        }

        if (_metadataItems.Count > 0)
        {
            result.Add(WrapObject(MetadataObjectGuid, BuildMetadataPayload(_metadataItems)));
        }

        if (_metadataLibraryItems.Count > 0)
        {
            result.Add(WrapObject(MetadataLibraryObjectGuid, BuildMetadataPayload(_metadataLibraryItems)));
        }

        return [.. result];
    }

    /// <summary>
    /// Builds the Content Description Object payload (no object header).
    /// </summary>
    /// <returns>The CDO payload bytes.</returns>
    public byte[] BuildContentDescriptionPayload()
    {
        var title = EncodeUnicodeString(Title ?? string.Empty);
        var author = EncodeUnicodeString(Author ?? string.Empty);
        var copyright = EncodeUnicodeString(Copyright ?? string.Empty);
        var desc = EncodeUnicodeString(Description ?? string.Empty);
        var rating = EncodeUnicodeString(Rating ?? string.Empty);

        // For null fields, emit a zero length so the field is absent on disk.
        var titleLen = Title is null ? 0 : title.Length;
        var authorLen = Author is null ? 0 : author.Length;
        var copyrightLen = Copyright is null ? 0 : copyright.Length;
        var descLen = Description is null ? 0 : desc.Length;
        var ratingLen = Rating is null ? 0 : rating.Length;

        using var ms = new MemoryStream();
        WriteU16(ms, (ushort)titleLen);
        WriteU16(ms, (ushort)authorLen);
        WriteU16(ms, (ushort)copyrightLen);
        WriteU16(ms, (ushort)descLen);
        WriteU16(ms, (ushort)ratingLen);
        if (Title is not null)
        {
            ms.Write(title, 0, title.Length);
        }

        if (Author is not null)
        {
            ms.Write(author, 0, author.Length);
        }

        if (Copyright is not null)
        {
            ms.Write(copyright, 0, copyright.Length);
        }

        if (Description is not null)
        {
            ms.Write(desc, 0, desc.Length);
        }

        if (Rating is not null)
        {
            ms.Write(rating, 0, rating.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds the Extended Content Description Object payload (no object header).
    /// </summary>
    /// <returns>The ECDO payload bytes.</returns>
    public byte[] BuildExtendedContentDescriptionPayload()
    {
        using var ms = new MemoryStream();
        WriteU16(ms, (ushort)_extendedItems.Count);
        foreach (var kvp in _extendedItems)
        {
            var name = EncodeUnicodeString(kvp.Key);
            var value = kvp.Value.ToByteArray();
            WriteU16(ms, (ushort)name.Length);
            ms.Write(name, 0, name.Length);
            WriteU16(ms, (ushort)kvp.Value.Type);
            WriteU16(ms, (ushort)value.Length);
            ms.Write(value, 0, value.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a Metadata Object or Metadata Library Object payload (no object header).
    /// </summary>
    /// <param name="items">The items to serialise.</param>
    /// <returns>The MO / MLO payload bytes.</returns>
    public static byte[] BuildMetadataPayload(IReadOnlyList<AsfMetadataItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        using var ms = new MemoryStream();
        WriteU16(ms, (ushort)items.Count);
        foreach (var item in items)
        {
            var name = EncodeUnicodeString(item.Name);
            var value = item.Value.ToByteArray();
            WriteU16(ms, item.LanguageListIndex);
            WriteU16(ms, item.StreamNumber);
            WriteU16(ms, (ushort)name.Length);
            WriteU16(ms, (ushort)item.Value.Type);
            WriteU32(ms, (uint)value.Length);
            ms.Write(name, 0, name.Length);
            ms.Write(value, 0, value.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Wraps a payload in a complete ASF object header (16-byte GUID + 8-byte little-endian size).
    /// </summary>
    /// <param name="objectId">The ASF object GUID.</param>
    /// <param name="payload">The payload bytes (excluding the header).</param>
    /// <returns>A byte array of <c>24 + payload.Length</c> bytes.</returns>
    public static byte[] WrapObject(Guid objectId, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var total = 24L + payload.Length;
        var buf = new byte[total];
        var guidBytes = objectId.ToByteArray();
        Buffer.BlockCopy(guidBytes, 0, buf, 0, 16);
        var sizeBytes = U64Le((ulong)total);
        Buffer.BlockCopy(sizeBytes, 0, buf, 16, 8);
        Buffer.BlockCopy(payload, 0, buf, 24, payload.Length);
        return buf;
    }

    /// <summary>
    /// Encodes a string as UTF-16LE bytes followed by a 2-byte null terminator.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public static byte[] EncodeUnicodeString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var raw = Encoding.Unicode.GetBytes(value);
        var buf = new byte[raw.Length + 2];
        Buffer.BlockCopy(raw, 0, buf, 0, raw.Length);
        return buf;
    }

    /// <summary>Returns a 2-byte little-endian encoding of <paramref name="value"/>.</summary>
    /// <param name="value">The 16-bit unsigned value.</param>
    /// <returns>A 2-byte little-endian array.</returns>
    public static byte[] U16Le(ushort value) => [(byte)(value & 0xFF), (byte)((value >> 8) & 0xFF)];

    /// <summary>Returns a 4-byte little-endian encoding of <paramref name="value"/>.</summary>
    /// <param name="value">The 32-bit unsigned value.</param>
    /// <returns>A 4-byte little-endian array.</returns>
    public static byte[] U32Le(uint value) =>
    [
        (byte)(value & 0xFF),
        (byte)((value >> 8) & 0xFF),
        (byte)((value >> 16) & 0xFF),
        (byte)((value >> 24) & 0xFF),
    ];

    /// <summary>Returns an 8-byte little-endian encoding of <paramref name="value"/>.</summary>
    /// <param name="value">The 64-bit unsigned value.</param>
    /// <returns>An 8-byte little-endian array.</returns>
    public static byte[] U64Le(ulong value)
    {
        var b = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            b[i] = (byte)((value >> (8 * i)) & 0xFF);
        }

        return b;
    }

    private static AsfTypedValue DecodeTypedValue(int dataType, byte[] data, int offset, int length)
    {
        return (AsfDataType)dataType switch
        {
            AsfDataType.UnicodeString => AsfTypedValue.FromString(DecodeUnicodeStripped(data, offset, length)),
            AsfDataType.Bool => AsfTypedValue.FromBool(length switch
            {
                >= 4 => ReadU32(data, offset) != 0,
                >= 2 => ReadU16(data, offset) != 0,
                >= 1 => data[offset] != 0,
                _ => false,
            }),
            AsfDataType.Dword => AsfTypedValue.FromDword(length >= 4 ? ReadU32(data, offset) : 0),
            AsfDataType.Qword => AsfTypedValue.FromQword(length >= 8 ? ReadU64(data, offset) : 0),
            AsfDataType.Word => AsfTypedValue.FromWord(length >= 2 ? ReadU16(data, offset) : (ushort)0),
            _ => AsfTypedValue.FromBytes(SliceCopy(data, offset, length)),
        };
    }

    private static byte[] SliceCopy(byte[] src, int offset, int length)
    {
        if (length <= 0)
        {
            return [];
        }

        var dst = new byte[length];
        Buffer.BlockCopy(src, offset, dst, 0, length);
        return dst;
    }

    private static string DecodeUnicodeStripped(byte[] data, int offset, int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        // Strip the trailing UTF-16LE null terminator if present.
        var effective = length;
        if (effective >= 2 && data[offset + effective - 2] == 0 && data[offset + effective - 1] == 0)
        {
            effective -= 2;
        }

        return effective <= 0 ? string.Empty : Encoding.Unicode.GetString(data, offset, effective);
    }

    private static string? ReadFixedUnicode(byte[] data, ref int pos, int length)
    {
        if (length == 0)
        {
            return null;
        }

        if (pos + length > data.Length)
        {
            // Truncated. Return what we can without throwing.
            length = Math.Max(0, data.Length - pos);
        }

        var value = DecodeUnicodeStripped(data, pos, length);
        pos += length;
        return value;
    }

    private static ushort ReadU16(byte[] b, int off) => (ushort)(b[off] | (b[off + 1] << 8));

    private static uint ReadU32(byte[] b, int off) =>
        (uint)(b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24));

    private static ulong ReadU64(byte[] b, int off)
    {
        ulong v = 0;
        for (var i = 7; i >= 0; i--)
        {
            v = (v << 8) | b[off + i];
        }

        return v;
    }

    private static void WriteU16(Stream s, ushort v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
    }

    private static void WriteU32(Stream s, uint v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
        s.WriteByte((byte)((v >> 16) & 0xFF));
        s.WriteByte((byte)((v >> 24) & 0xFF));
    }
}
