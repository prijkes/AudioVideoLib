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

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing the Id3v2 recommended buffer size.
    /// </summary>
    /// <remarks>
    /// Embedded tags are generally not recommended since this could render unpredictable behavior from present software/hardware.
    /// <para />
    /// For applications like streaming audio it might be an idea to embed tags into the audio stream though.
    /// If the clients connects to individual connections like HTTP and there is a possibility to begin every transmission with a tag,
    /// then this tag should include an <see cref="Id3v2RecommendedBufferSizeFrame" /> frame.
    /// If the client is connected to a arbitrary point in the stream, such as radio or multicast,
    /// then the <see cref="Id3v2RecommendedBufferSizeFrame" /> frame SHOULD be included in every <see cref="Id3v2Tag" />.
    /// <para />
    /// This frame supports <see cref="Id3v2Version" /> up to and including <see cref="Id3v2Version.Id3v240" />.
    /// </remarks>
    public sealed class Id3v2RecommendedBufferSizeFrame : Id3v2Frame
    {
        private int _flags;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RecommendedBufferSizeFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2RecommendedBufferSizeFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RecommendedBufferSizeFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2RecommendedBufferSizeFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the maximum buffer size of the next <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// The maximum buffer size of the next <see cref="Id3v2Tag"/>.
        /// </value>
        /// <remarks>
        /// The maximum buffer size indicates the maximum size the next <see cref="Id3v2Tag"/> may be.
        /// </remarks>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include an <see cref="Id3v2Tag"/> in the audio stream.
        /// </summary>
        /// <value>
        /// <c>true</c> if the audio stream should include an <see cref="Id3v2Tag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// If the embedded info flag is true (1) then this indicates that an ID3 tag with the maximum size
        /// described in <see cref="BufferSize"/> may occur in the audio stream.
        /// In such case the tag should reside between two MPEG [MPEG] frames, if the audio is MPEG encoded.
        /// If the position of the next tag is known, <see cref="OffsetToNextTag"/> may be used.
        /// </remarks>
        public bool UseEmbeddedInfo
        {
            get
            {
                return (_flags & Id3v2RecommendedBufferSizeFlags.EmbeddedInfo) != 0;
            }

            set
            {
                if (value)
                    _flags |= Id3v2RecommendedBufferSizeFlags.EmbeddedInfo;
                else
                    _flags &= ~Id3v2RecommendedBufferSizeFlags.EmbeddedInfo;
            }
        }

        /// <summary>
        /// Gets or sets the offset to the next <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <value>
        /// The offset to the next <see cref="Id3v2Tag"/>.
        /// </value>
        /// <remarks>
        /// The offset is calculated from the end of the <see cref="Id3v2Tag"/> in which this frame resides 
        /// to the first byte of the header in the next <see cref="Id3v2Tag"/>.
        /// This field may be omitted.
        /// </remarks>
        public int OffsetToNextTag { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteBigEndianBytes(BufferSize, 3);
                    stream.WriteByte((byte)_flags);
                    stream.WriteBigEndianInt32(OffsetToNextTag);
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    BufferSize = stream.ReadBigEndianInt(3);
                    _flags = (byte)stream.ReadByte();

                    // This field may be omitted.
                    if (stream.Position < value.Length)
                        OffsetToNextTag = stream.ReadBigEndianInt32();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "BUF" : "RBUF"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2RecommendedBufferSizeFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2RecommendedBufferSizeFrame"/>.
        /// </summary>
        /// <param name="rbs">The <see cref="Id3v2RecommendedBufferSizeFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2RecommendedBufferSizeFrame rbs)
        {
            if (ReferenceEquals(null, rbs))
                return false;

            if (ReferenceEquals(this, rbs))
                return true;

            return rbs.Version == Version;
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
