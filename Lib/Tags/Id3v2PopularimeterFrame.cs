/*
 * Date: 2011-07-23
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
    /// Class for storing the popularimeter.
    /// </summary>
    /// <remarks>
    /// The purpose of this frame is to specify how good an audio file is.
    /// Many interesting applications could be found to this frame such as a playlist that features better audio files more often 
    /// than others or it could be used to profile a person's taste and find other 'good' files by comparing people's profiles.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2PopularimeterFrame : Id3v2Frame
    {
        private string _emailToUser;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PopularimeterFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2PopularimeterFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PopularimeterFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2PopularimeterFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the email to user.
        /// </summary>
        /// <value>
        /// The email to user.
        /// </value>
        public string EmailToUser
        {
            get
            {
                return _emailToUser;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                _emailToUser = value;
            }
        }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>
        /// The rating.
        /// </value>
        public byte Rating { get; set; }

        /// <summary>
        /// Gets or sets the play counter.
        /// </summary>
        /// <value>
        /// The play counter.
        /// </value>
        public long Counter { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding emailEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Email to user
                    if (EmailToUser != null)
                        stream.WriteString(EmailToUser, emailEncoding);

                    // 0x00
                    stream.WriteByte(0x00);

                    // Rating
                    stream.WriteByte(Rating);

                    // Counter
                    // If no personal counter is wanted it may be omitted.
                    if (Counter > 0)
                        stream.WriteBigEndianBytes(Counter, (Counter > Int32.MaxValue) ? StreamBuffer.Int64Size : StreamBuffer.Int32Size);

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
                    _emailToUser = stream.ReadString(defaultEncoding, true);
                    Rating = (byte)stream.ReadByte();

                    // If no personal counter is wanted it may be omitted.
                    // When the counter reaches all one's, 
                    // one byte is inserted in front of the counter 
                    // thus making the counter eight bits bigger 
                    // in the same away as the play counter ("PCNT").
                    if (stream.Position < value.Length)
                        Counter = stream.ReadBigEndianInt64();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "POP" : "POPM"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2PopularimeterFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2PopularimeterFrame"/>.
        /// </summary>
        /// <param name="pm">The <see cref="Id3v2PopularimeterFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="EmailToUser"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2PopularimeterFrame pm)
        {
            if (ReferenceEquals(null, pm))
                return false;

            if (ReferenceEquals(this, pm))
                return true;

            return (pm.Version == Version) && String.Equals(pm.EmailToUser, EmailToUser, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified version is supported by the frame.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public override bool IsVersionSupported(Id3v2Version version)
        {
            return true;
        }
    }
}
