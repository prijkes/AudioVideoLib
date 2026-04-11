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

using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        /// <summary>
        /// The size of the <see cref="Id3v2Tag"/> header, in bytes.
        /// </summary>
        public const int HeaderSize = 10;

        /// <summary>
        /// The size of the <see cref="Id3v2Tag"/> footer, in bytes.
        /// </summary>
        public const int FooterSize = 10;

        /// <summary>
        /// The header identifier for an <see cref="Id3v2Tag"/>.
        /// The first three bytes of the tag are always "ID3" to indicate that this is an ID3 tag.
        /// </summary>
        public const string HeaderIdentifier = "ID3";

        /// <summary>
        /// The footer identifier for an <see cref="Id3v2Tag"/>.
        /// </summary>
        public const string FooterIdentifier = "3DI";

        private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(FooterIdentifier);

        private Id3v2Version _version;

        private bool _useCompression, _useExtendedHeader, _tagIsExperimental, _useFooter, _useHeader = true;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the version of the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when value is a valid out of valid ranges.</exception>
        /// <remarks>
        /// When changing the version, the version of all <see cref="Frames"/> will be changed as well.
        /// Frames which don't support the new version will be removed.
        /// </remarks>
        public Id3v2Version Version
        {
            get
            {
                return _version;
            }

            set
            {
                if (!IsValidVersion(value))
                    throw new ArgumentOutOfRangeException("value");

                for (int i = 0; i < _frames.Count; i++)
                {
                    if (!_frames[i].IsVersionSupported(value))
                        _frames.RemoveAt(i);
                    else
                        _frames[i].SetVersion(value);
                }
                _version = value;
            }
        }
        
        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Id3v2Tag"/> uses unsynchronization.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id3v2Tag"/> has been unsynchronized; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// If <c>true</c> the frame data will be unsynchronized as necessary.
        /// </remarks>
        public bool UseUnsynchronization { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether compression should be used.
        /// </summary>
        /// <value>
        /// <c>true</c> if compression should be used; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Compression is only supported in version <see cref="Id3v2Version.Id3v221"/> and earlier.
        /// <para />
        /// Since no compression scheme has been decided for version <see cref="Id3v2Version.Id3v221"/>, 
        /// the ID3 decoder (for now) should just ignore the entire tag if the compression property is set.
        /// </remarks>
        public bool UseCompression
        {
            get
            {
                return (Version < Id3v2Version.Id3v230) && _useCompression;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    _useCompression = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether an extended header should be added to the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if an extended header should be added to the <see cref="Id3v2Tag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        public bool UseExtendedHeader
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _useExtendedHeader;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _useExtendedHeader = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Id3v2Tag"/> is in experimental stage.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id3v2Tag"/> is in experimental stage; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later 
        /// and should always be set when the <see cref="Id3v2Tag"/> is in an experimental stage.
        /// </remarks>
        public bool TagIsExperimental
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) && _tagIsExperimental;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v230)
                    _tagIsExperimental = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a footer should be added to the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if a footer should be added to the <see cref="Id3v2Tag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This only applies for version <see cref="Id3v2Version.Id3v240"/> and later.
        /// </remarks>
        public bool UseFooter
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) && _useFooter;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    _useFooter = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether a header should be added to the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if a header should be added to the <see cref="Id3v2Tag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// For versions below <see cref="Id3v2Version.Id3v240"/>, this will always be <c>true</c> and can not be changed.
        /// <para />
        /// If this is set to <c>false</c>, <see cref="UseFooter"/> will be set to <c>true</c>.
        /// </remarks>
        public bool UseHeader
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) || _useHeader;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                {
                    _useHeader = value;

                    if (!value)
                        UseFooter = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags of the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <remarks>
        /// The flags indicate which fields are used in an <see cref="Id3v2Tag"/>.
        /// </remarks>
        public int Flags
        {
            get
            {
                int flags = 0;
                if (UseUnsynchronization)
                {
                    flags |= (Version < Id3v2Version.Id3v230) ? Id3v220HeaderFlags.Unsynchronization
                                 : (Version < Id3v2Version.Id3v240) ? Id3v230HeaderFlags.Unsynchronization 
                                 : Id3v240HeaderFlags.Unsynchronization;
                }

                if (UseCompression)
                {
                    flags |= (Version < Id3v2Version.Id3v230) ? Id3v220HeaderFlags.Compression : 0;
                }

                if (UseExtendedHeader)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230HeaderFlags.ExtendedHeader
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240HeaderFlags.ExtendedHeader 
                                 : 0;
                }

                if (TagIsExperimental)
                {
                    flags |= ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? Id3v230HeaderFlags.ExperimentalIndicator
                                 : (Version >= Id3v2Version.Id3v240) ? Id3v240HeaderFlags.ExperimentalIndicator
                                 : 0;
                }

                if (UseFooter)
                {
                    flags |= (Version >= Id3v2Version.Id3v240) ? Id3v240HeaderFlags.Footer : 0;
                }
                return flags;
            }

            private set
            {
                UseUnsynchronization = ((Version < Id3v2Version.Id3v230) ? (value & Id3v220HeaderFlags.Unsynchronization)
                                            : (Version < Id3v2Version.Id3v240) ? (value & Id3v230HeaderFlags.Unsynchronization)
                                            : (value & Id3v240HeaderFlags.Unsynchronization)) != 0;

                UseCompression = ((Version < Id3v2Version.Id3v230) ? (value & Id3v220HeaderFlags.Compression) : 0) != 0;

                UseExtendedHeader = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230HeaderFlags.ExtendedHeader)
                                         : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240HeaderFlags.ExtendedHeader)
                                         : 0) != 0;

                TagIsExperimental = (((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240)) ? (value & Id3v230HeaderFlags.ExperimentalIndicator)
                                         : (Version >= Id3v2Version.Id3v240) ? (value & Id3v240HeaderFlags.ExperimentalIndicator)
                                         : 0) != 0;

                UseFooter = ((Version >= Id3v2Version.Id3v240) ? (value & Id3v240HeaderFlags.Footer) : 0) != 0;
            }
        }
    }
}
