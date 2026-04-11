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
    /// Class for storing a private frame.
    /// </summary>
    /// <remarks>
    /// This frame is used to contain information from a software producer 
    /// that its program uses and does not fit into the other frames.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2PrivateFrame : Id3v2Frame
    {
        private string _ownerIdentifier;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PrivateFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        /// <remarks>
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2PrivateFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PrivateFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2PrivateFrame(Id3v2Version version) : base(version)
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
        /// The 'Owner identifier' is a null-terminated string with a URL containing an email address, 
        /// or a link to a location where an email address can be found, that belongs to the organization responsible for the frame.
        /// Questions regarding the frame should be sent to the indicated email address.
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
        /// Gets or sets the private data.
        /// </summary>
        /// <value>
        /// The private data.
        /// </value>
        public byte[] PrivateData { get; set; }

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

                    // Private data
                    if (PrivateData != null)
                        stream.Write(PrivateData);

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
                    PrivateData = new byte[stream.Length - stream.Position];
                    stream.Read(PrivateData, PrivateData.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "PRIV"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2PrivateFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2PrivateFrame"/>.
        /// </summary>
        /// <param name="pf">The <see cref="Id3v2PrivateFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="PrivateData"/> properties are equal.
        /// </remarks>
        public bool Equals(Id3v2PrivateFrame pf)
        {
            if (ReferenceEquals(null, pf))
                return false;

            if (ReferenceEquals(this, pf))
                return true;

            return (pf.Version == Version) && StreamBuffer.SequenceEqual(pf.PrivateData, PrivateData);
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
            return version >= Id3v2Version.Id3v230;
        }
    }
}
