namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Tags;

public sealed record Id3v2KnownTextFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions);

/// <summary>
/// The version-correct identifier to use when writing a text frame, plus the
/// priority-ordered list of identifiers to try when reading. Returned by
/// <see cref="Id3v2KnownTextFrameIds.Resolve(string, Id3v2Version)"/> and
/// <see cref="Id3v2KnownTextFrameIds.ResolveYear(Id3v2Version)"/>.
/// </summary>
public sealed record Id3v2TextFrameResolution(string Write, IReadOnlyList<string> Read);

public static class Id3v2KnownTextFrameIds
{
    /// <summary>
    /// Display names keyed by canonical (v2.3+) identifier. The only
    /// Studio-side data that's NOT derived from the lib's identifier table.
    /// Declared before <see cref="All"/> so static init populates it first
    /// (C# initializes static fields in textual order).
    /// Missing entries cause <see cref="BuildAll"/> to throw at static init.
    /// </summary>
    private static readonly Dictionary<string, string> FriendlyNames = new(StringComparer.Ordinal)
    {
        { "TALB", "Album" },
        { "TBPM", "Beats per minute" },
        { "TCOM", "Composer" },
        { "TCON", "Genre" },
        { "TCOP", "Copyright message" },
        { "TDEN", "Encoding time" },
        { "TDLY", "Playlist delay" },
        { "TDOR", "Original release time" },
        { "TDRC", "Recording time" },
        { "TDRL", "Release time" },
        { "TDTG", "Tagging time" },
        { "TENC", "Encoded by" },
        { "TEXT", "Lyricist / Text writer" },
        { "TFLT", "File type" },
        { "TIPL", "Involved people list" },
        { "TIT1", "Content group description" },
        { "TIT2", "Title" },
        { "TIT3", "Subtitle / refinement" },
        { "TKEY", "Initial key" },
        { "TLAN", "Language(s)" },
        { "TLEN", "Length" },
        { "TMCL", "Musician credits list" },
        { "TMED", "Media type" },
        { "TMOO", "Mood" },
        { "TOAL", "Original album" },
        { "TOFN", "Original filename" },
        { "TOLY", "Original lyricist" },
        { "TOPE", "Original artist" },
        { "TOWN", "File owner / licensee" },
        { "TPE1", "Lead artist" },
        { "TPE2", "Band / orchestra" },
        { "TPE3", "Conductor" },
        { "TPE4", "Interpreted / remixed by" },
        { "TPOS", "Part of a set" },
        { "TPRO", "Produced notice" },
        { "TPUB", "Publisher" },
        { "TRCK", "Track number" },
        { "TRSN", "Internet radio station name" },
        { "TRSO", "Internet radio station owner" },
        { "TSOA", "Album sort order" },
        { "TSOP", "Performer sort order" },
        { "TSOT", "Title sort order" },
        { "TSRC", "ISRC" },
        { "TSSE", "Encoding software / hardware" },
        { "TSST", "Set subtitle" },
        { "TDAT", "Date (DDMM, deprecated v2.4)" },
        { "TIME", "Time (HHMM, deprecated v2.4)" },
        { "TORY", "Original release year" },
        { "TRDA", "Recording dates" },
        { "TSIZ", "Size" },
        { "TYER", "Year" },
    };

    /// <summary>
    /// Catalog of every text-frame identifier known to the library, derived
    /// from <see cref="Id3v2TextFrame.EnumerateIdentifierMappings"/>. Adding
    /// or removing an identifier in the lib's table flows through here
    /// automatically; the only Studio-side data is <see cref="FriendlyNames"/>.
    /// </summary>
    public static readonly Id3v2KnownTextFrameId[] All = BuildAll();

