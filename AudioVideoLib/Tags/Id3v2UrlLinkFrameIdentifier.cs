namespace AudioVideoLib.Tags;

/// <summary>
/// A list of pre-defined known <see cref="Id3v2UrlLinkFrame"/> identifiers in an <see cref="Id3v2Tag"/>.
/// </summary>
public enum Id3v2UrlLinkFrameIdentifier
{
    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.CommercialInformations"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    CommercialInformations,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.CopyrightInformation"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    CopyrightInformation,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.OfficialArtistWebpage"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    OfficialArtistWebpage,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.OfficialAudioFileWebpage"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    OfficialAudioFileWebpage,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.OfficialAudioSource"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    OfficialAudioSource,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.PublishersOfficialWebpage"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    PublishersOfficialWebpage,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.OfficialInternetRadioStationHomepage"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    OfficialInternetRadioStationHomepage,

    /// <summary>
    /// Identifier for the <see cref="Id3v2Tag.PaymentWebpage"/> <see cref="Id3v2TextFrame"/>.
    /// </summary>
    PaymentWebpage
}
