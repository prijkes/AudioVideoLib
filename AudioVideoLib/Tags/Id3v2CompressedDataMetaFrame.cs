namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing compressed data meta.
/// <para />
/// This frame is specific for <see cref="Id3v2Version.Id3v221"/>.
/// </summary>
public sealed class Id3v2CompressedDataMetaFrame : Id3v2Frame
{
    private Id3v2CompressionMethod _compressionMethod;

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
            {
                throw new InvalidDataException("Compression method is not valid.");
            }

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
    public byte[] CompressedFrame { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            // Compression method
            stream.WriteByte((byte)CompressionMethod);

            // Compressed data
            var compressedData = CompressedData;
            if (compressedData != null)
            {
                stream.Write(compressedData);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            _compressionMethod = (Id3v2CompressionMethod)stream.ReadByte();

            var compressedSize = stream.ReadBigEndianInt32();
            CompressedFrame = new byte[compressedSize];
            stream.Read(CompressedFrame, compressedSize);
        }
    }

    /// <inheritdoc />
    public override string Identifier => "CDM";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
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
    public bool Equals(Id3v2CompressedDataMetaFrame? compressedDataMeta)
    {
        return compressedDataMeta is not null && (ReferenceEquals(this, compressedDataMeta) || ((Version == compressedDataMeta.Version) && StreamBuffer.SequenceEqual(compressedDataMeta.CompressedData, CompressedData)));
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
        return version == Id3v2Version.Id3v221;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static bool IsValidCompressionMethod(Id3v2CompressionMethod compressionMethod)
    {
        return Enum.TryParse(compressionMethod.ToString(), true, out Id3v2CompressionMethod _);
    }
}
