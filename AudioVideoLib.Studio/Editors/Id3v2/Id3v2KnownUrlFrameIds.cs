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
    /// True when an extra frame with this identifier may be added to a tag that
    /// already contains one. Currently only WOAR (multiple performers, ID3v2 §4.3.1).
    /// </summary>
    public static bool AllowsMultiple(string identifier)
    {
        foreach (var entry in All)
        {
            if (entry.AllowMultiple
                && (string.Equals(entry.Identifier, identifier, StringComparison.Ordinal)
                 || string.Equals(entry.V220Identifier, identifier, StringComparison.Ordinal)))
            {
                return true;
            }
        }
        return false;
    }
}
