/*
 * Date: 2012-12-08
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System.Collections.Generic;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing an URL link.
    /// </summary>
    public sealed partial class Id3v2UrlLinkFrame
    {
        private static readonly Dictionary<Id3v2UrlLinkFrameIdentifier, Dictionary<string, Id3v2Version[]>> Identifiers =
            new Dictionary<Id3v2UrlLinkFrameIdentifier, Dictionary<string, Id3v2Version[]>>
                {
                    {
                        Id3v2UrlLinkFrameIdentifier.CommercialInformations,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WCM", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WCOM", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.CopyrightInformation,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WCP", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WCOP", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.OfficialArtistWebpage,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WAR", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WOAR", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.OfficialAudioFileWebpage,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WAF", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WOAF", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.OfficialAudioSource,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WAS", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WOAS", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.OfficialInternetRadioStationHomepage,
                        new Dictionary<string, Id3v2Version[]>
                            { { "WORS", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.PaymentWebpage,
                        new Dictionary<string, Id3v2Version[]>
                            { { "WPAY", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2UrlLinkFrameIdentifier.PublishersOfficialWebpage,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "WPB", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "WPUB", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    }
                };
    }
}
