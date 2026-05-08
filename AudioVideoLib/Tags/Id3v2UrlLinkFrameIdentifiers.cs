namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class for storing an URL link.
/// </summary>
public sealed partial class Id3v2UrlLinkFrame
{
    private static readonly Dictionary<Id3v2UrlLinkFrameIdentifier, Dictionary<string, Id3v2Version[]>> Identifiers =
        new()
        {
                {
                    Id3v2UrlLinkFrameIdentifier.CommercialInformations,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WCM", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WCOM", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.CopyrightInformation,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WCP", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WCOP", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.OfficialArtistWebpage,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WAR", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WOAR", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.OfficialAudioFileWebpage,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WAF", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WOAF", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.OfficialAudioSource,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WAS", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WOAS", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.OfficialInternetRadioStationHomepage,
                    new Dictionary<string, Id3v2Version[]>
                        { { "WORS", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] } }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.PaymentWebpage,
                    new Dictionary<string, Id3v2Version[]>
                        { { "WPAY", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] } }
                },
                {
                    Id3v2UrlLinkFrameIdentifier.PublishersOfficialWebpage,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "WPB", [Id3v2Version.Id3v220, Id3v2Version.Id3v221] },
                            { "WPUB", [Id3v2Version.Id3v230, Id3v2Version.Id3v240] }
                        }
                }
            };

    /// <summary>
    /// Enumerates every URL-link-frame identifier mapping known to the
    /// library — every <see cref="Id3v2UrlLinkFrameIdentifier"/> entry, paired
    /// with its v2.3+ canonical identifier, optional v2.2 alternate, and union
    /// of supported versions. Single source of truth for downstream consumers.
    /// </summary>
    public static IEnumerable<Id3v2UrlLinkFrameIdentifierMapping> EnumerateIdentifierMappings()
    {
        foreach (var (_, dict) in Identifiers)
        {
            yield return BuildMapping(dict);
        }
    }

    private static Id3v2UrlLinkFrameIdentifierMapping BuildMapping(
        Dictionary<string, Id3v2Version[]> dict)
    {
        var v23Plus = dict.SingleOrDefault(kv =>
            kv.Value != null && (kv.Value.Contains(Id3v2Version.Id3v230) || kv.Value.Contains(Id3v2Version.Id3v240)));
        var v22 = dict.SingleOrDefault(kv =>
            kv.Value != null && (kv.Value.Contains(Id3v2Version.Id3v220) || kv.Value.Contains(Id3v2Version.Id3v221)));

        var canonical = v23Plus.Key ?? v22.Key
            ?? throw new InvalidOperationException(
                "Identifier dictionary entry has no v2.3+ or v2.2 mapping.");
        var alternate = (v22.Key != null && v22.Key != canonical) ? v22.Key : null;
        var allVersions = dict.Values.Where(v => v != null).SelectMany(v => v).Distinct().OrderBy(v => v).ToArray();

        return new Id3v2UrlLinkFrameIdentifierMapping(canonical, alternate, allVersions);
    }
}
