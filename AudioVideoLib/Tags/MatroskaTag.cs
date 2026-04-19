namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;

/// <summary>
/// EBML element ids that appear inside the Matroska <c>Tags</c> sub-tree.
/// </summary>
public static class MatroskaElementIds
{
    /// <summary>Tags element id.</summary>
    public const long Tags = 0x1254C367;

    /// <summary>Tag element id.</summary>
    public const long Tag = 0x7373;

    /// <summary>Targets element id.</summary>
    public const long Targets = 0x63C0;

    /// <summary>TargetTypeValue element id (uint).</summary>
    public const long TargetTypeValue = 0x68CA;

    /// <summary>TargetType element id (ASCII string).</summary>
    public const long TargetType = 0x63CA;

    /// <summary>TagTrackUID element id (uint, may repeat).</summary>
    public const long TagTrackUid = 0x63C5;

    /// <summary>TagEditionUID element id (uint, may repeat).</summary>
    public const long TagEditionUid = 0x63C9;

    /// <summary>TagChapterUID element id (uint, may repeat).</summary>
    public const long TagChapterUid = 0x63C4;

    /// <summary>TagAttachmentUID element id (uint, may repeat).</summary>
    public const long TagAttachmentUid = 0x63C6;

    /// <summary>SimpleTag element id.</summary>
    public const long SimpleTag = 0x67C8;

    /// <summary>TagName element id (UTF-8 string).</summary>
    public const long TagName = 0x45A3;

    /// <summary>TagLanguage element id (ASCII).</summary>
    public const long TagLanguage = 0x447A;

    /// <summary>TagLanguageBCP47 element id (ASCII).</summary>
    public const long TagLanguageBcp47 = 0x447B;

    /// <summary>TagDefault element id (uint 0/1).</summary>
    public const long TagDefault = 0x4484;

    /// <summary>TagString element id (UTF-8).</summary>
    public const long TagString = 0x4487;

    /// <summary>TagBinary element id (bytes).</summary>
    public const long TagBinary = 0x4485;
}

/// <summary>
/// The Matroska <c>Targets</c> element, identifying the level of metadata (album, track, ...) and the UIDs it applies to.
/// </summary>
public sealed class MatroskaTargets
{
    /// <summary>Gets or sets the target type value (e.g. 50 = album, 30 = track). 50 is the spec default.</summary>
    public ulong TargetTypeValue { get; set; } = 50;

    /// <summary>Gets or sets the optional textual target type (e.g. <c>"ALBUM"</c>).</summary>
    public string? TargetType { get; set; }

    /// <summary>Gets the list of track UIDs this tag applies to.</summary>
    public IList<ulong> TrackUids { get; } = [];

    /// <summary>Gets the list of edition UIDs this tag applies to.</summary>
    public IList<ulong> EditionUids { get; } = [];

    /// <summary>Gets the list of chapter UIDs this tag applies to.</summary>
    public IList<ulong> ChapterUids { get; } = [];

    /// <summary>Gets the list of attachment UIDs this tag applies to.</summary>
    public IList<ulong> AttachmentUids { get; } = [];
}

/// <summary>
/// A single SimpleTag entry inside a <see cref="MatroskaTagEntry"/>. SimpleTags can nest.
/// </summary>
public sealed class MatroskaSimpleTag
{
    /// <summary>Gets or sets the tag name (uppercase by convention, e.g. <c>"TITLE"</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag value (UTF-8 string). Mutually exclusive with <see cref="Binary"/>.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the language (ISO 639-2, e.g. <c>"eng"</c>; defaults to <c>"und"</c> per spec).</summary>
    public string Language { get; set; } = "und";

    /// <summary>Gets or sets the BCP-47 language tag (newer Matroska revisions). <c>null</c> when omitted.</summary>
    public string? LanguageBcp47 { get; set; }

    /// <summary>Gets or sets a value indicating whether this entry is the default for its language.</summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>Gets or sets the binary payload. Mutually exclusive with <see cref="Value"/>.</summary>
    public byte[]? Binary { get; set; }

    /// <summary>Gets the nested SimpleTag children.</summary>
    public IList<MatroskaSimpleTag> SimpleTags { get; } = [];
}

/// <summary>
/// A single Matroska <c>Tag</c> element: a <see cref="MatroskaTargets"/> + a list of <see cref="MatroskaSimpleTag"/>.
/// </summary>
public sealed class MatroskaTagEntry
{
    /// <summary>Gets or sets the targets.</summary>
    public MatroskaTargets Targets { get; set; } = new();

