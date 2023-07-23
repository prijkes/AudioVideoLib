/*
 * Date: 2011-07-04
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing an commercial frame.
    /// </summary>
    /// <remarks>
    /// This frame enables several competing offers in the same tag by bundling all needed information.
    /// That makes this frame rather complex but it's an easier solution 
    /// than if one tries to achieve the same result with several frames.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2CommercialFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _validUntil, _priceString, _contactUrl, _nameOfSeller, _shortDescription, _pictureMimeType;

        private Id3v2AudioDeliveryType _audioDeliveryType;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2CommercialFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2CommercialFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2CommercialFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2CommercialFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="NameOfSeller"/> or the <see cref="ShortDescription"/> 
        /// are not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }

            set
            {
                if (!String.IsNullOrEmpty(NameOfSeller) && !IsValidTextString(NameOfSeller, value, false))
                    throw new InvalidDataException("NameOfSeller contains one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(ShortDescription) && !IsValidTextString(ShortDescription, TextEncoding, false))
                    throw new InvalidDataException("ShortDescription contains one or more invalid characters for the specified frame encoding type.");

                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the price string.
        /// </summary>
        /// <value>
        /// The price string.
        /// </value>
        /// <remarks>
        /// A price is constructed by one three character currency code, encoded according to ISO-4217 alphabetic currency code, 
        /// followed by a numerical value where "." is used as decimal separator.
        /// In the price string several prices may be concatenated, separated by a "/" character, but there may only be one currency of each type.
        /// </remarks>
        public string PriceString
        {
            get
            {
                return _priceString;
            }

            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                    List<string> l = new List<string>();
                    foreach (string s in value.Split('/'))
                    {
                        if ((s.Length < 3) || !IsValidCurrencyCode(s.Substring(0, 3)))
                            throw new InvalidDataException("value contains one or more invalid ISO-4217 alphabetic currency codes.");

                        string currency = s.Substring(0, 3);
                        if (l.Contains(currency))
                            throw new InvalidDataException("value can not contain multiple prices for the same currency.");

                        float price;
                        if (!float.TryParse(value.Substring(3), out price))
                            throw new InvalidDataException("value contains one or more invalid price paid decimals.");

                        l.Add(currency);
                    }
                }
                _priceString = value;
            }
        }

        /// <summary>
        /// Gets or sets the date of purchase.
        /// </summary>
        /// <value>
        /// The date of purchase.
        /// </value>
        /// <remarks>
        /// The valid until field is an 8 character date string in the format YYYYMMDD, describing for how long the price is valid.
        /// </remarks>
        public string ValidUntil
        {
            get
            {
                return (_validUntil.Length > 8) ? _validUntil.Substring(0, 8) : _validUntil;
             }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _validUntil = value;
                    return;
                }

                if (!IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                if (value.Length < 8)
                    throw new InvalidDataException("value is not a valid date.");

                string d = value.Substring(0, 8);
                DateTime dateTime;
                if (!DateTime.TryParseExact(d, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                    throw new InvalidDataException("value contains an invalid date.");

                _validUntil = d;
            }
        }

        /// <summary>
        /// Gets or sets the contact URL.
        /// </summary>
        /// <value>
        /// The contact URL.
        /// </value>
        /// <remarks>
        /// The contact URL with which the user can contact the seller.
        /// The contact URL should be a valid RFC 1738 URL.
        /// </remarks>
        public string ContactUrl
        {
            get
            {
                return _contactUrl;
            }

            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                    if (!IsValidUrl(value))
                        throw new InvalidDataException("value is not a valid RFC 1738 URL.");
                }
                _contactUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the received as field.
        /// </summary>
        /// <value>
        /// The received as field.
        /// </value>
        /// <remarks>
        /// The received as field describes how the audio is delivered when bought according to the <see cref="Id3v2AudioDeliveryType"/>.
        /// </remarks>
        public Id3v2AudioDeliveryType ReceivedAs
        {
            get
            {
                return _audioDeliveryType;
            }

            set
            {
                if (!IsValidAudioDeliveryType(value))
                    throw new ArgumentOutOfRangeException("value");

                _audioDeliveryType = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the seller.
        /// </summary>
        /// <value>
        /// The name of the seller.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public string NameOfSeller
        {
            get
            {
                return _nameOfSeller;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _nameOfSeller = value;
            }
        }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        /// <value>
        /// The short description.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public string ShortDescription
        {
            get
            {
                return _shortDescription;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _shortDescription = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the picture MIME.
        /// </summary>
        /// <value>
        /// The type of the picture MIME.
        /// </value>
        /// <remarks>
        /// Field containing information about which picture format is used.
        /// In the event that the MIME media type name is omitted, "image/" will be implied.
        /// Currently only "image/png" and "image/jpeg" are allowed.
        /// This field may be omitted if no picture is attached.
        /// </remarks>
        public string PictureMimeType
        {
            get
            {
                return _pictureMimeType;
            }

            set
            {
                if (!String.IsNullOrEmpty(value)
                    && (!String.Equals(value, "image/", StringComparison.OrdinalIgnoreCase)
                        && !String.Equals(value, "image/png", StringComparison.OrdinalIgnoreCase)
                        && !String.Equals(value, "image/jpeg", StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentOutOfRangeException("value", "value not allowed, currently only 'image/png' and 'image/jpeg' are allowed.");

                _pictureMimeType = value;
            }
        }

        /// <summary>
        /// Gets or sets the seller logo.
        /// </summary>
        /// <value>
        /// The logo of the seller.
        /// </value>
        /// <remarks>
        /// This field may be omitted if no picture is attached.
        /// </remarks>
        public byte[] SellerLogo { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Price string
                    if (PriceString != null)
                        stream.WriteString(PriceString, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Valid until
                    if (ValidUntil != null)
                        stream.WriteString(ValidUntil, defaultEncoding);

                    // Contact URL
                    if (ContactUrl != null)
                        stream.WriteString(ContactUrl, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Received as
                    stream.WriteByte((byte)ReceivedAs);

                    // Preamble
                    stream.Write(preamble);

                    // Name of seller
                    if (NameOfSeller != null)
                        stream.WriteString(NameOfSeller, encoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(encoding.GetBytes("\0"));

                    // Preamble
                    stream.Write(preamble);

                    // Description
                    if (ShortDescription != null)
                        stream.WriteString(ShortDescription, encoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(encoding.GetBytes("\0"));

                    // Picture MIME type
                    if (PictureMimeType != null)
                        stream.WriteString(PictureMimeType, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Seller logo
                    if (SellerLogo != null)
                        stream.Write(SellerLogo);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _priceString = stream.ReadString(defaultEncoding, true);
                    _validUntil = stream.ReadString(8, defaultEncoding, true);
                    _contactUrl = stream.ReadString(defaultEncoding, true);
                    _audioDeliveryType = (Id3v2AudioDeliveryType)stream.ReadByte();
                    _nameOfSeller = stream.ReadString(encoding);
                    _shortDescription = stream.ReadString(encoding);
                    _pictureMimeType = stream.ReadString(defaultEncoding);
                    SellerLogo = new byte[stream.Length - stream.Position];
                    stream.Read(SellerLogo, SellerLogo.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "COMR"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2CommercialFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2CommercialFrame"/>.
        /// </summary>
        /// <param name="commercial">The <see cref="Id3v2CommercialFrame"/>.</param>
        /// <returns>true if equal; false <see cref="Id3v2CommercialFrame"/>.</returns>
        /// <remarks>
        /// Both instances are equal when the following fields are equal (case-insensitive):
        /// * <see cref="Version"/>
        /// * <see cref="TextEncoding"/>
        /// * <see cref="PriceString"/>
        /// * <see cref="ValidUntil"/>
        /// * <see cref="ContactUrl"/>
        /// * <see cref="ReceivedAs"/>
        /// * <see cref="NameOfSeller"/>
        /// * <see cref="ShortDescription"/>
        /// * <see cref="PictureMimeType"/>
        /// * <see cref="SellerLogo"/>
        /// </remarks>
        public bool Equals(Id3v2CommercialFrame commercial)
        {
            if (ReferenceEquals(null, commercial))
                return false;

            if (ReferenceEquals(this, commercial))
                return true;

            return (commercial.Version == Version) && (commercial.TextEncoding == TextEncoding)
                   && String.Equals(commercial.PriceString, PriceString, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(commercial.ValidUntil, ValidUntil, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(commercial.ContactUrl, ContactUrl, StringComparison.OrdinalIgnoreCase)
                   && (commercial.ReceivedAs == ReceivedAs)
                   && String.Equals(commercial.NameOfSeller, NameOfSeller, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(commercial.ShortDescription, ShortDescription, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(commercial.PictureMimeType, PictureMimeType, StringComparison.OrdinalIgnoreCase)
                   && StreamBuffer.SequenceEqual(commercial.SellerLogo, SellerLogo);
        }

        /// <summary>
        /// Determines whether the specified version is supported by the frame.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsVersionSupported(Id3v2Version version)
        {
            return (version >= Id3v2Version.Id3v230);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidAudioDeliveryType(Id3v2AudioDeliveryType audioDeliveryType)
        {
            return Enum.TryParse(audioDeliveryType.ToString(), true, out audioDeliveryType);
        }
    }
}
