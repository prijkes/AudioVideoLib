namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Tags;

public sealed record Id3v2KnownUrlFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions,
    bool AllowMultiple = false);

public static class Id3v2KnownUrlFrameIds
{
    /// <summary>
    /// Studio-side UI metadata keyed by canonical (v2.3+) identifier:
    /// FriendlyName plus AllowMultiple (per ID3v2 spec §4.3.1, only WOAR may
    /// appear once per performer when the audio has multiple artists).
    /// Declared before <see cref="All"/> so static init populates it first
    /// (C# initializes static fields in textual order).
    /// Missing entries cause <see cref="BuildAll"/> to throw at static init.
    /// </summary>
    private static readonly Dictionary<string, (string FriendlyName, bool AllowMultiple)> Metadata
        = new(StringComparer.Ordinal)
        {
            { "WCOM", ("Commercial information",         false) },
            { "WCOP", ("Copyright / legal information",  false) },
            { "WOAF", ("Official audio file webpage",    false) },
            { "WOAR", ("Official artist webpage",        true ) },
            { "WOAS", ("Official audio source webpage",  false) },
            { "WORS", ("Official internet radio webpage", false) },
            { "WPAY", ("Payment URL",                    false) },
            { "WPUB", ("Publishers official webpage",    false) },
        };

    /// <summary>
    /// Catalog of every URL-link-frame identifier known to the library, derived
    /// from <see cref="Id3v2UrlLinkFrame.EnumerateIdentifierMappings"/>.
    /// </summary>
    public static readonly Id3v2KnownUrlFrameId[] All = BuildAll();

    private static Id3v2KnownUrlFrameId[] BuildAll()
        => [.. Id3v2UrlLinkFrame.EnumerateIdentifierMappings()
            .Select(m => Metadata.TryGetValue(m.Identifier, out var meta)
                ? new Id3v2KnownUrlFrameId(
                    m.Identifier,
                    m.V220Identifier,
                    meta.FriendlyName,
                    Id3v2FrameIdCatalogHelpers.ToMask(m.SupportedVersions),
                    AllowMultiple: meta.AllowMultiple)
                : throw new InvalidOperationException(
                    $"Missing UI metadata for canonical URL-frame identifier '{m.Identifier}'."))];

    public static string IdentifierFor(Id3v2KnownUrlFrameId entry, Id3v2VersionMask versionMask)
        => (versionMask == Id3v2VersionMask.V220 || versionMask == Id3v2VersionMask.V221) && entry.V220Identifier is { } v220
            ? v220
            : entry.Identifier;

    /// <summary>
    /// Locates the catalog entry whose primary or v2.2 alternate identifier matches
    /// <paramref name="identifier"/> (case-insensitive). Returns <c>true</c> on hit.
    /// </summary>
    public static bool TryFind(string identifier, out Id3v2KnownUrlFrameId entry)
        => Id3v2FrameIdCatalogHelpers.TryFind(All, identifier, e => e.Identifier, e => e.V220Identifier, out entry!);
}