    /// <summary>Gets the simple tags.</summary>
    public IList<MatroskaSimpleTag> SimpleTags { get; } = [];
}

/// <summary>
/// Aggregates every top-level <c>Tag</c> entry seen inside a Matroska / WebM segment's <c>Tags</c> element.
/// </summary>
/// <remarks>
/// Provides serialisation back to the Matroska <c>Tags</c> element bytes for round-trip writers, plus a small set of
/// strongly-typed accessors that look up well-known SimpleTag names by EBML metadata convention.
/// </remarks>
public sealed class MatroskaTag
{
    /// <summary>The Matroska album-level <c>TargetTypeValue</c>.</summary>
    public const int AlbumLevel = 50;

    /// <summary>The Matroska track-level <c>TargetTypeValue</c>.</summary>
    public const int TrackLevel = 30;

    /// <summary>Gets the parsed Tag entries, one per top-level <c>Tag</c> element in the segment.</summary>
    public IList<MatroskaTagEntry> Entries { get; } = [];

    /// <summary>Gets the album title, falling back to the track title if no album-level title exists.</summary>
    public string? Title => FindAlbumOrTrack("TITLE");

    /// <summary>Gets the artist (album-level preferred, track-level fallback).</summary>
    public string? Artist => FindAlbumOrTrack("ARTIST");

    /// <summary>Gets the album name (album-level only).</summary>
    public string? Album => FindByTarget("TITLE", AlbumLevel);

    /// <summary>Gets the recording date (album-level preferred, track-level fallback).</summary>
    public string? Date => FindAlbumOrTrack("DATE_RELEASED") ?? FindAlbumOrTrack("DATE_RECORDED") ?? FindAlbumOrTrack("DATE");

    /// <summary>Gets the genre (album-level preferred, track-level fallback).</summary>
    public string? Genre => FindAlbumOrTrack("GENRE");

    /// <summary>Gets the comment (album-level preferred, track-level fallback).</summary>
    public string? Comment => FindAlbumOrTrack("COMMENT");

    /// <summary>Gets the track number (track-level only by convention).</summary>
    public string? TrackNumber => FindByTarget("PART_NUMBER", TrackLevel);

    /// <summary>
    /// Parses a <c>Tags</c> element's payload bytes (the bytes after its id+size header) into a <see cref="MatroskaTag"/>.
    /// </summary>
    /// <param name="payload">The raw payload bytes.</param>
    /// <returns>A populated <see cref="MatroskaTag"/>; returns an empty instance if <paramref name="payload"/> is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    public static MatroskaTag FromPayload(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var tag = new MatroskaTag();
        if (payload.Length == 0)
        {
            return tag;
        }

