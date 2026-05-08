namespace AudioVideoLib.Tags;

using System.Collections.Generic;

/// <summary>
/// Mapping of one logical text-frame field to the identifier strings that
/// represent it across <see cref="Id3v2Version"/>s. Returned by
/// <see cref="Id3v2TextFrame.EnumerateIdentifierMappings"/>.
/// </summary>
/// <param name="Identifier">The canonical (v2.3+) identifier string.</param>
/// <param name="V220Identifier">The 3-character v2.2/v2.2.1 alternate, or
/// <c>null</c> if none. (No frame in current data is v2.2-only — this null path
/// is forward compatibility insurance, not active behavior.)</param>
/// <param name="SupportedVersions">All <see cref="Id3v2Version"/>s where this
/// field exists in any form.</param>
public sealed record Id3v2TextFrameIdentifierMapping(
    string Identifier,
    string? V220Identifier,
    IReadOnlyList<Id3v2Version> SupportedVersions);
