namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;

public sealed record Id3v2KnownUrlFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions,
    bool AllowMultiple = false);

public static class Id3v2KnownUrlFrameIds
{
    // ID3v2 spec §4.3.1: each W* frame is unique per tag, except WOAR (WAR in v2.2)
    // which may appear once per performer when the audio has multiple artists.
    public static readonly Id3v2KnownUrlFrameId[] All =
    [
        new("WCOM", "WCM", "Commercial information",         Id3v2VersionMask.All),
        new("WCOP", "WCP", "Copyright / legal information",  Id3v2VersionMask.All),
        new("WOAF", "WAF", "Official audio file webpage",    Id3v2VersionMask.All),
        new("WOAR", "WAR", "Official artist webpage",        Id3v2VersionMask.All, AllowMultiple: true),
        new("WOAS", "WAS", "Official audio source webpage",  Id3v2VersionMask.All),
        new("WORS", null,  "Official internet radio webpage", Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPAY", null,  "Payment URL",                    Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPUB", "WPB", "Publishers official webpage",    Id3v2VersionMask.All),
    ];

    public static string IdentifierFor(Id3v2KnownUrlFrameId entry, Id3v2VersionMask versionMask)
        => (versionMask == Id3v2VersionMask.V220 || versionMask == Id3v2VersionMask.V221) && entry.V220Identifier is { } v220
            ? v220
            : entry.Identifier;

    /// <summary>
    /// Locates the catalog entry whose primary or v2.2 alternate identifier matches
    /// <paramref name="identifier"/> (case-insensitive). Returns <c>true</c> on hit.
    /// </summary>
    public static bool TryFind(string identifier, out Id3v2KnownUrlFrameId entry)
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
}
