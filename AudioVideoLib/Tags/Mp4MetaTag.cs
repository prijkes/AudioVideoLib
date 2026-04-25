namespace AudioVideoLib.Tags;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// Identifies the image format carried by an MP4 <c>covr</c> entry.
/// </summary>
public enum Mp4CoverArtFormat
{
    /// <summary>Unknown / unspecified image format.</summary>
    Unknown = 0,

    /// <summary>JPEG image (data type code 13).</summary>
    Jpeg = 13,

    /// <summary>PNG image (data type code 14).</summary>
    Png = 14,

    /// <summary>BMP image (data type code 27, occasionally seen).</summary>
    Bmp = 27,
}

/// <summary>
/// A single cover-art entry from an MP4 <c>covr</c> atom.
/// </summary>
/// <param name="Data">The raw image bytes.</param>
/// <param name="Format">The detected image format.</param>
public sealed record Mp4CoverArt(byte[] Data, Mp4CoverArtFormat Format);

/// <summary>
/// Identifies a free-form (<c>----</c>) iTunes metadata item by its <c>mean</c> namespace and <c>name</c> key.
/// </summary>
/// <param name="Mean">The reverse-DNS namespace (e.g. <c>com.apple.iTunes</c>).</param>
/// <param name="Name">The field name (e.g. <c>MusicBrainz Track Id</c>).</param>
public readonly record struct Mp4FreeFormKey(string Mean, string Name);

