/*
 * Date: 2011-08-14
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Collections.Generic;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing the audio seek point index.
    /// </summary>
    /// <remarks>
    /// Audio files with variable bitrates are intrinsically difficult to deal with in the case of seeking within the file.
    /// The <see cref="Id3v2AudioSeekPointIndexFrame"/> frame makes seeking easier by providing a list a seek points within the audio file.
    /// The seek points are a fractional offset within the audio data, 
    /// providing a starting point from which to find an appropriate point to start decoding.
    /// The presence of an <see cref="Id3v2AudioSeekPointIndexFrame"/> frame requires the existence of the <see cref="Id3v2Tag.Length"/> property, 
    /// indicating the duration of the file in milliseconds.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v240"/> only.
    /// </remarks>
    public sealed class Id3v2AudioSeekPointIndexFrame : Id3v2Frame
    {
        private readonly EventList<short> _fractionAtIndex = new EventList<short>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2AudioSeekPointIndexFrame"/> class with version <see cref="Id3v2Version.Id3v240"/>.
        /// </summary>
        public Id3v2AudioSeekPointIndexFrame() : base(Id3v2Version.Id3v240)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2AudioSeekPointIndexFrame" /> class.
        /// </summary>
        /// <param name="version">The <see cref="Id3v2Version" />.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2AudioSeekPointIndexFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the indexed data start.
        /// </summary>
        /// <value>
        /// The indexed data start.
        /// </value>
        /// <remarks>
        /// Indexed data start is a byte offset from the beginning of the file.
        /// </remarks>
        public int IndexedDataStart { get; set; }

        /// <summary>
        /// Gets or sets the length of the indexed data.
        /// </summary>
        /// <value>
        /// The length of the indexed data.
        /// </value>
        /// <remarks>
        /// Indexed data length is the byte length of the audio data being indexed.
        /// </remarks>
        public int IndexedDataLength { get; set; }

        /// <summary>
        /// Gets or sets the number of index points.
        /// </summary>
        /// <value>
        /// The number of index points.
        /// </value>
        /// <remarks>
        /// Number of index points is the number of index points, as the name implies.
        /// The recommended number is 100.
        /// </remarks>
        public short NumberOfIndexPoints { get; set; }

        /// <summary>
        /// Gets or sets the bits per index point.
        /// </summary>
        /// <value>
        /// The bits per index point.
        /// </value>
        /// <remarks>
        /// Bits per index point is 8 or 16, depending on the chosen precision.
        /// 8 bits works well for short files (less than 5 minutes of audio), while 16 bits is advantageous for long files.
        /// </remarks>
        public byte BitsPerIndexPoint { get; set; }

        /// <summary>
        /// Gets the fraction at an index.
        /// </summary>
        /// <value>
        /// The fraction at an index.
        /// </value>
        /// <remarks>
        /// Fraction at index is the numerator of the fraction representing a relative position in the data.
        /// The denominator is 2 to the power of b.
        /// </remarks>
        public IList<short> FractionAtIndex
        {
            get
            {
                return _fractionAtIndex;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteBigEndianInt32(IndexedDataStart);
                    stream.WriteBigEndianInt32(IndexedDataLength);
                    stream.WriteBigEndianInt16(NumberOfIndexPoints);
                    stream.WriteByte(BitsPerIndexPoint);

                    foreach (short i in FractionAtIndex)
                        stream.WriteBigEndianBytes(i, (BitsPerIndexPoint == 8) ? StreamBuffer.Int8Size : StreamBuffer.Int16Size);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    IndexedDataStart = stream.ReadBigEndianInt32();
                    IndexedDataLength = stream.ReadBigEndianInt32();
                    NumberOfIndexPoints = (short)stream.ReadBigEndianInt16();
                    BitsPerIndexPoint = (byte)stream.ReadByte();

                    _fractionAtIndex.Clear();
                    for (int i = 0; i < NumberOfIndexPoints; i++)
                        _fractionAtIndex.Add((BitsPerIndexPoint == 8) ? (byte)stream.ReadByte() : (short)stream.ReadBigEndianInt16());
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "ASPI"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2AudioSeekPointIndexFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2AudioSeekPointIndexFrame"/>.
        /// </summary>
        /// <param name="audioSeekPointIndexFrame">The <see cref="Id3v2AudioSeekPointIndexFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2AudioSeekPointIndexFrame audioSeekPointIndexFrame)
        {
            if (ReferenceEquals(null, audioSeekPointIndexFrame))
                return false;

            if (ReferenceEquals(this, audioSeekPointIndexFrame))
                return true;

            return (audioSeekPointIndexFrame.Version == Version);
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
            return (version >= Id3v2Version.Id3v240);
        }
    }
}
