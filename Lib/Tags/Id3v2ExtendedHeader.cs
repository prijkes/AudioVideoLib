/*
 * Date: 2011-03-04
 * Sources used: 
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// The extended header contains information that can provide further insight in the structure of the <see cref="Id3v2Tag"/>, 
    /// but is not vital to the correct parsing of the <see cref="Id3v2Tag"/> information; hence the extended header is optional.
    /// </summary>
    public sealed partial class Id3v2ExtendedHeader
    {
        private int _extendedFlagsFieldLength;

        private Id3v2TagRestrictions _tagRestrictions;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the size of the padding.
        /// </summary>
        /// <value>
        /// The size of the padding.
        /// </value>
        public int PaddingSize { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether the present tag is an update of a tag found earlier in the present file or stream.
        /// </summary>
        /// <value>
        /// <c>true</c> if the present tag is an update of a tag found earlier in the present file or stream otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for <see cref="Id3v2Version.Id3v240"/> and later.
        /// <para />
        /// If this flag is set, the present tag is an update of a tag found earlier in the present file or stream.
        /// If frames defined as unique are found in the present tag, they are to override any corresponding ones found in the earlier tag.
        /// </remarks>
        /// b - Tag is an update
        /// This flag has no corresponding data.
        /// Flag data length      $00
        public bool TagIsUpdate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CRC data is present.
        /// </summary>
        /// <value>
        /// <c>true</c> if CRC data is present; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// If set to <c>true</c>, a CRC-32 [ISO-3309] data is included in the extended header.
        /// </remarks>
        public bool CrcDataPresent { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Id3v2TagRestrictions"/>.
        /// </summary>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// <para />
        /// The <see cref="Id3v2TagRestrictions"/> indicate how the <see cref="Id3v2Tag"/> was restricted before encoding.
        /// </remarks>
        public Id3v2TagRestrictions TagRestrictions
        {
            get
            {
                return _tagRestrictions;
            }

            set
            {
                TagIsRestricted = (value != null);
                _tagRestrictions = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Id3v2Tag"/> is restricted.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id3v2Tag"/> is restricted; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property is only used for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// <para />
        /// The tag can be restricted by modifying the <see cref="TagRestrictions"/> property with <see cref="Id3v2TagRestrictions"/>.
        /// </remarks>
        public bool TagIsRestricted { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ExtendedHeader" /> class reading the <see cref="extendedFlags"/> for the specified <see cref="version"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="extendedFlags">The extended flags.</param>
        public static Id3v2ExtendedHeader InitExtendedHeader(Id3v2Version version, int extendedFlags)
        {
            Id3v2ExtendedHeader header = new Id3v2ExtendedHeader();
            header.SetFlags(version, extendedFlags);
            return header;
        }
        /// <summary>
        /// Gets the length of the tag restrictions data for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <value>
        /// The length of the tag restrictions data.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        public static int GetTagRestrictionsDataLength(Id3v2Version version)
        {
            return (version >= Id3v2Version.Id3v240) ? 1 : 0;
        }

        /// <summary>
        /// Gets the length of the CRC data for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <value>
        /// The length of the CRC data.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        public static int GetCrcDataLength(Id3v2Version version)
        {
            if (version < Id3v2Version.Id3v230)
                return 0;

            if (version < Id3v2Version.Id3v240)
                return 4;

            return (version >= Id3v2Version.Id3v240) ? 5 : 0;
        }

        /// <summary>
        /// Gets the length of the <see cref="TagIsUpdate"/> data field for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <value>
        /// The length of the <see cref="TagIsUpdate"/> data field.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        public static int GetTagIsUpdateDataLength(Id3v2Version version)
        {
            return (version >= Id3v2Version.Id3v240) ? 1 : 0;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the size of the extended header for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// <see cref="Id3v2Version.Id3v230"/>:
        /// Where the extended header size, currently 6 or 10 bytes, excludes itself.
        /// The extended header is considered separate from the header proper, and as such is subject to unsynchronization.
        /// <para />
        /// <see cref="Id3v2Version.Id3v240"/> and later:
        /// Where the extended header size is the size of the whole extended header, stored as a 32 bit synchsafe integer.
        /// An extended header can thus never have a size of fewer than six bytes.
        /// </remarks>
        public int GetHeaderSize(Id3v2Version version)
        {
            if ((version >= Id3v2Version.Id3v230) && (version < Id3v2Version.Id3v240))
                return 6 + (CrcDataPresent ? GetCrcDataLength(version) : 0);

            if (version >= Id3v2Version.Id3v240)
                return 6 + (TagIsUpdate ? GetTagIsUpdateDataLength(version) : 0) + (CrcDataPresent ? GetCrcDataLength(version) : 0)
                       + (TagIsRestricted ? GetTagRestrictionsDataLength(version) : 0);

            return 0;
        }
        
        /// <summary>
        /// Gets the extended flags for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <value>
        /// The extended flags.
        /// </value>
        /// The extended flags field, with its size described by 'number of flag bytes', is defined as: %0bcd0000
        public int GetFlags(Id3v2Version version)
        {
            int flags = 0;
            if (TagIsUpdate)
            {
                flags |= (version >= Id3v2Version.Id3v240) ? Id3v240ExtendedHeaderFlags.TagIsUpdate : 0;
            }

            if (CrcDataPresent)
            {
                flags |= ((version >= Id3v2Version.Id3v230) && (version < Id3v2Version.Id3v240))
                             ? Id3v230ExtendedHeaderFlags.CrcPresent
                             : (version >= Id3v2Version.Id3v240) ? Id3v240ExtendedHeaderFlags.CrcPresent : 0;
            }

            if (TagIsRestricted)
            {
                flags |= (version >= Id3v2Version.Id3v240) ? Id3v240ExtendedHeaderFlags.TagIsRestricted : 0;
            }

            return flags;
        }

        /// <summary>
        /// Gets the length of the extended flags field for the specified <see cref="Id3v2Version"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The length of the extended flags field, in bytes.</returns>
        public int GetFlagsFieldLength(Id3v2Version version)
        {
            if (version < Id3v2Version.Id3v230)
                return 0;

            if (version < Id3v2Version.Id3v240)
                return 2;

            return (version == Id3v2Version.Id3v240) ? 1 : _extendedFlagsFieldLength;
        }

        /// <summary>
        /// Sets the length of the extended flags, in bytes.
        /// </summary>
        /// <remarks>
        /// Length of the extended flags, in bytes; this should be 2 bytes for <see cref="Id3v2Version.Id3v230"/> and 1 byte for <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public void SetExtendedFlagsFieldLength(int extendedFlagsFieldLength)
        {
            _extendedFlagsFieldLength = extendedFlagsFieldLength;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object extendedHeader)
        {
            return Equals(extendedHeader as Id3v2ExtendedHeader);
        }

        /// <summary>
        /// Equals the specified extended header.
        /// </summary>
        /// <param name="extendedHeader">The extended header.</param>
        /// <returns></returns>
        public bool Equals(Id3v2ExtendedHeader extendedHeader)
        {
            if (ReferenceEquals(null, extendedHeader))
                return false;

            if (ReferenceEquals(this, extendedHeader))
                return true;

            return (TagRestrictions != null) && (extendedHeader.TagRestrictions != null)
                   && StreamBuffer.SequenceEqual(extendedHeader.TagRestrictions.ToByte(), TagRestrictions.ToByte());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 397;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void SetFlags(Id3v2Version version, int extendedFlags)
        {
            TagIsUpdate = ((version >= Id3v2Version.Id3v240) ? (extendedFlags & Id3v240ExtendedHeaderFlags.TagIsUpdate) : 0) != 0;

            CrcDataPresent = (((version >= Id3v2Version.Id3v230) && (version < Id3v2Version.Id3v240))
                                  ? (extendedFlags & Id3v230ExtendedHeaderFlags.CrcPresent)
                                  : (version >= Id3v2Version.Id3v240) ? (extendedFlags & Id3v240ExtendedHeaderFlags.CrcPresent) : 0) != 0;

            TagIsRestricted = ((version >= Id3v2Version.Id3v240) ? (extendedFlags & Id3v240ExtendedHeaderFlags.TagIsRestricted) : 0) != 0;
        }
    }
}
