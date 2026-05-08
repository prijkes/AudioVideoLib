namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;

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
public sealed record Id3v2TextFrameResolution(string Write, System.Collections.Generic.IReadOnlyList<string> Read);

public static class Id3v2KnownTextFrameIds
{
    public static readonly Id3v2KnownTextFrameId[] All =
    [
        new("TALB", "TAL", "Album",                          Id3v2VersionMask.All),
        new("TBPM", "TBP", "Beats per minute",               Id3v2VersionMask.All),
        new("TCOM", "TCM", "Composer",                       Id3v2VersionMask.All),
        new("TCON", "TCO", "Genre",                          Id3v2VersionMask.All),
        new("TCOP", "TCR", "Copyright message",              Id3v2VersionMask.All),
        new("TDEN", null,  "Encoding time",                  Id3v2VersionMask.V240),
        new("TDLY", "TDY", "Playlist delay",                 Id3v2VersionMask.All),
        new("TDOR", null,  "Original release time",          Id3v2VersionMask.V240),
        new("TDRC", null,  "Recording time",                 Id3v2VersionMask.V240),
        new("TDRL", null,  "Release time",                   Id3v2VersionMask.V240),
        new("TDTG", null,  "Tagging time",                   Id3v2VersionMask.V240),
        new("TENC", "TEN", "Encoded by",                     Id3v2VersionMask.All),
        new("TEXT", "TXT", "Lyricist / Text writer",         Id3v2VersionMask.All),
        new("TFLT", "TFT", "File type",                      Id3v2VersionMask.All),
        new("TIPL", null,  "Involved people list",           Id3v2VersionMask.V240),
        new("TIT1", "TT1", "Content group description",      Id3v2VersionMask.All),
        new("TIT2", "TT2", "Title",                          Id3v2VersionMask.All),
        new("TIT3", "TT3", "Subtitle / refinement",          Id3v2VersionMask.All),
        new("TKEY", "TKE", "Initial key",                    Id3v2VersionMask.All),
        new("TLAN", "TLA", "Language(s)",                    Id3v2VersionMask.All),
        new("TLEN", "TLE", "Length",                         Id3v2VersionMask.All),
        new("TMCL", null,  "Musician credits list",          Id3v2VersionMask.V240),
        new("TMED", "TMT", "Media type",                     Id3v2VersionMask.All),
        new("TMOO", null,  "Mood",                           Id3v2VersionMask.V240),
        new("TOAL", "TOT", "Original album",                 Id3v2VersionMask.All),
        new("TOFN", "TOF", "Original filename",              Id3v2VersionMask.All),
        new("TOLY", "TOL", "Original lyricist",              Id3v2VersionMask.All),
        new("TOPE", "TOA", "Original artist",                Id3v2VersionMask.All),
        new("TOWN", null,  "File owner / licensee",          Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TPE1", "TP1", "Lead artist",                    Id3v2VersionMask.All),
        new("TPE2", "TP2", "Band / orchestra",               Id3v2VersionMask.All),
        new("TPE3", "TP3", "Conductor",                      Id3v2VersionMask.All),
        new("TPE4", "TP4", "Interpreted / remixed by",       Id3v2VersionMask.All),
        new("TPOS", "TPA", "Part of a set",                  Id3v2VersionMask.All),
        new("TPRO", null,  "Produced notice",                Id3v2VersionMask.V240),
        new("TPUB", "TPB", "Publisher",                      Id3v2VersionMask.All),
        new("TRCK", "TRK", "Track number",                   Id3v2VersionMask.All),
        new("TRSN", null,  "Internet radio station name",    Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TRSO", null,  "Internet radio station owner",   Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TSOA", null,  "Album sort order",               Id3v2VersionMask.V240),
        new("TSOP", null,  "Performer sort order",           Id3v2VersionMask.V240),
        new("TSOT", null,  "Title sort order",               Id3v2VersionMask.V240),
        new("TSRC", "TRC", "ISRC",                           Id3v2VersionMask.All),
        new("TSSE", "TSS", "Encoding software / hardware",   Id3v2VersionMask.All),
        new("TSST", null,  "Set subtitle",                   Id3v2VersionMask.V240),
        new("TDAT", "TDA", "Date (DDMM, deprecated v2.4)",   Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
        new("TIME", "TIM", "Time (HHMM, deprecated v2.4)",   Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
        new("TORY", "TOR", "Original release year",          Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
        new("TRDA", "TRD", "Recording dates",                Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
        new("TSIZ", "TSI", "Size",                           Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
        new("TYER", "TYE", "Year",                           Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230),
    ];

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
