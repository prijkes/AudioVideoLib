/*
 * Date: 2011-05-28
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
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing information for identifying the audio file in a database
    /// that may contain more information relevant to the content.
    /// Since standardization of such a database is beyond this document, this class only holds contact information for such a database.
    /// </summary>
    /// <remarks>
    /// The purpose of the <see cref="Id3v2UniqueFileIdentifierFrame"/> frame is to be able to identify the audio file in a database that may contain more information relevant to the content.
    /// All <see cref="Id3v2UniqueFileIdentifierFrame"/>s contain an URL of an email address, or a link to a location where an email address can be found,
    /// that belongs to the organization responsible for this specific database implementation.
    /// Questions regarding the database should be sent to the indicated email address.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2UniqueFileIdentifierFrame : Id3v2Frame
    {
        private string _ownerIdentifier;

        private byte[] _identifierData;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2UniqueFileIdentifierFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2UniqueFileIdentifierFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2UniqueFileIdentifierFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2UniqueFileIdentifierFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a null-terminated string with a URL containing an email address,
        /// or a link to a location where an email address can be found.
        /// Questions regarding the database should be sent to the indicated email address.
        /// The URL should not be used for the actual database queries.
        /// The string "http://www.id3.org/dummy/ufid.html" should be used for tests.
        /// </summary>
        /// <remarks>
        /// For <see cref="Id3v2Version.Id3v230"/> and later: The owner identifier must be non-empty (more than just a termination).
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
                // The 'Owner identifier' must be non-empty (more than just a termination).
                if ((Version >= Id3v2Version.Id3v230) && String.IsNullOrEmpty(value))
                    throw new InvalidDataException("value may not be empty.");

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
        /// Gets or sets the identifier, which may be up to 64 bytes.
        /// </summary>
        /// <remarks>
        /// Only the first 64 bytes will be taken if there are more than 64 bytes in the new value.
        /// </remarks>
        public byte[] IdentifierData
        {
            get
            {
                return _identifierData;
            }

            set
            {
                _identifierData = (value != null) ? value.Take(64).ToArray() : null;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override byte[] Data
        {
            get
            {
                // Use Default as default encoding (according to spec).
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Owner identifier
                    if (OwnerIdentifier != null)
                        stream.WriteString(OwnerIdentifier, defaultEncoding);

                    // 0x00
                    stream.WriteByte(0x00);

                    // Identifier value
                    if (IdentifierData != null)
                        stream.Write(IdentifierData);

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
                    _identifierData = new byte[stream.Length - stream.Position];
                    stream.Read(_identifierData, _identifierData.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "UFI" : "UFID"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2UniqueFileIdentifierFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2UniqueFileIdentifierFrame"/>.
        /// </summary>
        /// <param name="ufi">The <see cref="Id3v2UniqueFileIdentifierFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="OwnerIdentifier"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2UniqueFileIdentifierFrame ufi)
        {
            if (ReferenceEquals(null, ufi))
                return false;

            if (ReferenceEquals(this, ufi))
                return true;

            return (ufi.Version == Version) && String.Equals(ufi.OwnerIdentifier, OwnerIdentifier, StringComparison.OrdinalIgnoreCase);
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
            return true;
        }
    }
}
