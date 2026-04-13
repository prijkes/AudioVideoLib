namespace AudioVideoLib.Tags;

/// <summary>
/// Class to store an Id3v2 tag.
/// </summary>
public partial class Id3v2Tag
{
    /// <summary>
    /// Gets or sets the official internet radio station homepage.
    /// </summary>
    /// <value>
    /// The official internet radio station homepage.
    /// </value>
    /// <remarks>
    /// The 'Official internet radio station homepage' contains a URL pointing at the homepage of the internet radio station.
    /// </remarks>
    public Id3v2UrlLinkFrame? OfficialInternetRadioStationHomepage
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.OfficialInternetRadioStationHomepage, Id3v2Version.Id3v230);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.OfficialInternetRadioStationHomepage, Id3v2Version.Id3v230);
    }

    /// <summary>
    /// Gets or sets the payment webpage.
    /// </summary>
    /// <value>
    /// The payment webpage.
    /// </value>
    /// <remarks>
    /// The 'Payment' frame is a URL pointing at a webpage that will handle the process of paying for this file.
    /// </remarks>
    public Id3v2UrlLinkFrame? PaymentWebpage
    {
        get => GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.PaymentWebpage, Id3v2Version.Id3v230);
        set => SetVersionedUrlLinkFrame(value, Id3v2UrlLinkFrameIdentifier.PaymentWebpage, Id3v2Version.Id3v230);
    }
}