    private static Id3v2KnownTextFrameId[] BuildAll()
        => [.. Id3v2TextFrame.EnumerateIdentifierMappings()
            .Select(m => new Id3v2KnownTextFrameId(
                m.Identifier,
                m.V220Identifier,
                FriendlyNames.TryGetValue(m.Identifier, out var name)
                    ? name
                    : throw new InvalidOperationException(
                        $"Missing FriendlyName for canonical text-frame identifier '{m.Identifier}'."),
                ToMask(m.SupportedVersions)))];

    private static Id3v2VersionMask ToMask(IReadOnlyList<Id3v2Version> versions)
    {
        var mask = Id3v2VersionMask.None;
        foreach (var v in versions)
        {
            mask |= v.ToMask();
        }
        return mask;
    }

    public static string IdentifierFor(Id3v2KnownTextFrameId entry, Id3v2VersionMask versionMask)
        => (versionMask == Id3v2VersionMask.V220 || versionMask == Id3v2VersionMask.V221) && entry.V220Identifier is { } v220
            ? v220
            : entry.Identifier;

    /// <summary>
    /// Locates the catalog entry whose primary or v2.2 alternate identifier matches
    /// <paramref name="identifier"/> (case-insensitive). Returns <c>true</c> on hit.
    /// </summary>
    /// <remarks>
    /// Match is <see cref="StringComparison.OrdinalIgnoreCase"/> for resilience against
    /// case-mismatched callers; ID3v2 identifiers are uppercase by spec.
    /// </remarks>
    public static bool TryFind(string identifier, out Id3v2KnownTextFrameId entry)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        foreach (var e in All)
        {
            if (string.Equals(e.Identifier, identifier, StringComparison.OrdinalIgnoreCase)
                || (e.V220Identifier is not null
                    && string.Equals(e.V220Identifier, identifier, StringComparison.OrdinalIgnoreCase)))
            {
                entry = e;
                return true;
            }
        }
        entry = null!;
        return false;
    }

    /// <summary>
    /// Resolves a canonical identifier to the version-correct identifier to write
    /// and a priority-ordered list of identifiers to try when reading. The canonical
    /// may be either the v2.3+ identifier or the v2.2 alternate (case-insensitive).
    /// </summary>
    /// <param name="canonicalIdentifier">An identifier known to <see cref="All"/>.</param>
    /// <param name="version">The tag version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="canonicalIdentifier"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="canonicalIdentifier"/> is not in the catalog or its
    /// owning entry does not support <paramref name="version"/>. For year, callers must
    /// use <see cref="ResolveYear"/> — TDRC and TYER are separate entries; passing one
    /// at a version that doesn't support it throws here.
    /// </exception>
    public static Id3v2TextFrameResolution Resolve(string canonicalIdentifier, Id3v2Version version)
    {
        if (!TryFind(canonicalIdentifier, out var entry))
        {
            throw new ArgumentException(
                $"'{canonicalIdentifier}' is not a known text-frame identifier.",
                nameof(canonicalIdentifier));
        }
        var versionMask = version.ToMask();
        if ((entry.SupportedVersions & versionMask) == 0)
        {
            throw new ArgumentException(
                $"Text frame '{entry.Identifier}' is not supported in {version}; " +
                "for year fields use ResolveYear.",
                nameof(canonicalIdentifier));
        }
        var write = IdentifierFor(entry, versionMask);
        var alternate = string.Equals(entry.Identifier, write, StringComparison.OrdinalIgnoreCase)
            ? entry.V220Identifier
            : entry.Identifier;
        return new(write, alternate is null ? [write] : [write, alternate]);
    }

    /// <summary>
    /// Resolves the recording-year field across the v2.3↔v2.4 identifier change.
    /// Returns TDRC on v2.4+ (with TYER/TYE legacy reads), TYER on v2.3 (with TYE legacy),
    /// TYE on v2.2/v2.2.1 (with TYER legacy).
    /// </summary>
    public static Id3v2TextFrameResolution ResolveYear(Id3v2Version version)
    {
        return version >= Id3v2Version.Id3v240
            ? new("TDRC", ["TDRC", "TYER", "TYE"])
            : Resolve("TYER", version);
    }
}
