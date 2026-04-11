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
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing an Id3v2 encryption method registration.
    /// </summary>
    /// <remarks>
    /// To identify with which method a frame has been encrypted the encryption method must be registered in the tag with this frame.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2EncryptionMethodRegistrationFrame : Id3v2Frame
    {
        private string _ownerIdentifier;

        private byte _methodSymbol;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EncryptionMethodRegistrationFrame"/> class with version <see cref="Id3v2Version.Id3v220"/>.
        /// </summary>
        public Id3v2EncryptionMethodRegistrationFrame() : base(Id3v2Version.Id3v220)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EncryptionMethodRegistrationFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2EncryptionMethodRegistrationFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the owner identifier.
        /// </summary>
        /// <value>
        /// The owner identifier.
        /// </value>
        /// <remarks>
        /// The owner identifier is a null-terminated string with a URL containing an email address, 
        /// or a link to a location where an email address can be found, 
        /// that belongs to the organization responsible for this specific encryption method.
        /// Questions regarding the encryption method should be sent to the indicated email address.
        /// <para />
        /// The URL must be a valid RFC 1738 URL, use <see cref="Id3v2Frame.IsValidUrl"/> to check if a value is a valid URL.
        /// </remarks>
        public string OwnerIdentifier
        {
            get
            {
                return _ownerIdentifier;
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
                _ownerIdentifier = value;
            }
        }

        /// <summary>
        /// Gets or sets the method symbol.
        /// </summary>
        /// <value>
        /// The method symbol.
        /// </value>
        /// <remarks>
        /// The method symbol contains a value that is associated with this method throughout the whole tag.
        /// Values below 0x80 are reserved.
        /// For version <see cref="Id3v2Version.Id3v240"/> and higher, values above 0xF0 are also reserved.
        /// </remarks>
        public byte MethodSymbol
        {
            get
            {
                return _methodSymbol;
            }

            set
            {
                if (value < 0x80)
                    throw new ArgumentOutOfRangeException("value", "values below 0x80 are reserved.");

                if ((Version >= Id3v2Version.Id3v240) && (value > 0xF0))
                    throw new ArgumentOutOfRangeException("value", "values above 0xF0 are reserved.");

                _methodSymbol = value;
            }
        }

        /// <summary>
        /// Gets or sets the encryption data.
        /// </summary>
        /// <value>
        /// The encryption data.
        /// </value>
        /// <remarks>
        /// The encryption data field is optional.
        /// </remarks>
        public byte[] EncryptionData { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Owner identifier
                    if (OwnerIdentifier != null)
                        stream.WriteString(OwnerIdentifier, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Method symbol
                    stream.WriteByte(MethodSymbol);

                    // Encryption data
                    if (EncryptionData != null)
                        stream.Write(EncryptionData);

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
                    _ownerIdentifier = stream.ReadString(defaultEncoding, true);
                    _methodSymbol = (byte)stream.ReadByte();
                    EncryptionData = new byte[stream.Length - stream.Position];
                    stream.Read(EncryptionData, EncryptionData.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "ENCR"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2EncryptionMethodRegistrationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2EncryptionMethodRegistrationFrame"/>.
        /// </summary>
        /// <param name="encryptionMethodRegistrationFrame">The <see cref="Id3v2EncryptionMethodRegistrationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/>, 
        /// <see cref="MethodSymbol"/> and <see cref="OwnerIdentifier"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2EncryptionMethodRegistrationFrame encryptionMethodRegistrationFrame)
        {
            if (ReferenceEquals(null, encryptionMethodRegistrationFrame))
                return false;

            if (ReferenceEquals(this, encryptionMethodRegistrationFrame))
                return true;

            return (encryptionMethodRegistrationFrame.Version == Version)
                   && ((encryptionMethodRegistrationFrame.MethodSymbol == MethodSymbol)
                       || String.Equals(encryptionMethodRegistrationFrame.OwnerIdentifier, OwnerIdentifier, StringComparison.OrdinalIgnoreCase));
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
