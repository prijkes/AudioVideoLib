/*
 * Date: 2011-06-26
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing CD identification information.
    /// </summary>
    /// <remarks>
    /// This frame is intended for music that comes from a CD, so that the CD can be identified in databases such as the CDDB [CDDB].
    /// The frame consists of a binary dump of the Table Of Contents, TOC,  from the CD, 
    /// which is a header of 4 bytes and then 8 bytes/track on the CD making a maximum of 804 bytes.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2MusicCdIdentifierFrame : Id3v2Frame
    {
        /// <summary>
        /// The table of contents consists of a header of 4 bytes and then 8 bytes/track on a CD plus 8 bytes for the 'lead out' making a maximum of 804 bytes.
        /// </summary>
        private const int MaxTableOfContentsSize = 804;

        private byte[] _tableOfContents;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2MusicCdIdentifierFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2MusicCdIdentifierFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2MusicCdIdentifierFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2MusicCdIdentifierFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the binary dump of the Table Of Contents.
        /// </summary>
        /// <value>
        /// The table of contents.
        /// </value>
        /// <remarks>
        /// The table of contents contains a header of 4 bytes and then 8 bytes/track on the CD making a maximum of 804 bytes.
        /// <para />
        /// If more than <see cref="MaxTableOfContentsSize"/> bytes are passed into the byte array, 
        /// only the first <see cref="MaxTableOfContentsSize"/> bytes will be used.
        /// </remarks>
        public byte[] TableOfContents
        {
            get
            {
                return _tableOfContents;
            }

            set
            {
                _tableOfContents = (value != null) ? value.Take(MaxTableOfContentsSize).ToArray() : null;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                return TableOfContents;
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                TableOfContents = value;
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "MCI" : "MCDI"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2MusicCdIdentifierFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2MusicCdIdentifierFrame"/>.
        /// </summary>
        /// <param name="mci">The <see cref="Id3v2MusicCdIdentifierFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2MusicCdIdentifierFrame mci)
        {
            if (ReferenceEquals(null, mci))
                return false;

            if (ReferenceEquals(this, mci))
                return true;

            return mci.Version == Version;
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
