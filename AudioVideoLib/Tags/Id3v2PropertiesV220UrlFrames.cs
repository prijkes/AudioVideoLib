/*
 * Date: 2011-08-14
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags;

/// <summary>
/// Class to store an Id3v2 tag.
/// </summary>
public partial class Id3v2Tag
{
    /// <summary>
    /// Gets or sets a collection of commercial information.
    /// </summary>
    /// <value>
    /// A collection commercial information.
    /// </value>
    /// <remarks>
    /// The 'Commercial information' frame is a URL pointing at a webpage with information such as where the album can be bought.
    /// There may be more than one "WCOM" frame in a tag, but not with the same content.
    /// </remarks>
    public Id3v2FrameCollection<Id3v2UrlLinkFrame> CommercialInformations
    {
        get => GetVersionedUrlFrameCollection(Id3v2UrlLinkFrameIdentifier.CommercialInformations);
        set => SetVersionedUrlFrameCollection(value, Id3v2UrlLinkFrameIdentifier.CommercialInformations);
    }

    /// <summary>
    /// Gets or sets the copyright information.
    /// </summary>
    /// <value>
    /// The copyright information.
    /// </value>
    /// <remarks>
    /// The 'Copyright/Legal information' frame is a URL pointing at a webpage 
    /// where the terms of use and ownership of the file is described.
    /// </remarks>
    public Id3v2UrlLinkFrame? CopyrightInformation
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.CopyrightInformation);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.CopyrightInformation);
    }

    /// <summary>
    /// Gets or sets the official artist webpage.
    /// </summary>
    /// <value>
    /// The official artist webpage.
    /// </value>
    /// <remarks>
    /// The 'Official artist/performer webpage' frame is a URL pointing at the artists official webpage.
    /// There may be more than one "WOAR" frame in a tag if the audio contains more than one performer, but not with the same content.
    /// </remarks>
    public Id3v2FrameCollection<Id3v2UrlLinkFrame> OfficialArtistWebpage
    {
        get => GetVersionedUrlFrameCollection(Id3v2UrlLinkFrameIdentifier.OfficialArtistWebpage);
        set => SetVersionedUrlFrameCollection(value, Id3v2UrlLinkFrameIdentifier.OfficialArtistWebpage);
    }

    /// <summary>
    /// Gets or sets the official audio file webpage.
    /// </summary>
    /// <value>
    /// The official audio file webpage.
    /// </value>
    /// <remarks>
    /// The 'Official audio file webpage' frame is a URL pointing at a file specific webpage.
    /// </remarks>
    public Id3v2UrlLinkFrame? OfficialAudioFileWebpage
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.OfficialAudioFileWebpage);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.OfficialAudioFileWebpage);
    }

    /// <summary>
    /// Gets or sets the official audio source.
    /// </summary>
    /// <value>
    /// The official audio source.
    /// </value>
    /// <remarks>
    /// The 'Official audio source webpage' frame is a URL pointing 
    /// at the official webpage for the source of the audio file, e.g. a movie.
    /// </remarks>
    public Id3v2UrlLinkFrame? OfficialAudioSource
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.OfficialAudioSource);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.OfficialAudioSource);
    }

    /// <summary>
    /// Gets or sets the publishers official webpage.
    /// </summary>
    /// <value>
    /// The publishers official webpage.
    /// </value>
    /// <remarks>
    /// The 'Publishers official webpage' frame is a URL pointing at the official webpage for the publisher.
    /// </remarks>
    public Id3v2UrlLinkFrame? PublishersOfficialWebpage
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.PublishersOfficialWebpage);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.PublishersOfficialWebpage);
    }
}
