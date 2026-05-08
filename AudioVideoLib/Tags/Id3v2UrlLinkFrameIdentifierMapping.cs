namespace AudioVideoLib.Tags;

using System.Collections.Generic;

/// <summary>
/// Mirror of <see cref="Id3v2TextFrameIdentifierMapping"/> for URL-link frames.
/// Returned by <see cref="Id3v2UrlLinkFrame.EnumerateIdentifierMappings"/>.
/// </summary>
public sealed record Id3v2UrlLinkFrameIdentifierMapping(
    string Identifier,
    string? V220Identifier,
    IReadOnlyList<Id3v2Version> SupportedVersions);