/// <summary>
/// A parsed iTunes-style metadata tag from an MP4 / M4A container's <c>moov.udta.meta.ilst</c> atom.
/// </summary>
/// <remarks>
/// Strongly-typed properties cover the well-known iTunes atoms; everything else (or anything supplied via the
/// <c>----</c> escape hatch) is exposed via <see cref="Items"/> and <see cref="FreeFormItems"/>.
/// Serialisation produces the body of an <c>ilst</c> atom (children only — the caller wraps it in
/// <c>ilst</c> / <c>meta</c> / <c>udta</c> / <c>moov</c>).
/// </remarks>
public sealed class Mp4MetaTag
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    private readonly List<Mp4MetaItem> _items = [];
    private readonly Dictionary<Mp4FreeFormKey, string> _freeForm = [];

    /// <summary>Gets all parsed ilst items, in the order they appeared in the source atom.</summary>
    public IReadOnlyList<Mp4MetaItem> Items => _items;

    /// <summary>Gets the dictionary of free-form (<c>----</c>) items keyed by <c>mean</c> + <c>name</c>.</summary>
    public IReadOnlyDictionary<Mp4FreeFormKey, string> FreeFormItems => _freeForm;

    /// <summary>Gets or sets the title (<c>©nam</c>).</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artist (<c>©ART</c>).</summary>
    public string? Artist { get; set; }

    /// <summary>Gets or sets the album artist (<c>aART</c>).</summary>
    public string? AlbumArtist { get; set; }

    /// <summary>Gets or sets the album (<c>©alb</c>).</summary>
    public string? Album { get; set; }

    /// <summary>Gets or sets the year / release date (<c>©day</c>).</summary>
    public string? Year { get; set; }

    /// <summary>
    /// Gets or sets the genre. Sourced from <c>©gen</c> if present, otherwise a numeric mapping
    /// from <c>gnre</c> (1-based ID3v1 genre index, surfaced as the raw integer in string form).
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>Gets or sets the comment (<c>©cmt</c>).</summary>
    public string? Comment { get; set; }

    /// <summary>Gets or sets the composer (<c>©wrt</c>).</summary>
    public string? Composer { get; set; }

    /// <summary>Gets or sets the encoding tool / encoder (<c>©too</c>).</summary>
    public string? Tool { get; set; }

    /// <summary>Gets or sets the track number (<c>trkn</c>).</summary>
    public int? TrackNumber { get; set; }

    /// <summary>Gets or sets the total number of tracks (<c>trkn</c>).</summary>
    public int? TrackTotal { get; set; }

    /// <summary>Gets or sets the disc number (<c>disk</c>).</summary>
    public int? DiscNumber { get; set; }

    /// <summary>Gets or sets the total number of discs (<c>disk</c>).</summary>
    public int? DiscTotal { get; set; }

    /// <summary>Gets or sets the BPM (<c>tmpo</c>).</summary>
    public int? Bpm { get; set; }

    /// <summary>Gets or sets the compilation flag (<c>cpil</c>).</summary>
    public bool? Compilation { get; set; }

    /// <summary>Gets the cover art entries (<c>covr</c>); multiple entries are supported.</summary>
    public IList<Mp4CoverArt> CoverArt { get; } = [];

    /// <summary>
    /// Sets or removes a free-form item identified by its <c>mean</c> namespace and <c>name</c> key.
    /// </summary>
    /// <param name="mean">The reverse-DNS namespace (e.g. <c>com.apple.iTunes</c>).</param>
    /// <param name="name">The field name.</param>
    /// <param name="value">The value, or <c>null</c> to remove.</param>
    public void SetFreeFormItem(string mean, string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(mean);
        ArgumentNullException.ThrowIfNull(name);

        var key = new Mp4FreeFormKey(mean, name);
        if (value is null)
        {
            _freeForm.Remove(key);
        }
        else
        {
            _freeForm[key] = value;
        }
    }

    /// <summary>
    /// Parses the body of an <c>ilst</c> atom (children only — no surrounding <c>ilst</c> header) into a tag.
    /// </summary>
    /// <param name="payload">The raw ilst payload bytes.</param>
    /// <returns>A parsed tag — possibly empty if no recognised children were found.</returns>
    public static Mp4MetaTag Parse(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Parse((ReadOnlySpan<byte>)payload);
    }

    /// <summary>
    /// Parses the body of an <c>ilst</c> atom (children only — no surrounding <c>ilst</c> header) into a tag.
    /// </summary>
    /// <param name="payload">The raw ilst payload bytes.</param>
    /// <returns>A parsed tag — possibly empty if no recognised children were found.</returns>
    /// <remarks>
    /// Span-based overload. Lets callers pass slices of an existing buffer
    /// (<see cref="ArrayPool{T}"/>-rented, memory-mapped, etc.) without
    /// allocating an intermediate <see cref="T:byte[]"/>.
    /// </remarks>
    public static Mp4MetaTag Parse(ReadOnlySpan<byte> payload)
    {
        var tag = new Mp4MetaTag();
        var pos = 0;
        while (pos + 8 <= payload.Length)
        {
            var size = ReadBeU32(payload, pos);
            if (size < 8 || pos + size > payload.Length)
            {
                break;
            }

            var atomType = Latin1.GetString(payload.Slice(pos + 4, 4));
            var childStart = pos + 8;
            var childEnd = pos + (int)size;
            tag.AbsorbItem(atomType, payload, childStart, childEnd);
            pos = childEnd;
        }

        return tag;
    }

    /// <summary>
    /// Serialises the tag's strongly-typed properties and free-form items into the body of an <c>ilst</c> atom
    /// (children only — the caller is responsible for wrapping the result in an <c>ilst</c> + <c>meta</c> + <c>udta</c> + <c>moov</c> chain).
    /// </summary>
    public byte[] ToByteArray()
    {
        using var ms = new MemoryStream();
        WriteTextAtom(ms, "\u00A9nam", Title);
        WriteTextAtom(ms, "\u00A9ART", Artist);
        WriteTextAtom(ms, "aART", AlbumArtist);
        WriteTextAtom(ms, "\u00A9alb", Album);
        WriteTextAtom(ms, "\u00A9day", Year);
        WriteTextAtom(ms, "\u00A9gen", Genre);
        WriteTextAtom(ms, "\u00A9cmt", Comment);
        WriteTextAtom(ms, "\u00A9wrt", Composer);
        WriteTextAtom(ms, "\u00A9too", Tool);

        if (TrackNumber is not null || TrackTotal is not null)
        {
            WriteDataAtom(ms, "trkn", Mp4DataType.Implicit, Mp4MetaItem.BuildIndexTotalPayload(TrackNumber ?? 0, TrackTotal ?? 0));
        }

        if (DiscNumber is not null || DiscTotal is not null)
        {
            WriteDataAtom(ms, "disk", Mp4DataType.Implicit, Mp4MetaItem.BuildIndexTotalPayload(DiscNumber ?? 0, DiscTotal ?? 0));
        }

        if (Bpm is not null)
        {
            WriteDataAtom(ms, "tmpo", Mp4DataType.BeSignedInt, [(byte)((Bpm.Value >> 8) & 0xFF), (byte)(Bpm.Value & 0xFF)]);
        }

        if (Compilation is not null)
        {
            WriteDataAtom(ms, "cpil", Mp4DataType.BeSignedInt, [(byte)(Compilation.Value ? 1 : 0)]);
        }

        foreach (var cover in CoverArt)
        {
            var dataType = cover.Format switch
            {
                Mp4CoverArtFormat.Jpeg => Mp4DataType.Jpeg,
                Mp4CoverArtFormat.Png => Mp4DataType.Png,
                _ => (Mp4DataType)cover.Format,
            };
            WriteDataAtom(ms, "covr", dataType, cover.Data);
        }

        foreach (var kv in _freeForm)
        {
            WriteFreeFormAtom(ms, kv.Key.Mean, kv.Key.Name, kv.Value);
        }

        return ms.ToArray();
    }

    private void AbsorbItem(string atomType, ReadOnlySpan<byte> payload, int start, int end)
    {
        if (atomType == "----")
        {
            AbsorbFreeForm(payload, start, end);
            return;
        }

        if (!TryReadDataChild(payload, start, end, out var dataType, out var dataStart, out var dataLen))
        {
            return;
        }

        var item = new Mp4MetaItem(atomType, dataType, payload.Slice(dataStart, dataLen).ToArray());
        _items.Add(item);

        switch (atomType)
        {
            case "\u00A9nam":
                Title = item.AsUtf8String();
                break;
            case "\u00A9ART":
                Artist = item.AsUtf8String();
                break;
            case "aART":
                AlbumArtist = item.AsUtf8String();
                break;
            case "\u00A9alb":
                Album = item.AsUtf8String();
                break;
            case "\u00A9day":
                Year = item.AsUtf8String();
                break;
            case "\u00A9gen":
                Genre = item.AsUtf8String();
                break;
            case "gnre":
                Genre ??= item.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "\u00A9cmt":
                Comment = item.AsUtf8String();
                break;
            case "\u00A9wrt":
                Composer = item.AsUtf8String();
                break;
            case "\u00A9too":
                Tool = item.AsUtf8String();
                break;
            case "trkn":
                {
                    var (idx, tot) = item.AsIndexTotalPair();
                    TrackNumber = idx;
                    TrackTotal = tot;
                }

                break;
            case "disk":
                {
                    var (idx, tot) = item.AsIndexTotalPair();
                    DiscNumber = idx;
                    DiscTotal = tot;
                }

                break;
            case "tmpo":
                Bpm = (int)item.AsInteger();
                break;
            case "cpil":
                Compilation = item.AsInteger() != 0;
                break;
            case "covr":
                {
                    var fmt = dataType switch
                    {
                        Mp4DataType.Jpeg => Mp4CoverArtFormat.Jpeg,
                        Mp4DataType.Png => Mp4CoverArtFormat.Png,
                        _ => SniffImageFormat(item.Payload),
                    };
                    CoverArt.Add(new Mp4CoverArt(item.Payload, fmt));
                }

                break;
        }
    }

    private void AbsorbFreeForm(ReadOnlySpan<byte> payload, int start, int end)
    {
        string? mean = null;
        string? name = null;
        var dataType = Mp4DataType.Implicit;
        byte[] data = [];

        var pos = start;
        while (pos + 8 <= end)
        {
            var size = ReadBeU32(payload, pos);
            if (size < 8 || pos + size > end)
            {
                break;
            }

            var subType = Latin1.GetString(payload.Slice(pos + 4, 4));
            var bodyStart = pos + 8;
            var bodyEnd = pos + (int)size;
            switch (subType)
            {
                case "mean" when bodyEnd - bodyStart >= 4:
                    mean = Encoding.UTF8.GetString(payload.Slice(bodyStart + 4, bodyEnd - bodyStart - 4));
                    break;
                case "name" when bodyEnd - bodyStart >= 4:
                    name = Encoding.UTF8.GetString(payload.Slice(bodyStart + 4, bodyEnd - bodyStart - 4));
                    break;
                case "data" when bodyEnd - bodyStart >= 8:
                    dataType = (Mp4DataType)ReadBeU32(payload, bodyStart);
                    data = payload.Slice(bodyStart + 8, bodyEnd - bodyStart - 8).ToArray();
                    break;
            }

            pos = bodyEnd;
        }

        if (mean is null || name is null)
        {
            return;
        }

        var item = new Mp4MetaItem("----", dataType, data, mean, name);
        _items.Add(item);
        _freeForm[new Mp4FreeFormKey(mean, name)] = Encoding.UTF8.GetString(data);
    }

    private static bool TryReadDataChild(ReadOnlySpan<byte> payload, int start, int end, out Mp4DataType dataType, out int dataStart, out int dataLen)
    {
        var pos = start;
        while (pos + 8 <= end)
        {
            var size = ReadBeU32(payload, pos);
            if (size < 8 || pos + size > end)
            {
                break;
            }

            var subType = Latin1.GetString(payload.Slice(pos + 4, 4));
            var bodyStart = pos + 8;
            var bodyEnd = pos + (int)size;
            if (subType == "data" && bodyEnd - bodyStart >= 8)
            {
                dataType = (Mp4DataType)ReadBeU32(payload, bodyStart);
                dataStart = bodyStart + 8;
                dataLen = bodyEnd - dataStart;
                return true;
            }

            pos = bodyEnd;
        }

        dataType = Mp4DataType.Implicit;
        dataStart = 0;
        dataLen = 0;
        return false;
    }

    private static void WriteTextAtom(Stream s, string atomType, string? value)
    {
        if (value is null)
        {
            return;
        }

        WriteDataAtom(s, atomType, Mp4DataType.Utf8, Encoding.UTF8.GetBytes(value));
    }

    private static void WriteDataAtom(Stream s, string atomType, Mp4DataType dataType, byte[] payload)
    {
        // ilst child:  size(4) + type(4) + [ data sub-atom: size(4) + 'data'(4) + dataType(4) + locale(4) + payload ]
        var dataAtomSize = 8 + 8 + payload.Length;
        var outerSize = 8 + dataAtomSize;
        WriteBeU32(s, (uint)outerSize);
        WriteFourCc(s, atomType);
        WriteBeU32(s, (uint)dataAtomSize);
        WriteFourCc(s, "data");
        WriteBeU32(s, (uint)dataType);
        WriteBeU32(s, 0);
        s.Write(payload, 0, payload.Length);
    }

    private static void WriteFreeFormAtom(Stream s, string mean, string name, string value)
    {
        var meanBytes = Encoding.UTF8.GetBytes(mean);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var valueBytes = Encoding.UTF8.GetBytes(value);

        var meanAtomSize = 8 + 4 + meanBytes.Length;
        var nameAtomSize = 8 + 4 + nameBytes.Length;
        var dataAtomSize = 8 + 8 + valueBytes.Length;
        var outerSize = 8 + meanAtomSize + nameAtomSize + dataAtomSize;

        WriteBeU32(s, (uint)outerSize);
        WriteFourCc(s, "----");

        WriteBeU32(s, (uint)meanAtomSize);
        WriteFourCc(s, "mean");
        WriteBeU32(s, 0);
        s.Write(meanBytes, 0, meanBytes.Length);

        WriteBeU32(s, (uint)nameAtomSize);
        WriteFourCc(s, "name");
        WriteBeU32(s, 0);
        s.Write(nameBytes, 0, nameBytes.Length);

        WriteBeU32(s, (uint)dataAtomSize);
        WriteFourCc(s, "data");
        WriteBeU32(s, (uint)Mp4DataType.Utf8);
        WriteBeU32(s, 0);
        s.Write(valueBytes, 0, valueBytes.Length);
    }

    private static Mp4CoverArtFormat SniffImageFormat(byte[] data) =>
        data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF
            ? Mp4CoverArtFormat.Jpeg
            : data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                ? Mp4CoverArtFormat.Png
                : Mp4CoverArtFormat.Unknown;

    private static uint ReadBeU32(ReadOnlySpan<byte> b, int off) =>
        ((uint)b[off] << 24) | ((uint)b[off + 1] << 16) | ((uint)b[off + 2] << 8) | b[off + 3];

    private static void WriteBeU32(Stream s, uint v)
    {
        s.WriteByte((byte)((v >> 24) & 0xFF));
        s.WriteByte((byte)((v >> 16) & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
        s.WriteByte((byte)(v & 0xFF));
    }

    private static void WriteFourCc(Stream s, string fourCc)
    {
        var bytes = Latin1.GetBytes(fourCc);
        if (bytes.Length != 4)
        {
            throw new ArgumentException("Four-CC must encode to exactly 4 bytes.", nameof(fourCc));
        }

        s.Write(bytes, 0, 4);
    }

}
