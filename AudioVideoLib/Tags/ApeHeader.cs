/*
 * Date: 2011-04-11
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTag
    {
        /// <summary>
        /// Size of the header, in bytes.
        /// </summary>
        public const int HeaderSize = 32;

        /// <summary>
        /// Size of the footer, in bytes.
        /// </summary>
        public const int FooterSize = 32;

        /// <summary>
        /// Gets the identifier of the <see cref="ApeTag" />.
        /// </summary>
        /// <value>
        /// The identifier of the <see cref="ApeTag" />.
        /// </value>
        /// <remarks>
        /// 64 bits APE Tag, identified as { 'A', 'P', 'E', 'T', 'A', 'G', 'E', 'X' }
        /// </remarks>
        public const string TagIdentifier = "APETAGEX";

        private static readonly byte[] TagIdentifierBytes = Encoding.ASCII.GetBytes(TagIdentifier);

        private int _flags = ApeHeaderFlags.ContainsNoFooter;

        private static readonly byte[] Reserved = new byte[StreamBuffer.Int64Size];

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the version of the <see cref="ApeTag"/>.
        /// </summary>
        /// <value>
        /// The version of the <see cref="ApeTag"/>.
        /// </value>
        /// <remarks>
        /// 32 bits version number.
        /// 1000 = Version 1.000 (old)
        /// 2000 = Version 2.000 (new)
        /// </remarks>
        public ApeVersion Version { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether a header should be added to the <see cref="ApeTag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if a header should be added to the <see cref="ApeTag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// An <see cref="ApeTag"/> with <see cref="Version"/> <see cref="ApeVersion.Version2"/> or higher needs to have either a footer or a header.
        /// When the value for this property is set to false, <see cref="UseFooter"/> will automatically be set to true.
        /// </remarks>
        public bool UseHeader
        {
            get
            {
                return UseHeaderFlag(Version, Flags);
            }

            set
            {
                if (Version >= ApeVersion.Version2)
                    SetUseHeaderFlag(value, true);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a footer should be added to the <see cref="ApeTag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if a footer should be added to the <see cref="ApeTag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// An <see cref="ApeTag"/> with <see cref="Version"/> <see cref="ApeVersion.Version2"/> or higher needs to have either a footer or a header.
        /// When the value for this property is set to false, <see cref="UseHeader"/> will automatically be set to true.
        /// </remarks>
        public bool UseFooter
        {
            get
            {
                return UseFooterFlag(Version, Flags);
            }

            set
            {
                if (Version >= ApeVersion.Version2)
                    SetUseFooterFlag(value, true);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ApeTag"/> is read only or read/write.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the <see cref="ApeTag"/> is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get
            {
                return IsReadOnlyFlag(Version, Flags);
            }

            set
            {
                if (Version >= ApeVersion.Version2)
                {
                    if (value)
                        Flags |= ApeHeaderFlags.IsReadOnly;
                    else
                        Flags &= ~ApeHeaderFlags.IsReadOnly;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the header is a header or footer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the header is header; otherwise, <c>false</c>.
        /// </value>
        public bool IsHeader
        {
            get
            {
                return IsHeaderFlag(Version, Flags);
            }

            private set
            {
                if (Version >= ApeVersion.Version2)
                {
                    if (value)
                        Flags |= ApeHeaderFlags.IsHeader;
                    else
                        Flags &= ~ApeHeaderFlags.IsHeader;
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>
        /// The flags.
        /// </value>
        /// <remarks>
        /// 32 bits global flags of the tag. There are also private flags for every item.
        /// <para/>
        /// APE Tags 1.0 do not use any of the APE Tag flags. All are set to zero on creation and ignored on reading.
        /// </remarks>
        private int Flags
        {
            get
            {
                return (Version >= ApeVersion.Version2) ? _flags : 0;
            }

            set
            {
                _flags = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsHeaderFlag(ApeVersion version, int flags)
        {
            return (version >= ApeVersion.Version2) && ((flags & ApeHeaderFlags.IsHeader) != 0);
        }

        private static bool UseHeaderFlag(ApeVersion version, int flags)
        {
            return (version >= ApeVersion.Version2) && ((flags & ApeHeaderFlags.ContainsHeader) != 0);
        }

        private static bool UseFooterFlag(ApeVersion version, int flags)
        {
            return (version < ApeVersion.Version2) || ((flags & ApeHeaderFlags.ContainsNoFooter) == 0);
        }

        private static bool IsReadOnlyFlag(ApeVersion version, int flags)
        {
            return (version >= ApeVersion.Version2) && ((flags & ApeHeaderFlags.IsReadOnly) != 0);
        }

        private void SetUseHeaderFlag(bool useHeader, bool forceUseFooter)
        {
            if (useHeader)
                Flags |= ApeHeaderFlags.ContainsHeader;
            else
                Flags &= ~ApeHeaderFlags.ContainsHeader;

            if (forceUseFooter && !useHeader)
                SetUseFooterFlag(true, false);
        }

        private void SetUseFooterFlag(bool useFooter, bool forceUseHeader)
        {
            // When the bit is 1, it means it has no footer...
            if (useFooter)
                Flags &= ~ApeHeaderFlags.ContainsNoFooter;
            else
                Flags |= ApeHeaderFlags.ContainsNoFooter;

            if (forceUseHeader && !useFooter)
                SetUseHeaderFlag(true, false);
        }
    }
}
