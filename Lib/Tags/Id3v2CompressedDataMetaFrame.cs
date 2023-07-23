/*
 * Date: 2012-12-03
 * Sources used:
 *  http://id3lib.sourceforge.net/api/tag__parse_8cpp-source.html
 *  http://nedbatchelder.com/code/modules/id3reader.py
 *  https://gnunet.org/svn/Extractor/test/id3v2/README.txt
 *  http://id3lib.sourceforge.net/id3lib-manual.php
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing compressed data meta.
    /// <para />
    /// This frame is specific for <see cref="Id3v2Version.Id3v221"/>.
    /// </summary>
    public sealed class Id3v2CompressedDataMetaFrame : Id3v2Frame
    {
        private Id3v2CompressionMethod _compressionMethod;

        private byte[] _compressedData;

        private Id3v2Frame _frame;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2CompressedDataMetaFrame"/> class with version <see cref="Id3v2Version.Id3v221"/>.
        /// </summary>
        public Id3v2CompressedDataMetaFrame() : base(Id3v2Version.Id3v221)
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the compression method.
        /// </summary>
        /// <value>
        /// The compression method.
        /// </value>
        public Id3v2CompressionMethod CompressionMethod
        {
            get
            {
                return _compressionMethod;
            }

            set
            {
                if (!IsValidCompressionMethod(value))
                    throw new InvalidDataException("Compression method is not valid.");

                _compressionMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets the compressed frame.
        /// </summary>
        /// <value>
        /// The compressed frame.
        /// </value>
        /// <remarks>
        /// The compressed frame is the whole <see cref="Id3v2Frame"/> compressed; not just the frame's data.
        /// </remarks>
        public byte[] CompressedFrame
        {
            get
            {
                return _compressedData;
            }

            set
            {
                _compressedData = value;
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
                    // Compression method
                    stream.WriteByte((byte)CompressionMethod);

                    // Compressed data
                    byte[] compressedData = CompressedData;
                    if (compressedData != null)
                        stream.Write(compressedData);

                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _compressionMethod = (Id3v2CompressionMethod)stream.ReadByte();

                    int compressedSize = stream.ReadBigEndianInt32();
                    _compressedData = new byte[compressedSize];
                    stream.Read(_compressedData, compressedSize);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "CDM"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2CompressedDataMetaFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2CompressedDataMetaFrame"/>.
        /// </summary>
        /// <param name="compressedDataMeta">The <see cref="Id3v2CompressedDataMetaFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="CompressedData"/> properties are equal.
        /// </remarks>
        public bool Equals(Id3v2CompressedDataMetaFrame compressedDataMeta)
        {
            if (ReferenceEquals(null, compressedDataMeta))
                return false;

            if (ReferenceEquals(this, compressedDataMeta))
                return true;

            return (Version == compressedDataMeta.Version) && StreamBuffer.SequenceEqual(compressedDataMeta.CompressedData, CompressedData);
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
            return (version == Id3v2Version.Id3v221);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidCompressionMethod(Id3v2CompressionMethod compressionMethod)
        {
            return Enum.TryParse(compressionMethod.ToString(), true, out compressionMethod);
        }
    }
}
