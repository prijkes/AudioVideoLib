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
using System.Collections.Generic;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing the MPEG location lookup table.
    /// </summary>
    /// <remarks>
    /// To increase performance and accuracy of jumps within a MPEG [MPEG] audio file, 
    /// frames with time codes in different locations in the file might be useful.
    /// This frame includes references that the software can use to calculate positions in the file.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2MpegLocationLookupTableFrame : Id3v2Frame
    {
        private readonly EventList<Id3v2MpegLookupTableItem> _references = new EventList<Id3v2MpegLookupTableItem>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2MpegLocationLookupTableFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2MpegLocationLookupTableFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2MpegLocationLookupTableFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2MpegLocationLookupTableFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the MPEG frames between reference.
        /// </summary>
        /// <value>
        /// The MPEG frames between reference.
        /// </value>
        /// <remarks>
        /// The MpegFramesBetweenReference is a descriptor of how much the 'frame counter' should increase for every reference.
        /// If this value is two then the first reference points out the second frame, 
        /// the 2nd reference the 4th frame, the 3rd reference the 6th frame etc.
        /// </remarks>
        public short MpegFramesBetweenReference { get; set; }

        /// <summary>
        /// Gets or sets the bytes between reference.
        /// </summary>
        /// <value>
        /// The bytes between reference.
        /// </value>
        /// <remarks>
        /// In a similar way as the <see cref="MpegFramesBetweenReference"/> field, 
        /// the bytes between reference and milliseconds between reference points out bytes and milliseconds respectively.
        /// </remarks>
        public int BytesBetweenReference { get; set; }

        /// <summary>
        /// Gets or sets the milliseconds between reference.
        /// </summary>
        /// <value>
        /// The milliseconds between reference.
        /// </value>
        /// <remarks>
        /// In a similar way as the <see cref="MpegFramesBetweenReference"/> field, 
        /// the bytes between reference and milliseconds between reference points out bytes and milliseconds respectively.
        /// </remarks>
        public int MillisecondsBetweenReference { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Id3v2MpegLookupTableItem"/> references.
        /// </summary>
        /// <value>
        /// The <see cref="Id3v2MpegLookupTableItem"/> references.
        /// </value>
        public IList<Id3v2MpegLookupTableItem> References
        {
            get
            {
                return _references;
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
                    // MPEG frames between reference
                    stream.WriteBigEndianInt16(MpegFramesBetweenReference);

                    // Bytes between reference
                    stream.WriteBigEndianBytes(BytesBetweenReference, 3);

                    // Milliseconds between reference
                    stream.WriteBigEndianBytes(MillisecondsBetweenReference, 3);

                    // References
                    foreach (Id3v2MpegLookupTableItem mpegLookupTableItem in References)
                    {
                        stream.WriteByte(mpegLookupTableItem.DeviationInBytes);
                        stream.WriteByte(mpegLookupTableItem.DeviationInMilliseconds);
                    }
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    MpegFramesBetweenReference = (short)stream.ReadBigEndianInt16();
                    BytesBetweenReference = stream.ReadBigEndianInt(3);
                    MillisecondsBetweenReference = stream.ReadBigEndianInt(3);

                    // Read the references
                    _references.Clear();
                    while (stream.Position < stream.Length)
                        _references.Add(new Id3v2MpegLookupTableItem((byte)stream.ReadByte(), (byte)stream.ReadByte()));
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "MLL" : "MLLT"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2MpegLocationLookupTableFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2MpegLocationLookupTableFrame"/>.
        /// </summary>
        /// <param name="mllt">The <see cref="Id3v2MpegLocationLookupTableFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2MpegLocationLookupTableFrame mllt)
        {
            if (ReferenceEquals(null, mllt))
                return false;

            if (ReferenceEquals(this, mllt))
                return true;

            return mllt.Version == Version;
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
