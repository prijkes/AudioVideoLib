namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing audio encryption.
/// </summary>
/// <remarks>
/// This frame indicates if the actual audio stream is encrypted, and by whom.
/// Since standardization of such encryption scheme is beyond this document, 
/// all <see cref="Id3v2AudioEncryptionFrame"/> frames contain an URL which can be an email address, 
/// or a link to a location where an email address can be found, 
/// that belongs to the organization responsible for this specific encrypted audio file.
/// Questions regarding the encrypted audio should be sent to the email address specified.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2AudioEncryptionFrame : Id3v2Frame
{
    private string _ownerIdentifier = null!;
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2AudioEncryptionFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2AudioEncryptionFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2AudioEncryptionFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2AudioEncryptionFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException(string.Format("Version {0} not supported by this frame.", version));
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the owner identifier.
    /// </summary>
    /// <value>
    /// The owner identifier.
    /// </value>
    /// <remarks>
    /// The owner identifier is a terminated string with a URL containing an email address, 
    /// or a link to a location where an email address can be found, 
    /// that belongs to the organization responsible for this specific encrypted audio file.
    /// Questions regarding the encrypted audio should be sent to the email address specified.
    /// <para />
    /// If the owner identifier is empty and the audio file indeed is encrypted, the whole file may be considered useless.
    /// <para />
    /// The URL must be a valid RFC 1738 URL, use <see cref="Id3v2Frame.IsValidUrl"/> to check if a value is a valid URL.
    /// </remarks>
    public string OwnerIdentifier
    {
        get
        {
            return _ownerIdentifier;
        }

        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!IsValidDefaultTextString(value, false))
                {
                    throw new InvalidDataException("value contains one or more invalid characters.");
                }

                if (!IsValidUrl(value))
                {
                    throw new InvalidDataException("value is not a valid RFC 1738 URL.");
                }
            }
            _ownerIdentifier = value;
        }
    }

    /// <summary>
    /// Gets or sets the preview start.
    /// </summary>
    /// <value>
    /// The preview start.
    /// </value>
    /// <remarks>
    /// The preview start is a pointer to an unencrypted part of the audio.
    /// The preview start and <see cref="PreviewLength"/> are described in frames.
    /// If no part is unencrypted, these fields should be left zeroed.
    /// </remarks>
    public short PreviewStart { get; set; }

    /// <summary>
    /// Gets or sets the length of the preview.
    /// </summary>
    /// <value>
    /// The length of the preview.
    /// </value>
    /// <remarks>
    /// The <see cref="PreviewStart"/> and preview length are described in frames.
    /// If no part is unencrypted, these fields should be left zeroed.
    /// </remarks>
    public short PreviewLength { get; set; }

    /// <summary>
    /// Gets or sets the encryption info.
    /// </summary>
    /// <value>
    /// The encryption info.
    /// </value>
    /// <remarks>
    /// The encryption info is an optional data block required for decryption of the audio.
    /// </remarks>
    public byte[] EncryptionInfo { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var stream = new StreamBuffer();
            // Owner Identifier
            if (OwnerIdentifier != null)
            {
                stream.WriteString(OwnerIdentifier, defaultEncoding);
            }

            // 0x00
            stream.WriteByte(0x00);

            // Preview start
            stream.WriteBigEndianInt16(PreviewStart);

            // Preview length
            stream.WriteBigEndianInt16(PreviewLength);

            // Encryption info
            if (EncryptionInfo != null)
            {
                stream.Write(EncryptionInfo);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            var stream = new StreamBuffer(value);
            _ownerIdentifier = stream.ReadString(defaultEncoding, true);
            PreviewStart = (short)stream.ReadBigEndianInt16();
            PreviewLength = (short)stream.ReadBigEndianInt16();
            EncryptionInfo = new byte[stream.Length - stream.Position];
            stream.Read(EncryptionInfo, EncryptionInfo.Length);
        }
    }

    /// <inheritdoc />
    public override string Identifier
    {
        get { return (Version < Id3v2Version.Id3v230) ? "CRA" : "AENC"; }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2AudioEncryptionFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2AudioEncryptionFrame"/>.
    /// </summary>
    /// <param name="audioEncryptionFrame">The <see cref="Id3v2AudioEncryptionFrame"/>.</param>
    /// <returns>
    /// true if equal; false otherwise.
    /// </returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> and <see cref="OwnerIdentifier"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2AudioEncryptionFrame? audioEncryptionFrame)
    {
        return audioEncryptionFrame is not null && (ReferenceEquals(this, audioEncryptionFrame) || ((audioEncryptionFrame.Version == Version)
               && string.Equals(audioEncryptionFrame.OwnerIdentifier, OwnerIdentifier, StringComparison.OrdinalIgnoreCase)));
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
