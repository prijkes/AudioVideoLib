namespace AudioVideoLib.Studio.Editors.Id3v2;

public sealed record Id3v2KnownUrlFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions);

public static class Id3v2KnownUrlFrameIds
{
    public static readonly Id3v2KnownUrlFrameId[] All =
    [
        new("WCOM", "WCM", "Commercial information",         Id3v2VersionMask.All),
        new("WCOP", "WCP", "Copyright / legal information",  Id3v2VersionMask.All),
        new("WOAF", "WAF", "Official audio file webpage",    Id3v2VersionMask.All),
        new("WOAR", "WAR", "Official artist webpage",        Id3v2VersionMask.All),
        new("WOAS", "WAS", "Official audio source webpage",  Id3v2VersionMask.All),
        new("WORS", null,  "Official internet radio webpage", Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPAY", null,  "Payment URL",                    Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPUB", "WPB", "Publishers official webpage",    Id3v2VersionMask.All),
    ];

    public static string IdentifierFor(Id3v2KnownUrlFrameId entry, Id3v2VersionMask versionMask)
        => (versionMask == Id3v2VersionMask.V220 || versionMask == Id3v2VersionMask.V221) && entry.V220Identifier is { } v220
            ? v220
            : entry.Identifier;
}
