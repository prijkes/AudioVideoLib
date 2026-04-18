namespace AudioVideoLib.Tags;

using System.Collections.Generic;

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
}
