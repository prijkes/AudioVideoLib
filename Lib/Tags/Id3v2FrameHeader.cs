/*
 * Date: 2011-03-04
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
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
    /// The header for an <see cref="Id3v2Frame"/>.
    /// </summary>
    public partial class Id3v2Frame
    {
        private bool _tagAlterPreservation,
                     _fileAlterPreservation,
                     _isReadOnly,
                     _useCompression,
                     _useEncryption,
                     _useGroupingIdentity,
                     _useUnsynchronization,
                     _useDataLengthIndicator;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the frame version of the frame.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public Id3v2Version Version { get; private set; }

        /// <summary>
        /// Gets the identifier of this frame.
        /// </summary>
        /// <value>
        /// The identifier of the frame.
        /// </value>
        public virtual string Identifier { get; private set; }

        /// <summary>
        /// Gets or sets the type of encryption.
        /// </summary>
        /// <value>
        /// The encryption byte indicates with which method the frame should be encrypted.
        /// <para />
        /// The encryption type is only used when <see cref="UseEncryption"/> is <c>true</c>.
        /// </value>
        public byte EncryptionType { get; set; }

        /// <summary>
        /// Gets or sets the group identifier indicating the group this frame belongs to.
        /// </summary>
        /// <value>
        /// The group identifier byte used to indicate which group this frame is in.
        /// </value>
        /// <remarks>
        /// The group identifier is only used when <see cref="UseGroupingIdentity"/> is <c>true</c>.
        /// </remarks>
        public byte GroupIdentifier { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Id3v2Frame"/> should be preserved after alteration of the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id3v2Frame"/> should be preserved after alternation of the <see cref="Id3v2Tag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// This property tells the software what to do with this <see cref="Id3v2Frame"/> 
        /// if it is unknown and the <see cref="Id3v2Tag"/> is altered in any way.
        /// This applies to all kinds of alterations, including adding more padding and reordering the <see cref="Id3v2Tag.Frames"/>.
        /// </remarks>
        public bool TagAlterPreservation
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _tagAlterPreservation;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _tagAlterPreservation = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Id3v2Frame"/> should be preserved after alteration of the file.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id3v2Frame"/> should be preserved after alternation of the file; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// This property tells the software what to do with this <see cref="Id3v2Frame"/> 
        /// if it is unknown and the file, excluding the <see cref="Id3v2Tag"/>, is altered.
        /// This does not apply when the audio is completely replaced with other audio data.
        /// </remarks>
        public bool FileAlterPreservation
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _fileAlterPreservation;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _fileAlterPreservation = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the frame is flagged read only.
        /// </summary>
        /// <value><c>true</c> if the frame is flagged as read only; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// The read only flag, if set, tells the software that the contents of this frame is intended to be read only.
        /// Changing the contents might break something, e.g. a signature.
        /// If the contents are changed, without knowledge in why the frame was flagged read only
        /// and without taking the proper means to compensate, e.g. recalculating the signature, the flag bit will be cleared.
        /// </remarks>
        public bool IsReadOnly
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _isReadOnly;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _isReadOnly = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the frame uses compression.
        /// </summary>
        /// <value><c>true</c> if the frame uses compression; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// If the tag version is Id3v2.4.0, a data length indicator will be automatically added after the frame header,
        /// and the data length indicator flag will be set to true.
        /// </remarks>
        public bool UseCompression
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _useCompression;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                {
                    _useCompression = value;
                    if ((Version >= Id3v2Version.Id3v240) && value)
                    {
                        // A 'Data Length Indicator' byte MUST be included in the frame.
                        UseDataLengthIndicator = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the frame uses encryption.
        /// </summary>
        /// <value><c>true</c> if this instance uses encryption; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        public bool UseEncryption
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _useEncryption;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _useEncryption = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the frame uses a grouping identifier.
        /// </summary>
        /// <value><c>true</c> if this instance uses a grouping identifier; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// <para />
        /// See <see cref="GroupIdentifier"/> for setting the grouping identifier to use.
        /// The default used is 0.
        /// </remarks>
        public bool UseGroupingIdentity
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _useGroupingIdentity;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _useGroupingIdentity = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether unsynchronization should be used on this frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if unsynchronization should be used; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        public bool UseUnsynchronization
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) && _useUnsynchronization;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    _useUnsynchronization = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the frame uses a data length indicator field.
        /// </summary>
        /// <value><c>true</c> if the frame uses a data length indicator field; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// <para />
        /// If the compression flag is set, this flag will be set to true and a data length indicator will automatically be added after the header.
        /// Setting the data length indicator flag to false when <see cref="UseCompression"/> is true will not update the property.
        /// </remarks>
        public bool UseDataLengthIndicator
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) && _useDataLengthIndicator;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                {
                    if (value || !UseCompression)
                        _useDataLengthIndicator = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the data length indicator as the 'Frame length' if all of the frame format flags were zeroed.
        /// </summary>
        /// <remarks>
        /// The data length indicator is only only set when reading the <see cref="Id3v2Frame"/> from a stream and
        /// <see cref="UseDataLengthIndicator"/> is <c>true</c>.
        /// <para />
        /// This value should always match <see cref="Data"/>.Length.
        /// </remarks>
        private int DataLengthIndicator { get; set; }

        /// <summary>
        /// Gets or sets the flags of this frame. The flags value is stored as short (2 bytes) for all versions.
        /// </summary>
        private int Flags
        {
            get
            {
                int flags = 0;

                // Default is true; only add the flag when it's not.
                if (!TagAlterPreservation)
                {
                    // 0 - Frame should be preserved.
                    // 1 - Frame should be discarded.
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.TagAlterPreservation
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.TagAlterPreservation
                                 : 0;
                }

                // Default is true; only add the flag when it's not.
                if (!FileAlterPreservation)
                {
                    // 0 - Frame should be preserved.
                    // 1 - Frame should be discarded.
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.FileAlterPreservation
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.FileAlterPreservation
                                 : 0;
                }

                if (IsReadOnly)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.ReadOnly
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.ReadOnly
                                 : 0;
                }

                if (UseCompression)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.Compressed
                                  : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.Compressed
                                  : 0;
                }

                if (UseEncryption)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.Encrypted
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.Encrypted
                                 : 0;
                }

                if (UseGroupingIdentity)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230FrameHeaderFlags.GroupingIdentity
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.GroupingIdentity
                                 : 0;
                }

                if (UseUnsynchronization)
                    flags |= (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.Unsynchronized : 0;
                
                if (UseDataLengthIndicator)
                    flags |= (Version >= Id3v2Version.Id3v240) ? Id3v240FrameHeaderFlags.DataLengthIndicator : 0;

                return flags;
            }

            set
            {
                // 0 - Frame should be preserved.
                // 1 - Frame should be discarded.
                TagAlterPreservation = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.TagAlterPreservation)
                                            : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.TagAlterPreservation)
                                            : 0) == 0;

                // 0 - Frame should be preserved.
                // 1 - Frame should be discarded.
                FileAlterPreservation = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.FileAlterPreservation)
                                            : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.FileAlterPreservation)
                                            : 0) == 0;

                IsReadOnly = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.ReadOnly)
                                  : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.ReadOnly)
                                  : 0) != 0;

                UseCompression = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.Compressed)
                                      : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.Compressed)
                                      : 0) != 0;

                UseEncryption = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.Encrypted)
                                      : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.Encrypted)
                                      : 0) != 0;

                UseGroupingIdentity = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230FrameHeaderFlags.GroupingIdentity)
                                      : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240FrameHeaderFlags.GroupingIdentity)
                                      : 0) != 0;

                UseUnsynchronization = (Version >= Id3v2Version.Id3v240) && (value & Id3v240FrameHeaderFlags.Unsynchronized) != 0;

                UseDataLengthIndicator = (Version >= Id3v2Version.Id3v240) && (value & Id3v240FrameHeaderFlags.DataLengthIndicator) != 0;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the <see cref="Identifier"/>, in bytes.
        /// </summary>
        /// <remarks>
        /// The length of the name field is 3 bytes for <see cref="Id3v2Version.Id3v220"/> 
        /// and 4 bytes for <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        private int IdentifierFieldLength
        {
            get { return GetIdentifierFieldLength(Version); }
        }

        /// <summary>
        /// Gets the length of the data size field, in bytes.
        /// </summary>
        /// <remarks>
        /// The length of the data size field is 3 bytes for <see cref="Id3v2Version.Id3v220"/> 
        /// and 4 bytes for <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        private int DataSizeFieldLength
        {
            get { return GetDataSizeFieldLength(Version); }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the size of the frame when decompressed.
        /// </summary>
        /// <remarks>
        /// The decompressed frame size is only set when reading the <see cref="Id3v2Frame"/> from a stream 
        /// and <see cref="UseCompression"/> is <c>true</c>.
        /// <para />
        /// This value should always match <see cref="Data"/>.Length.
        /// </remarks>
        private int DecompressedFrameSize { get; set; }
    }
}
