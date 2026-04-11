/*
 * Date: 2011-08-13
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Globalization;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing the ownership.
    /// </summary>
    /// <remarks>
    /// The ownership frame might be used as a reminder of a made transaction or, if signed, as proof.
    /// Note that the <see cref="Id3v2TermsOfUseFrame"/> frame and <see cref="Id3v2Tag.FileOwner"/> property are good to use in conjunction with this one.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2OwnershipFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _frameEncodingType;

        private string _pricePaid, _dateOfPurchase, _seller;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2OwnershipFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2OwnershipFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2OwnershipFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2OwnershipFrame(Id3v2Version version) : base(version)
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
        /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="Seller"/> is not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }

            set
            {
                if (!String.IsNullOrEmpty(Seller) && !IsValidTextString(Seller, value, false))
                    throw new InvalidDataException("Seller contains one or more invalid characters for the specified frame encoding type.");

                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the price paid.
        /// </summary>
        /// <value>
        /// The price paid.
        /// </value>
        /// <remarks>
        /// The first three characters of this field contains the currency used for the transaction, 
        /// encoded according to ISO-4217 alphabetic currency code.
        /// Concatenated to this is the actual price paid, as a numerical string using "." as the decimal separator.
        /// <para />
        /// Use <see cref="Id3v2Frame.IsValidCurrencyCode"/> to check if the first three characters of the value is a valid ISO-4217 currency code.
        /// </remarks>
        public string PricePaid
        {
            get
            {
                return _pricePaid;
            }

            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                    if ((value.Length < 3) || !IsValidCurrencyCode(value.Substring(0, 3)))
                        throw new InvalidDataException("value contains an invalid ISO-4217 alphabetic currency code.");

                    float price;
                    if (!float.TryParse(value.Substring(3), out price))
                        throw new InvalidDataException("value contains an invalid price paid decimal.");
                }
                _pricePaid = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the date of purchase.
        /// </summary>
        /// <value>
        /// The date of purchase.
        /// </value>
        /// <remarks>
        /// Date of purchase is an 8 character date string (YYYYMMDD).
        /// </remarks>
        public string DateOfPurchase
        {
            get
            {
                return _dateOfPurchase;
            }

            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                    DateTime dateTime;
                    if (!DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                        throw new InvalidDataException("value is not a valid date.");
                }
                _dateOfPurchase = value;
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
        public string Seller
        {
            get
            {
                return _seller;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _seller = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);

                    // Text encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Price paid
                    if (PricePaid != null)
                        stream.WriteString(PricePaid, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Date of purchase
                    if (DateOfPurchase != null)
                        stream.WriteString(DateOfPurchase, defaultEncoding);

                    // Preamble
                    stream.Write(Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding));

                    // Seller
                    if (Seller != null)
                        stream.WriteString(Seller, Id3v2FrameEncoding.GetEncoding(TextEncoding));

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
                    _pricePaid = stream.ReadString(defaultEncoding, true);
                    _dateOfPurchase = stream.ReadString(8, defaultEncoding, true);
                    _seller = stream.ReadString(encoding);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "OWNE"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2OwnershipFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2OwnershipFrame"/>.
        /// </summary>
        /// <param name="ownership">The <see cref="Id3v2OwnershipFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2OwnershipFrame ownership)
        {
            if (ReferenceEquals(null, ownership))
                return false;

            if (ReferenceEquals(this, ownership))
                return true;

            return ownership.Version == Version;
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
    }
}
