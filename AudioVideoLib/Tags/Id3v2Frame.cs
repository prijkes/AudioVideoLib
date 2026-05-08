namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class used to store an <see cref="Id3v2Tag"/> frame.
/// </summary>
/// <remarks>
/// A frame is a block of information in an <see cref="Id3v2Tag"/>.
/// </remarks>
public partial class Id3v2Frame : IAudioTagFrame, IEquatable<Id3v2Frame>
{
    /// <summary>
    /// The max size an <see cref="Id3v2Frame"/> can be.
    /// </summary>
    /// <remarks>
    /// Frames can be up to 16MB in size.
    /// </remarks>
    //// frameSize <= MaxAllowedSize
    public const int MaxAllowedSize = (1024 * 1024 * 16) - 1; // or 0xFFFFFF

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2Frame"/> class.
    /// </summary>
    /// <param name="version">The <see cref="Id3v2Version"/> of the <see cref="Id3v2Tag"/>.</param>
    /// <param name="identifier">The identifier of the frame.</param>
    public Id3v2Frame(Id3v2Version version, string identifier)
    {
        if (!IsValidVersion(version))
        {
            throw new InvalidDataException($"Version {version} not valid.");
        }

        ArgumentNullException.ThrowIfNull(identifier);

        if (!IsValidIdentifier(version, identifier))
        {
            throw new InvalidDataException($"identifier {identifier} is not valid for version {version}.");
        }

        Identifier = identifier;
        Version = version;

        // Virtual call from constructor: dispatches to the derived IsVersionSupported.
        // Audited safe — Id3v2TextFrame and Id3v2UrlLinkFrame's overrides read _identifier
        // (null at this point because they call : base(version), not : base(version, id))
        // and fall through via null-fallthrough to the default base check, which only
        // consults the just-set Version. See Id3v2FrameBaseCtorTests for the pin.
#pragma warning disable CA2214 // Virtual call from constructor — audited safe; see comment above.
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
#pragma warning restore CA2214
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2Frame"/> class.
    /// </summary>
    /// <param name="version">The <see cref="Id3v2Version"/> of the <see cref="Id3v2Tag"/>.</param>
    /// <exception cref="InvalidDataException">Thrown if <paramref name="version"/> is not a valid <see cref="Id3v2Version"/>.</exception>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    protected Id3v2Frame(Id3v2Version version)
    {
        if (!IsValidVersion(version))
        {
            throw new InvalidDataException($"Version {version} not valid.");
        }

        Version = version;

        // See comment in (version, identifier) ctor above for the CA2214 audit.
#pragma warning disable CA2214
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
#pragma warning restore CA2214
    }

    private Id3v2Frame(string identifier)
    {
        Identifier = identifier;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public virtual byte[] Data { get; protected set; } = null!;

    /// <summary>
    /// Gets the encrypted data.
    /// </summary>
    /// <remarks>
    /// When an <see cref="Id3v2Frame"/> is read from a stream and the data is encrypted, the encrypted data will be stored in this field.
    /// Consumers can use the <see cref="EncryptedData"/> property to retrieve the data as-read.
    /// </remarks>
    public byte[] EncryptedData { get; private set; } = null!;

    /// <summary>
    /// Gets the compressed data.
    /// </summary>
    /// <remarks>
    /// When an <see cref="Id3v2Frame"/> is read from a stream and the data is compressed, the compressed data will be stored in this field.
    /// Consumers can use the <see cref="CompressedData"/> property to retrieve the data as-read.
    /// <para />
    /// If the data has been encrypted and compressed, but could not be decrypted, the data will be stored in the <see cref="EncryptedData"/> property instead.
    /// </remarks>
    public byte[] CompressedData { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the cryptor for handling <see cref="Id3v2Frame"/> encryption / decryption.
    /// </summary>
    /// <value>
    /// An <see cref="IId3v2FrameCryptor"/> which handles <see cref="Id3v2Frame"/> encryption / decryption.
    /// </value>
    public IId3v2FrameCryptor Cryptor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the compressor for handling <see cref="Id3v2Frame"/> compression / decompression.
    /// </summary>
    /// <value>
    /// An <see cref="IId3v2FrameCompressor"/> which handles <see cref="Id3v2Frame"/> compression / decompression.
    /// </value>
    public IId3v2FrameCompressor Compressor { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Id3v2Frame);
    }

    /// <inheritdoc/>
    public bool Equals(IAudioTagFrame? audioFrame)
    {
        return Equals(audioFrame as Id3v2Frame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2Frame"/>.
    /// </summary>
    /// <param name="frame">The <see cref="Id3v2Frame"/>.</param>
    /// <returns>
    /// Both instances are equal when the following fields are equal (case-insensitive):
    /// * <see cref="Version"/>
    /// * <see cref="Identifier"/>
    /// * <see cref="Flags"/>
    /// * <see cref="GroupIdentifier"/>
    /// * <see cref="EncryptionType"/>
    /// * <see cref="DataLengthIndicator"/>
    /// * <see cref="Data"/>
    /// </returns>
    public virtual bool Equals(Id3v2Frame? frame)
    {
        return frame is not null && (ReferenceEquals(this, frame) || ((frame.Version == Version) && string.Equals(frame.Identifier, Identifier, StringComparison.OrdinalIgnoreCase)
               && (frame.Flags == Flags) && (frame.GroupIdentifier == GroupIdentifier) && (frame.EncryptionType == EncryptionType)
               && (frame.DataLengthIndicator == DataLengthIndicator)
               && ((frame.Data != null) && (Data != null) ? StreamBuffer.SequenceEqual(frame.Data, Data) : (frame.Data == null) && (Data == null))));
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <remarks>
    /// Combines <see cref="Identifier"/> and <see cref="Version"/> — every
    /// derived <see cref="Equals(Id3v2Frame?)"/> requires equality on both,
    /// so this is the strongest base hash that respects the contract.
    /// Collisions on additional discriminator fields (Description, Language,
    /// OwnerIdentifier, etc.) are allowed.
    /// </remarks>
    public override int GetHashCode() => HashCode.Combine(Identifier, Version);

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Determines whether the specified version is supported by the frame.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Unless overridden, the <paramref name="version"/> is supported if any of the following is true:
    /// * Both <paramref name="version"/> and the current <see cref="Version"/> are equal to or higher than <see cref="Id3v2Version.Id3v230"/>.
    /// * Both <paramref name="version"/> and the current <see cref="Version"/> are lower than <see cref="Id3v2Version.Id3v230"/>.
    /// <para />
    /// This method is called from the base constructor: overrides must not access derived-class
    /// state that isn't initialised yet. Reading <see cref="Version"/> is safe (the base ctor sets
    /// it before the call); reading <see cref="Identifier"/> is also safe in `(version, identifier)`
    /// flows. Reading other backing fields is not safe — those are still null/default.
    /// </remarks>
    public virtual bool IsVersionSupported(Id3v2Version version)
    {
        return ((version >= Id3v2Version.Id3v230) && (Version >= Id3v2Version.Id3v230))
               || ((version < Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v230));
    }

    /// <summary>
    /// Sets the version of the <see cref="Id3v2Frame"/>.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    /// <remarks>
    /// This function can be used to change the <see cref="Version"/> of the <see cref="Id3v2Frame"/>.
    /// Classes inheriting this class can override <see cref="IsVersionSupported(Id3v2Version)"/> to specify whether the <paramref name="version"/> is supported or not.
    /// </remarks>
    public void SetVersion(Id3v2Version version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} is not supported by this frame");
        }

        Version = version;
    }

    /// <summary>
    /// Decrypts this instance.
    /// </summary>
    /// <returns>
    /// true if <see cref="Cryptor"/> has been set and this instance could be decrypted; otherwise, false.
    /// </returns>
    public bool Decrypt()
    {
        if (Cryptor == null)
        {
            return false;
        }

        var decryptedData = Cryptor.Decrypt(EncryptionType, EncryptedData, EncryptedData.Length);
        if (decryptedData == null)
        {
            return false;
        }

        _isEncrypted = false;
        Data = decryptedData;
        return true;
    }

    /// <summary>
    /// Decompresses this instance.
    /// </summary>
    /// <returns>
    /// true if <see cref="Compressor"/> has been set and this instance could be decompressed; otherwise, false.
    /// </returns>
    public bool Decompress()
    {
        if (Compressor == null)
        {
            return false;
        }

        var decompressedData = Compressor.Decompress(CompressedData, CompressedData.Length);
        if (decompressedData == null)
        {
            return false;
        }

        _isCompressed = false;
        Data = decompressedData;
        return true;
    }

    /// <summary>
    /// Writes the frame into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the frame.
    /// </returns>
    /// <remarks>
    /// When <see cref="UseEncryption"/> is set, <see cref="Cryptor"/> will be called to encrypt the data.
    /// Note that the data passed to the <see cref="Cryptor"/> function will be compressed if <see cref="UseCompression"/> is set.
    /// </remarks>
    public byte[] ToByteArray()
    {
        var data = (_isCompressed ? (_isEncrypted ? EncryptedData : CompressedData) : (_isEncrypted ? EncryptedData : Data)) ?? [];

        // Don't write 0-byte data fields as they won't be parsed again (according to the specs the data should contain at least 1 byte).
        if (data.Length == 0)
        {
            return [];
        }

        using var buffer = new StreamBuffer();

        // Write the extra header fields if needed.
        // Note that the order of fields differ by version.
        if (Version is >= Id3v2Version.Id3v230 and < Id3v2Version.Id3v240)
        {
            // Some flags indicates that the frame header is extended with additional information.
            // This information will be added to the frame header in the same order as the flags indicating the additions.
            // I.e. the four bytes of decompressed size will precede the encryption method byte.

            // Frame is compressed using [#ZLIB zlib] with 4 bytes for 'decompressed size' appended to the frame header.
            if (UseCompression)
            {
                buffer.WriteBigEndianInt32(data.Length);
            }

            // Frame is encrypted.
            if (UseEncryption)
            {
                buffer.WriteByte(EncryptionType);
            }

            // Frame contains group information
            if (UseGroupingIdentity)
            {
                buffer.WriteByte(GroupIdentifier);
            }
        }
        else if (Version >= Id3v2Version.Id3v240)
        {
            // Some frame format flags indicate that additional information fields are added to the frame.
            // This information is added after the frame header and before the frame data
            // in the same order as the flags that indicates them.
            // I.e. the four bytes of decompressed size will precede the encryption method byte.

            // Frame contains group information
            if (UseGroupingIdentity)
            {
                buffer.WriteByte(GroupIdentifier);
            }

            // Frame is encrypted.
            if (UseEncryption)
            {
                buffer.WriteByte(EncryptionType);
            }

            // A data length Indicator has been added to the frame.
            if (UseCompression || UseDataLengthIndicator)
            {
                buffer.WriteBigEndianInt32(Id3v2Tag.GetSynchsafeValue(data.Length));
            }
        }

        // If the frameData is not initially compressed and the compression flag has been set - compress the data.
        if (!_isEncrypted && !_isCompressed && UseCompression && (Compressor != null))
        {
            // Frame should be compressed using zlib [zlib] deflate method.
            data = Compressor.Compress(data);

            // A 'Data Length Indicator' byte MUST be included in the frame.
            UseDataLengthIndicator = true;
        }

        // If we can't encrypt the frame, i.e. because it's encrypted and we couldn't decrypt it, we can't compress it or encrypt it again.
        // If the encryption flag has been set - encrypt the data.
        if (!_isEncrypted && UseEncryption && (Cryptor != null))
        {
            data = Cryptor.Encrypt(EncryptionType, data) ?? data;
        }

        // Synchronize the data.
        if (UseUnsynchronization)
        {
            data = Id3v2Tag.GetUnsynchronizedData(data, 0, data.Length);
        }

        // Write the data to the temp buffer; header has already been written at this point.
        buffer.Write(data);

        data = buffer.ToByteArray();

        // Write the frame header to the final buffer.
        using var finalBuffer = new StreamBuffer();

        // identifier needs to match the IdentifierFieldLength; pad the identifier if needed (not legal, but some programs write incorrect identifiers).
        var identifier = (Identifier ?? string.Empty).PadRight(GetIdentifierFieldLength(Version), '\0');
        finalBuffer.WriteString(identifier[..IdentifierFieldLength]);
        if (Version >= Id3v2Version.Id3v240)
        {
            finalBuffer.WriteBigEndianInt32(Id3v2Tag.GetSynchsafeValue(data.Length));
        }
        else
        {
            finalBuffer.WriteBigEndianBytes(data.Length, DataSizeFieldLength);
        }

        // Write the flags to the final buffer.
        if (Version >= Id3v2Version.Id3v230)
        {
            finalBuffer.WriteBigEndianInt16((short)Flags);
        }

        finalBuffer.Write(data);
        return finalBuffer.ToByteArray();
    }
}
