namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing the seek indicator.
/// </summary>
/// <remarks>
/// This frame indicates where other tags in a file/stream can be found.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v240"/> only.
/// </remarks>
public sealed class Id3v2SeekFrame : Id3v2Frame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2SeekFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2SeekFrame() : base(Id3v2Version.Id3v240)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2SeekFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2SeekFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the minimum offset to the next tag.
    /// </summary>
    /// <value>
    /// The minimum offset to the next tag.
    /// </value>
    /// <remarks>
    /// The minimum offset to next tag is calculated from the end of this tag to the beginning of the next.
    /// </remarks>
    public int MinimumOffsetToNextTag { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            stream.WriteBigEndianInt32(MinimumOffsetToNextTag);

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            MinimumOffsetToNextTag = stream.ReadBigEndianInt32();
        }
    }

    /// <inheritdoc />
    public override string Identifier => "SEEK";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2SeekFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2SeekFrame"/>.
    /// </summary>
    /// <param name="seek">The <see cref="Id3v2SeekFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> property is equal.
    /// </remarks>
    public bool Equals(Id3v2SeekFrame? seek)
    {
        return seek is not null && (ReferenceEquals(this, seek) || seek.Version == Version);
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
        return version >= Id3v2Version.Id3v240;
    }
}
