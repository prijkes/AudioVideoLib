/*
 * Date: 2011-08-12
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
    /// Class for storing the position synchronization.
    /// </summary>
    /// <remarks>
    /// This frame delivers information to the listener of how far into the audio stream he picked up; 
    /// in effect, it states the time offset of the first frame in the stream.
    /// The position is where in the audio the listener starts to receive, i.e. the beginning of the next frame.
    /// If this frame is used in the beginning of a file the value is always 0.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public sealed class Id3v2PositionSynchronizationFrame : Id3v2Frame
    {
        private Id3v2TimeStampFormat _timeStampFormat;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PositionSynchronizationFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2PositionSynchronizationFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2PositionSynchronizationFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2PositionSynchronizationFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the time stamp format.
        /// </summary>
        /// <value>
        /// The time stamp format.
        /// </value>
        public Id3v2TimeStampFormat TimeStampFormat
        {
            get
            {
                return _timeStampFormat;
            }

            set
            {
                if (!IsValidTimeStampFormat(value))
                    throw new ArgumentOutOfRangeException("value");

                _timeStampFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public long Position { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteByte((byte)TimeStampFormat);
                    stream.WriteBigEndianBytes(
                        Position, (Position > Int64.MaxValue) ? StreamBuffer.Int64Size : StreamBuffer.Int32Size);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _timeStampFormat = (Id3v2TimeStampFormat)stream.ReadByte();
                    Position = stream.ReadBigEndianInt64();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "POSS"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2PositionSynchronizationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2PositionSynchronizationFrame"/>.
        /// </summary>
        /// <param name="ps">The <see cref="Id3v2PositionSynchronizationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2PositionSynchronizationFrame ps)
        {
            if (ReferenceEquals(null, ps))
                return false;

            if (ReferenceEquals(this, ps))
                return true;

            return ps.Version == Version;
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
