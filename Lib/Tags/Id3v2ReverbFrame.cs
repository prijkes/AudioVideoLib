/*
 * Date: 2011-07-06
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
    /// Class for storing the reverb.
    /// </summary>
    /// <remarks>
    /// This frame is used to adjust echoes of different kinds.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2ReverbFrame : Id3v2Frame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ReverbFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2ReverbFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ReverbFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2ReverbFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the reverb left, in milliseconds.
        /// </summary>
        /// <value>
        /// The reverb left, in milliseconds.
        /// </value>
        /// <remarks>
        /// Reverb left/right is the delay between every bounce in milliseconds.
        /// </remarks>
        public short ReverbLeftMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the reverb right, in milliseconds.
        /// </summary>
        /// <value>
        /// The reverb right, in milliseconds.
        /// </value>
        /// <remarks>
        /// Reverb left/right is the delay between every bounce in milliseconds.
        /// </remarks>
        public short ReverbRightMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the reverb bounces left.
        /// </summary>
        /// <value>
        /// The reverb bounces left.
        /// </value>
        /// <remarks>
        /// Reverb bounces left/right is the number of bounces that should be made.
        /// 0xFF equals an infinite number of bounces.
        /// </remarks>
        public byte ReverbBouncesLeft { get; set; }

        /// <summary>
        /// Gets or sets the reverb bounces right.
        /// </summary>
        /// <value>
        /// The reverb bounces right.
        /// </value>
        /// <remarks>
        /// Reverb bounces left/right is the number of bounces that should be made.
        /// 0xFF equals an infinite number of bounces.
        /// </remarks>
        public byte ReverbBouncesRight { get; set; }

        /// <summary>
        /// Gets or sets the reverb feedback left to left.
        /// </summary>
        /// <value>
        /// The reverb feedback left to left.
        /// </value>
        /// <remarks>
        /// Feedback is the amount of volume that should be returned to the next echo bounce. 0x00 is 0%, 0xFF is 100%.
        /// If this value were 0x7F, there would be 50% volume reduction on the first bounce, yet 50% on the second and so on.
        /// Left to left means the sound from the left bounce to be played in the left speaker,
        /// while left to right means sound from the left bounce to be played in the right speaker.
        /// </remarks>
        public byte ReverbFeedbackLeftToLeft { get; set; }

        /// <summary>
        /// Gets or sets the reverb feedback left to right.
        /// </summary>
        /// <value>
        /// The reverb feedback left to right.
        /// </value>
        /// <remarks>
        /// Feedback is the amount of volume that should be returned to the next echo bounce. 0x00 is 0%, 0xFF is 100%.
        /// If this value were 0x7F, there would be 50% volume reduction on the first bounce, yet 50% on the second and so on.
        /// Left to left means the sound from the left bounce to be played in the left speaker,
        /// while left to right means sound from the left bounce to be played in the right speaker.
        /// </remarks>
        public byte ReverbFeedbackLeftToRight { get; set; }

        /// <summary>
        /// Gets or sets the reverb feedback right to right.
        /// </summary>
        /// <value>
        /// The reverb feedback right to right.
        /// </value>
        /// <remarks>
        /// Feedback is the amount of volume that should be returned to the next echo bounce. 0x00 is 0%, 0xFF is 100%.
        /// If this value were 0x7F, there would be 50% volume reduction on the first bounce, yet 50% on the second and so on.
        /// Left to left means the sound from the left bounce to be played in the left speaker,
        /// while left to right means sound from the left bounce to be played in the right speaker.
        /// </remarks>
        public byte ReverbFeedbackRightToRight { get; set; }

        /// <summary>
        /// Gets or sets the reverb feedback right to left.
        /// </summary>
        /// <value>
        /// The reverb feedback right to left.
        /// </value>
        /// <remarks>
        /// Feedback is the amount of volume that should be returned to the next echo bounce. 0x00 is 0%, 0xFF is 100%.
        /// If this value were 0x7F, there would be 50% volume reduction on the first bounce, yet 50% on the second and so on.
        /// Left to left means the sound from the left bounce to be played in the left speaker,
        /// while left to right means sound from the left bounce to be played in the right speaker.
        /// </remarks>
        public byte ReverbFeedbackRightToLeft { get; set; }

        /// <summary>
        /// Gets or sets the premix left to right.
        /// </summary>
        /// <value>
        /// The premix left to right.
        /// </value>
        /// <remarks>
        /// 'Premix left to right' is the amount of left sound to be mixed in the right before any reverb is applied, 
        /// where 0x00 id 0% and 0xFF is 100%.
        /// 'Premix right to left' does the same thing, but right to left.
        /// Setting both premix to 0xFF would result in a mono output (if the reverb is applied symmetric).
        /// </remarks>
        public byte PremixLeftToRight { get; set; }

        /// <summary>
        /// Gets or sets the premix right to left.
        /// </summary>
        /// <value>
        /// The premix right to left.
        /// </value>
        /// <remarks>
        /// 'Premix left to right' is the amount of left sound to be mixed in the right before any reverb is applied, 
        /// where 0x00 id 0% and 0xFF is 100%.
        /// 'Premix right to left' does the same thing, but right to left.
        /// Setting both premix to 0xFF would result in a mono output (if the reverb is applied symmetric).
        /// </remarks>
        public byte PremixRightToLeft { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteBigEndianInt16(ReverbLeftMilliseconds);
                    stream.WriteBigEndianInt16(ReverbRightMilliseconds);
                    stream.WriteByte(ReverbBouncesLeft);
                    stream.WriteByte(ReverbBouncesRight);
                    stream.WriteByte(ReverbFeedbackLeftToLeft);
                    stream.WriteByte(ReverbFeedbackLeftToRight);
                    stream.WriteByte(ReverbFeedbackRightToRight);
                    stream.WriteByte(ReverbFeedbackRightToLeft);
                    stream.WriteByte(PremixLeftToRight);
                    stream.WriteByte(PremixRightToLeft);
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    ReverbLeftMilliseconds = (short)stream.ReadBigEndianInt16();
                    ReverbRightMilliseconds = (short)stream.ReadBigEndianInt16();
                    ReverbBouncesLeft = (byte)stream.ReadByte();
                    ReverbBouncesRight = (byte)stream.ReadByte();
                    ReverbFeedbackLeftToLeft = (byte)stream.ReadByte();
                    ReverbFeedbackLeftToRight = (byte)stream.ReadByte();
                    ReverbFeedbackRightToRight = (byte)stream.ReadByte();
                    ReverbFeedbackRightToLeft = (byte)stream.ReadByte();
                    PremixLeftToRight = (byte)stream.ReadByte();
                    PremixRightToLeft = (byte)stream.ReadByte();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "REV" : "REVB"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2ReverbFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2ReverbFrame"/>.
        /// </summary>
        /// <param name="rev">The <see cref="Id3v2ReverbFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2ReverbFrame rev)
        {
            if (ReferenceEquals(null, rev))
                return false;

            if (ReferenceEquals(this, rev))
                return true;

            return rev.Version == Version;
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
