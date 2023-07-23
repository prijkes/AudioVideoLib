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
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing an Id3v2 encrypted meta frame.
    /// </summary>
    /// <remarks>
    /// This frame contains one or more encrypted frames.
    /// <para />
    /// This enables protection of copyrighted information such as pictures and text, that people might want to pay extra for.
    /// Since standardization of such an encryption scheme is beyond this document, 
    /// all <see cref="Id3v2EncryptedMetaFrame"/> frames contain an URL [URL] which can be an email address, 
    /// or a link to a location where an email address can be found, 
    /// that belongs to the organization responsible for this specific encrypted meta frame.
    /// Questions regarding the encrypted frame should be sent to the indicated email address.
    /// <para />
    /// When an Id3v2 decoder encounters an <see cref="Id3v2EncryptedMetaFrame"/> frame, 
    /// it should send the data block to the 'plugin' with the corresponding <see cref="OwnerIdentifier"/> 
    /// and expect to receive either a data block with one or several Id3v2 frames after each other or an error.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to but not including <see cref="Id3v2Version.Id3v230"/>.
    /// </remarks>
    public sealed class Id3v2EncryptedMetaFrame : Id3v2Frame
    {
        private string _ownerIdentifier, _contentExplanation;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EncryptedMetaFrame"/> class with version <see cref="Id3v2Version.Id3v220"/>.
        /// </summary>
        public Id3v2EncryptedMetaFrame() : base(Id3v2Version.Id3v220)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EncryptedMetaFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2EncryptedMetaFrame(Id3v2Version version) : base(version)
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
        /// The owner identifier is terminated string with a URL [URL] containing an email address, 
        /// or a link to a location where an email address can be found, 
        /// that belongs to the organization responsible for this specific encrypted meta frame.
        /// <para />
        /// Questions regarding the encrypted frame should be sent to the indicated email address.
        /// If the owner identifier is empty, the whole frame should be ignored, and preferably be removed.
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
        /// Gets or sets the content explanation.
        /// </summary>
        /// <value>
        /// The content explanation.
        /// </value>
        /// <remarks>
        /// The content explanation is a short content description and explanation as to why it's encrypted.
        /// </remarks>
        public string ContentExplanation
        {
            get 
            {
                return _contentExplanation;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                _contentExplanation = value;
            }
        }

        /// <summary>
        /// Gets or sets the encrypted data block.
        /// </summary>
        /// <value>
        /// The encrypted data block.
        /// </value>
        public byte[] EncryptedDataBlock { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Owner Identifier
                    if (OwnerIdentifier != null)
                        stream.WriteString(OwnerIdentifier, defaultEncoding);

                    // 0x00
                    stream.WriteByte(0x00);

                    // Content/explanation
                    if (ContentExplanation != null)
                        stream.WriteString(ContentExplanation, defaultEncoding);

                    // 0x00
                    stream.WriteByte(0x00);

                    // Encrypted data block
                    if (EncryptedDataBlock != null)
                        stream.Write(EncryptedDataBlock);

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
                    _ownerIdentifier = stream.ReadString(defaultEncoding);
                    _contentExplanation = stream.ReadString(defaultEncoding);
                    EncryptedDataBlock = new byte[stream.Length - stream.Position];
                    stream.Read(EncryptedDataBlock, EncryptedDataBlock.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "CRM"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2EncryptedMetaFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2EncryptedMetaFrame"/>.
        /// </summary>
        /// <param name="emf">The <see cref="Id3v2EncryptedMetaFrame"/>.</param>
        /// <returns>
        /// true if equal; false otherwise.
        /// </returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="OwnerIdentifier"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2EncryptedMetaFrame emf)
        {
            if (ReferenceEquals(null, emf))
                return false;

            if (ReferenceEquals(this, emf))
                return true;

            return (emf.Version == Version) && String.Equals(emf.OwnerIdentifier, OwnerIdentifier, StringComparison.OrdinalIgnoreCase);
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
            return (version < Id3v2Version.Id3v230);
        }
    }
}
