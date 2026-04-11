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

namespace AudioVideoLib.Tags
{
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
        public Id3v2UrlLinkFrame OfficialInternetRadioStationHomepage
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.OfficialInternetRadioStationHomepage) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(OfficialInternetRadioStationHomepage);
                else
                    SetFrame(value);
            }
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
        public Id3v2UrlLinkFrame PaymentWebpage
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetUrlLinkFrame(Id3v2UrlLinkFrameIdentifier.PaymentWebpage) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(PaymentWebpage);
                else
                    SetFrame(value);
            }
        }
    }
}