        using var ms = new MemoryStream(payload, writable: false);
        ParseTagsPayload(ms, payload.Length, tag, depth: 0);
        return tag;
    }

    /// <summary>
    /// Serialises this tag as the on-disk bytes of a Matroska <c>Tags</c> element (id + size VINT + payload).
    /// </summary>
    public byte[] ToByteArray()
    {
        var payload = SerializePayload();
        var idBytes = EbmlElement.EncodeId(MatroskaElementIds.Tags);
        var sizeBytes = EbmlElement.EncodeVintSize(payload.Length);
        var buf = new byte[idBytes.Length + sizeBytes.Length + payload.Length];
        Buffer.BlockCopy(idBytes, 0, buf, 0, idBytes.Length);
        Buffer.BlockCopy(sizeBytes, 0, buf, idBytes.Length, sizeBytes.Length);
        Buffer.BlockCopy(payload, 0, buf, idBytes.Length + sizeBytes.Length, payload.Length);
        return buf;
    }

    /// <summary>
    /// Serialises just the payload bytes of the <c>Tags</c> element (i.e. the concatenation of every Tag entry).
    /// </summary>
    public byte[] SerializePayload()
    {
        using var ms = new MemoryStream();
        foreach (var entry in Entries)
        {
            WriteTag(ms, entry);
        }

        return ms.ToArray();
    }

    private static void ParseTagsPayload(Stream stream, long limit, MatroskaTag tag, int depth)
    {
        if (depth > 16)
        {
            return;
        }

        var end = stream.Position + limit;
        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var size, out var unknown))
            {
                return;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : size;
            if (actualSize < 0 || actualSize > available)
            {
                return;
            }

            if (id == MatroskaElementIds.Tag)
            {
                var entry = ReadTagEntry(stream, actualSize, depth + 1);
                if (entry is not null)
                {
                    tag.Entries.Add(entry);
                }
            }
            else
            {
                stream.Position = payloadStart + actualSize;
            }
        }
    }

    private static MatroskaTagEntry? ReadTagEntry(Stream stream, long size, int depth)
    {
        if (depth > 16)
        {
            stream.Position += size;
            return null;
        }

        var entry = new MatroskaTagEntry();
        var end = stream.Position + size;
        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var elemSize, out var unknown))
            {
                return entry;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : elemSize;
            if (actualSize < 0 || actualSize > available)
            {
                return entry;
            }

            if (id == MatroskaElementIds.Targets)
            {
                entry.Targets = ReadTargets(stream, actualSize);
            }
            else if (id == MatroskaElementIds.SimpleTag)
            {
                var st = ReadSimpleTag(stream, actualSize, depth + 1);
                if (st is not null)
                {
                    entry.SimpleTags.Add(st);
                }
            }
            else
            {
                stream.Position = payloadStart + actualSize;
            }
        }

        return entry;
    }

    private static MatroskaTargets ReadTargets(Stream stream, long size)
    {
        var targets = new MatroskaTargets();
        var end = stream.Position + size;
        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var elemSize, out var unknown))
            {
                return targets;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : elemSize;
            if (actualSize < 0 || actualSize > available)
            {
                return targets;
            }

            var payload = ReadBytes(stream, actualSize);
            if (id == MatroskaElementIds.TargetTypeValue)
            {
                targets.TargetTypeValue = (ulong)EbmlElement.DecodeUInt(payload);
            }
            else if (id == MatroskaElementIds.TargetType)
            {
                targets.TargetType = Encoding.ASCII.GetString(payload);
            }
            else if (id == MatroskaElementIds.TagTrackUid)
            {
                targets.TrackUids.Add((ulong)EbmlElement.DecodeUInt(payload));
            }
            else if (id == MatroskaElementIds.TagEditionUid)
            {
                targets.EditionUids.Add((ulong)EbmlElement.DecodeUInt(payload));
            }
            else if (id == MatroskaElementIds.TagChapterUid)
            {
                targets.ChapterUids.Add((ulong)EbmlElement.DecodeUInt(payload));
            }
            else if (id == MatroskaElementIds.TagAttachmentUid)
            {
                targets.AttachmentUids.Add((ulong)EbmlElement.DecodeUInt(payload));
            }
        }

        return targets;
    }

    private static MatroskaSimpleTag? ReadSimpleTag(Stream stream, long size, int depth)
    {
        if (depth > 16)
        {
            stream.Position += size;
            return null;
        }

        var st = new MatroskaSimpleTag();
        var end = stream.Position + size;
        var sawString = false;
        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var elemSize, out var unknown))
            {
                return st;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : elemSize;
            if (actualSize < 0 || actualSize > available)
            {
                return st;
            }

            if (id == MatroskaElementIds.SimpleTag)
            {
                var nested = ReadSimpleTag(stream, actualSize, depth + 1);
                if (nested is not null)
                {
                    st.SimpleTags.Add(nested);
                }

                continue;
            }

            var payload = ReadBytes(stream, actualSize);
            if (id == MatroskaElementIds.TagName)
            {
                st.Name = Encoding.UTF8.GetString(payload);
            }
            else if (id == MatroskaElementIds.TagString)
            {
                st.Value = Encoding.UTF8.GetString(payload);
                sawString = true;
            }
            else if (id == MatroskaElementIds.TagBinary)
            {
                st.Binary = payload;
            }
            else if (id == MatroskaElementIds.TagLanguage)
            {
                st.Language = Encoding.ASCII.GetString(payload);
            }
            else if (id == MatroskaElementIds.TagLanguageBcp47)
            {
                st.LanguageBcp47 = Encoding.ASCII.GetString(payload);
            }
            else if (id == MatroskaElementIds.TagDefault)
            {
                st.IsDefault = EbmlElement.DecodeUInt(payload) != 0;
            }
        }

        // If neither TagString nor TagBinary was present, leave Value/Binary as null/null.
        // If TagString was empty, preserve empty string rather than null.
        if (!sawString && st.Binary is null && st.Value is null)
        {
            // Spec says one of TagString or TagBinary is required; tolerate missing by leaving null.
        }

        return st;
    }

    private static byte[] ReadBytes(Stream stream, long size)
    {
        if (size <= 0)
        {
            return [];
        }

        if (size > int.MaxValue)
        {
            // Pathologically huge; refuse to allocate.
            stream.Position += size;
            return [];
        }

        var buf = new byte[size];
        var read = 0;
        while (read < buf.Length)
        {
            var n = stream.Read(buf, read, buf.Length - read);
            if (n <= 0)
            {
                break;
            }

            read += n;
        }

        if (read < buf.Length)
        {
            Array.Resize(ref buf, read);
        }

        return buf;
    }

    private static void WriteTag(Stream stream, MatroskaTagEntry entry)
    {
        var payload = SerializeTagEntry(entry);
        WriteElement(stream, MatroskaElementIds.Tag, payload);
    }

    private static byte[] SerializeTagEntry(MatroskaTagEntry entry)
    {
        using var ms = new MemoryStream();
        WriteElement(ms, MatroskaElementIds.Targets, SerializeTargets(entry.Targets));
        foreach (var st in entry.SimpleTags)
        {
            WriteElement(ms, MatroskaElementIds.SimpleTag, SerializeSimpleTag(st));
        }

        return ms.ToArray();
    }

    private static byte[] SerializeTargets(MatroskaTargets targets)
    {
        using var ms = new MemoryStream();
        WriteElement(ms, MatroskaElementIds.TargetTypeValue, EbmlElement.EncodeUInt((long)targets.TargetTypeValue));
        if (!string.IsNullOrEmpty(targets.TargetType))
        {
            WriteElement(ms, MatroskaElementIds.TargetType, Encoding.ASCII.GetBytes(targets.TargetType));
        }

        foreach (var uid in targets.TrackUids)
        {
            WriteElement(ms, MatroskaElementIds.TagTrackUid, EbmlElement.EncodeUInt((long)uid));
        }

        foreach (var uid in targets.EditionUids)
        {
            WriteElement(ms, MatroskaElementIds.TagEditionUid, EbmlElement.EncodeUInt((long)uid));
        }

        foreach (var uid in targets.ChapterUids)
        {
            WriteElement(ms, MatroskaElementIds.TagChapterUid, EbmlElement.EncodeUInt((long)uid));
        }

        foreach (var uid in targets.AttachmentUids)
        {
            WriteElement(ms, MatroskaElementIds.TagAttachmentUid, EbmlElement.EncodeUInt((long)uid));
        }

        return ms.ToArray();
    }

    private static byte[] SerializeSimpleTag(MatroskaSimpleTag st)
    {
        using var ms = new MemoryStream();
        WriteElement(ms, MatroskaElementIds.TagName, Encoding.UTF8.GetBytes(st.Name ?? string.Empty));
        WriteElement(ms, MatroskaElementIds.TagLanguage, Encoding.ASCII.GetBytes(st.Language ?? "und"));
        if (st.LanguageBcp47 is not null)
        {
            WriteElement(ms, MatroskaElementIds.TagLanguageBcp47, Encoding.ASCII.GetBytes(st.LanguageBcp47));
        }

        WriteElement(ms, MatroskaElementIds.TagDefault, EbmlElement.EncodeUInt(st.IsDefault ? 1 : 0));
        if (st.Binary is not null)
        {
            WriteElement(ms, MatroskaElementIds.TagBinary, st.Binary);
        }
        else
        {
            WriteElement(ms, MatroskaElementIds.TagString, Encoding.UTF8.GetBytes(st.Value ?? string.Empty));
        }

        foreach (var nested in st.SimpleTags)
        {
            WriteElement(ms, MatroskaElementIds.SimpleTag, SerializeSimpleTag(nested));
        }

        return ms.ToArray();
    }

    private static void WriteElement(Stream stream, long id, byte[] payload)
    {
        var idBytes = EbmlElement.EncodeId(id);
        var sizeBytes = EbmlElement.EncodeVintSize(payload.Length);
        stream.Write(idBytes, 0, idBytes.Length);
        stream.Write(sizeBytes, 0, sizeBytes.Length);
        stream.Write(payload, 0, payload.Length);
    }

    private string? FindAlbumOrTrack(string name) =>
        FindByTarget(name, AlbumLevel) ?? FindByTarget(name, TrackLevel);

    private string? FindByTarget(string name, int targetLevel)
    {
        foreach (var entry in Entries)
        {
            if ((int)entry.Targets.TargetTypeValue != targetLevel)
            {
                continue;
            }

            var v = FindInSimpleTags(entry.SimpleTags, name);
            if (v is not null)
            {
                return v;
            }
        }

        return null;
    }

    private static string? FindInSimpleTags(IEnumerable<MatroskaSimpleTag> tags, string name)
    {
        foreach (var st in tags)
        {
            if (string.Equals(st.Name, name, StringComparison.OrdinalIgnoreCase) && st.Value is not null)
            {
                return st.Value;
            }
        }

        return null;
    }
}
