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
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing linked information.
    /// </summary>
    /// <remarks>
    /// To keep space waste as low as possible this frame may be used to link information 
    /// from another Id3v2 tag that might reside in another audio file or alone in a binary file.
    /// It is recommended that this method is only used when the files are stored on a CD-ROM 
    /// or other circumstances when the risk of file separation is low.
    /// The frame contains a frame identifier, which is the frame that should be linked into this tag, 
    /// a URL [URL] field, where a reference to the file where the frame is given, and additional ID data, if needed.
    /// Data should be retrieved from the first tag found in the file to which this link points.
    /// <para />
    /// A linked frame is to be considered as part of the <see cref="Id3v2Tag"/> and has the same restrictions 
    /// as if it was a physical part of the tag (i.e. only one <see cref="Id3v2ReverbFrame"/> frame allowed, whether it's linked or not).
    /// <para />
    /// Frames that may be linked and need no additional data are 
    /// <see cref="Id3v2InvolvedPeopleListFrame"/>, 
    /// <see cref="Id3v2MusicCdIdentifierFrame"/>, 
    /// <see cref="Id3v2EventTimingCodesFrame"/>, 
    /// <see cref="Id3v2SyncedTempoCodesFrame"/>, 
    /// <see cref="Id3v2RelativeVolumeAdjustmentFrame"/>, 
    /// <see cref="Id3v2EqualisationFrame"/>, 
    /// <see cref="Id3v2ReverbFrame"/>, 
    /// <see cref="Id3v2RecommendedBufferSizeFrame"/>, 
    /// the <see cref="Id3v2TextFrame"/>s and the <see cref="Id3v2UrlLinkFrame"/> frames.
    /// <para />
    /// The <see cref="Id3v2UserDefinedTextInformationFrame"/>, 
    /// <see cref="Id3v2AttachedPictureFrame"/>, 
    /// <see cref="Id3v2GeneralEncapsulatedObjectFrame"/>, 
    /// <see cref="Id3v2EncryptedMetaFrame"/> and <see cref="Id3v2AudioEncryptionFrame"/> frames 
    /// may be linked with the content descriptor as <see cref="AdditionalIdData"/>.
    /// <para />
    /// The <see cref="Id3v2CommentFrame"/>, 
    /// <see cref="Id3v2SynchronizedLyricsFrame"/> and <see cref="Id3v2UnsynchronizedLyricsFrame"/> frames 
    /// may be linked with three bytes of language descriptor directly followed by a content descriptor as <see cref="AdditionalIdData"/>.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2LinkedInformationFrame : Id3v2Frame
    {
        private string _frameIdentifier, _url, _additionalIdData;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2LinkedInformationFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="frameIdentifier">The frame identifier.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="frameIdentifier" /> is null.</exception>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2LinkedInformationFrame(Id3v2Version version, string frameIdentifier) : base(version)
        {
            if (frameIdentifier == null)
                throw new ArgumentNullException("frameIdentifier");

            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            FrameIdentifier = frameIdentifier;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the frame identifier.
        /// </summary>
        /// <value>
        /// The frame identifier.
        /// </value>
        public string FrameIdentifier
        {
            get
            {
                return _frameIdentifier;
            }

            set
            {
                // FrameIdentifier may not be null.
                if (value == null)
                    throw new ArgumentNullException("value");

                _frameIdentifier = (value.Length > ((Version < Id3v2Version.Id3v230) ? 3 : 4))
                                       ? value.Substring(0, (Version < Id3v2Version.Id3v230) ? 3 : 4)
                                       : value;
            }
        }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL link.
        /// </value>
        /// <remarks>
        /// The URL must be a valid RFC 1738 URL, use <see cref="Id3v2Frame.IsValidUrl"/> to check if a value is a valid URL.
        /// </remarks>
        public string Url
        {
            get
            {
                return _url;
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
                _url = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the additional id data.
        /// </summary>
        /// <value>
        /// The additional id data.
        /// </value>
        public string AdditionalIdData
        {
            get
            {
                return _additionalIdData;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                _additionalIdData = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Frame Identifier
                    stream.WriteString(FrameIdentifier);

                    // URL
                    if (Url != null)
                        stream.WriteString(Url, defaultEncoding);

                    // 0x00
                    stream.WriteByte(0x00);

                    // Additional ID data
                    if (AdditionalIdData != null)
                        stream.WriteString(AdditionalIdData);

                    // 0x00
                    stream.WriteByte(0x00);
                    
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
                    _frameIdentifier = stream.ReadString((Version < Id3v2Version.Id3v230) ? 3 : 4, defaultEncoding, true);
                    _url = stream.ReadString(defaultEncoding, true);
                    _additionalIdData = stream.ReadString(defaultEncoding);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "LNK" : "LINK"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2LinkedInformationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2LinkedInformationFrame"/>.
        /// </summary>
        /// <param name="li">The <see cref="Id3v2LinkedInformationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/>, <see cref="FrameIdentifier"/>, 
        /// <see cref="Url"/> and <see cref="AdditionalIdData"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2LinkedInformationFrame li)
        {
            if (ReferenceEquals(null, li))
                return false;

            if (ReferenceEquals(this, li))
                return true;

            return (li.Version == Version)
                   && String.Equals(li.FrameIdentifier, FrameIdentifier, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(li.Url, Url, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(li.AdditionalIdData, AdditionalIdData, StringComparison.OrdinalIgnoreCase);
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
