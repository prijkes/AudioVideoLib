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
    /// Class for storing a group identification registration.
    /// </summary>
    /// <remarks>
    /// This frame enables grouping of otherwise unrelated frames.
    /// This can be used when some frames are to be signed.
    /// To identify which frames belongs to a set of frames a group identifier must be registered in the tag with this frame.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2GroupIdentificationRegistrationFrame : Id3v2Frame
    {
        private string _ownerIdentifier;

        private byte _groupSymbol;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2GroupIdentificationRegistrationFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2GroupIdentificationRegistrationFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2GroupIdentificationRegistrationFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2GroupIdentificationRegistrationFrame(Id3v2Version version) : base(version)
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
        /// or a link to a location where an email address can be found, that belongs to the organization responsible for this grouping.
        /// Questions regarding the grouping should be sent to the indicated email address.
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
        /// The 'Group symbol' contains a value that associates the frame with this group throughout the whole tag.
        /// Values below 0x80 are reserved.
        /// For version <see cref="Id3v2Version.Id3v240"/> and higher, values above 0xF0 are also reserved.
        /// </remarks>
        public byte GroupSymbol
        {
            get
            {
                return _groupSymbol;
            }

            set
            {
                if (value < 0x80)
                    throw new ArgumentOutOfRangeException("value", "values below 0x80 are reserved.");

                if ((Version >= Id3v2Version.Id3v240) && (value > 0xF0))
                    throw new ArgumentOutOfRangeException("value", "values above 0xF0 are reserved.");

                _groupSymbol = value;
            }
        }

        /// <summary>
        /// Gets or sets the group dependent data.
        /// </summary>
        /// <value>
        /// The group dependent data.
        /// </value>
        /// <remarks>
        /// This field is optional can be used for some group specific data, e.g. a digital signature.
        /// </remarks>
        public byte[] GroupDependentData { get; set; }

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

                    // Group symbol
                    stream.WriteByte(GroupSymbol);

                    // Group dependent data
                    if (GroupDependentData != null)
                        stream.Write(GroupDependentData);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _ownerIdentifier = stream.ReadString(defaultEncoding, true);
                    _groupSymbol = (byte)stream.ReadByte();
                    GroupDependentData = new byte[stream.Length - stream.Position];
                    stream.Read(GroupDependentData, GroupDependentData.Length);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "GRID"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2GroupIdentificationRegistrationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2GroupIdentificationRegistrationFrame"/>.
        /// </summary>
        /// <param name="gir">The <see cref="Id3v2GroupIdentificationRegistrationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/>, <see cref="GroupSymbol"/> and <see cref="OwnerIdentifier"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2GroupIdentificationRegistrationFrame gir)
        {
            if (ReferenceEquals(null, gir))
                return false;

            if (ReferenceEquals(this, gir))
                return true;

            return (gir.Version == Version)
                   && ((gir.GroupSymbol == GroupSymbol) || String.Equals(gir.OwnerIdentifier, OwnerIdentifier, StringComparison.OrdinalIgnoreCase));
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
