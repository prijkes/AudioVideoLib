namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing a signature.
/// </summary>
/// <remarks>
/// This frame enables a group of frames, grouped with the <see cref="Id3v2GroupIdentificationRegistrationFrame"/>, to be signed.
/// Although signatures can reside inside the registration frame, 
/// it might be desired to store the signature elsewhere, e.g. in watermarks.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v240"/> only.
/// </remarks>
public sealed class Id3v2SignatureFrame : Id3v2Frame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2SignatureFrame"/> class with version <see cref="Id3v2Version.Id3v240"/>.
    /// </summary>
    public Id3v2SignatureFrame() : base(Id3v2Version.Id3v240)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2SignatureFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2SignatureFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException(string.Format("Version {0} not supported by this frame.", version));
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the group symbol.
    /// </summary>
    /// <value>
    /// The group symbol.
    /// </value>
    public byte GroupSymbol { get; set; }

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    /// <value>
    /// The signature.
    /// </value>
    public byte[] SignatureData { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            // Group symbol
            stream.WriteByte(GroupSymbol);

            // Signature data
            if (SignatureData != null)
            {
                stream.Write(SignatureData);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            GroupSymbol = (byte)stream.ReadByte();
            SignatureData = new byte[stream.Length - stream.Position];
            stream.Read(SignatureData, SignatureData.Length);
        }
    }

    /// <inheritdoc />
    public override string Identifier
    {
        get { return "SIGN"; }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2SignatureFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2SignatureFrame"/>.
    /// </summary>
    /// <param name="signature">The <see cref="Id3v2SignatureFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/>, <see cref="GroupSymbol"/> and <see cref="SignatureData"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2SignatureFrame? signature)
    {
        return signature is not null && (ReferenceEquals(this, signature) || ((signature.Version == Version) && (signature.GroupSymbol == GroupSymbol)
               && StreamBuffer.SequenceEqual(signature.SignatureData, SignatureData)));
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
