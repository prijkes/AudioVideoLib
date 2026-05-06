namespace AudioVideoLib.Studio.Editors.Id3v2;

public sealed record Id3v2KnownTextFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions);

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
}
