/*
 * Date: 2013-10-20
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="Id3v2Tag"/> frame.
    /// </summary>
    /// <remarks>
    /// A frame is a block of information in an <see cref="Id3v2Tag"/>.
    /// </remarks>
    public partial class Id3v2Frame
    {
        // Encryption should be done after compression.
        // we can't decrypt the data so it's still encrypted when decompressed.
        private bool _isEncrypted;

        // Compression should be done before encryption.
        // we can't decompress the data if it's still encrypted.
        private bool _isCompressed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads an <see cref="Id3v2Frame"/> from a <see cref="Stream"/> at the current position.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="maximumFrameSize">Maximum size of the frame.</param>
        /// <returns>
        /// An <see cref="Id3v2Frame"/> if found; otherwise, null.
        /// </returns>
        public static Id3v2Frame ReadFromStream(Id3v2Version version, Stream stream, long maximumFrameSize)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return ReadFrame(version, stream as StreamBuffer ?? new StreamBuffer(stream), maximumFrameSize);
        }

        /// <summary>
        /// Reads a type specific <see cref="Id3v2Frame" /> from a <see cref="Stream" /> at the current position.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Id3v2Frame" />.</typeparam>
        /// <param name="version">The version.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="maximumFrameSize">Maximum size of the frame.</param>
        /// <returns>
        /// The type specific <see cref="Id3v2Frame" /> if found; otherwise, null.
        /// </returns>
        public static T ReadFromStream<T>(Id3v2Version version, Stream stream, long maximumFrameSize) where T : Id3v2Frame, new()
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return ReadFrame<T>(version, stream as StreamBuffer ?? new StreamBuffer(stream), maximumFrameSize);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static Id3v2Frame ReadFrame(Id3v2Version version, StreamBuffer sb, long maximumFrameSize)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            string identifier = ReadIdentifier(version, sb, maximumFrameSize);
            if (identifier == null)
                return null;

            Id3v2Frame frame = GetFrame(version, identifier);
            return frame.ReadFrame(sb, maximumFrameSize) ? frame : null;
        }

        private static T ReadFrame<T>(Id3v2Version version, StreamBuffer sb, long maximumFrameSize) where T : Id3v2Frame, new()
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            string identifier = ReadIdentifier(version, sb, maximumFrameSize);
            if (identifier == null)
                return null;

            // Initialize and read the frame.
            T frame = new T { Version = version, Identifier = identifier };
            return frame.ReadFrame(sb, maximumFrameSize) ? frame : null;
        }

        private static string ReadIdentifier(Id3v2Version version, StreamBuffer sb, long maximumFrameSize)
        {
            // Find the frame.
            long startPosition = sb.Position;
            long bytesToSkip = GetBytesUntilNextFrame(version, sb, maximumFrameSize);

            // Not found; return null.
            if (bytesToSkip == -1)
                return null;

            // Move the stream position to the start of the frame.
            sb.Position = startPosition + bytesToSkip;

            // ID3v2 frame identifier field length.
            int identifierFieldLength = GetIdentifierFieldLength(version);
            byte[] identifierBytes = new byte[identifierFieldLength];
            sb.Read(identifierBytes, identifierFieldLength);

            // Filter the bad chars out
            return Encoding.ASCII.GetString(identifierBytes.Where(b => IsValidIdentifierByte(b)).ToArray());
        }

        private bool ReadFrame(StreamBuffer sb, long maximumFrameSize)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            // Store the startPosition so we can retrieve the amount of header bytes read later.
            // The identifier is passed as argument here; so subtract the amount of bytes read
            long startPosition = sb.Position - IdentifierFieldLength;

            // Read the frame size.
            // The frame ID is followed by a size descriptor, making a total header size of ten bytes in every frame.
            // The size is calculated as frame size excluding frame header (frame size - 10) for ID3v2.3.0 and later, or 6 bytes for earlier versions.
            int dataSizeFieldLength = GetDataSizeFieldLength(Version);
            long initialSize = sb.ReadBigEndianInt64(dataSizeFieldLength);
            int dataSize = GetFrameSize(Version, initialSize, maximumFrameSize - GetHeaderSize(Version));

            // Only version Id3v2.3.0 and later have flags.
            if (Version >= Id3v2Version.Id3v230)
                Flags = sb.ReadBigEndianInt16();

            // Store the amount of bytes read for the header here, for later use.
            long headerSize = sb.Position - startPosition;

            // Read the data.
            byte[] data = new byte[dataSize];
            dataSize = sb.Read(data, dataSize);

            // Synchronize the data again if it has been unsynchronized (Id3v2.4.0 and later only).
            if (UseUnsynchronization)
                data = Id3v2Tag.GetSynchronizedData(data, 0, dataSize);

            // streamBuffer still contains the unsynchronized data, place data in a BinaryReader and read data.
            BinaryReader binaryStream = new BinaryReader(new MemoryStream(data));

            // Some flags indicates that the frame header is extended with additional information.
            // This information will be added to the frame header in the same order as the flags indicating the additions.
            // I.e. the four bytes of decompressed size will precede the encryption method byte.
            // Note that the order of the fields differ by version.
            if ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240))
            {
                if (UseCompression)
                {
                    _isCompressed = true;
                    int decompressedSize = binaryStream.ReadInt32();
                    DecompressedFrameSize = StreamBuffer.SwitchEndianness(decompressedSize);
                }

                if (UseEncryption)
                {
                    _isEncrypted = true;
                    EncryptionType = binaryStream.ReadByte();
                }

                if (UseGroupingIdentity)
                    GroupIdentifier = binaryStream.ReadByte();
            }
            else if (Version >= Id3v2Version.Id3v240)
            {
                if (UseGroupingIdentity)
                    GroupIdentifier = binaryStream.ReadByte();

                if (UseCompression)
                {
                    _isCompressed = true;
                    int decompressedSize = binaryStream.ReadInt32();
                    DecompressedFrameSize = StreamBuffer.SwitchEndianness(decompressedSize);
                }

                if (UseEncryption)
                {
                    _isEncrypted = true;
                    EncryptionType = binaryStream.ReadByte();
                }

                if (UseDataLengthIndicator)
                {
                    int synchedDataLength = binaryStream.ReadInt32();
                    synchedDataLength = StreamBuffer.SwitchEndianness(synchedDataLength);
                    DataLengthIndicator = Id3v2Tag.GetUnsynchedValue(synchedDataLength);
                }
            }

            // Now calculate the frame data.
            // frameDataSize != dataSize; this can be different. If the frame is synchronized, encrypted -or- compressed, the size will be different.
            int frameDataSize = (int)Math.Max(binaryStream.BaseStream.Length - binaryStream.BaseStream.Position, 0);
            byte[] frameData = new byte[frameDataSize];
            int dataBytesRead = binaryStream.Read(frameData, 0, frameDataSize);
            int bytesLeftInStream = (int)(maximumFrameSize - headerSize - dataSize);
            if ((dataSize > 0) && (bytesLeftInStream > 0) && (dataBytesRead >= frameDataSize))
            {
                // Hold a temporary buffer for all data available.
                using (StreamBuffer dataBuffer = new StreamBuffer(frameData) { Position = frameData.Length })
                {
                    // If there's still data left in the stream which we haven't read yet, do it here.
                    // Try to find the start of the next frame.
                    long startPositionNextFrame = sb.Position;
                    long bytesUntilNextFrame = GetBytesUntilNextFrame(Version, sb, bytesLeftInStream);
                    sb.Position = startPositionNextFrame;

                    // Seems that the size indicated by the frame is not the total size of the frame; read the extra bytes here.
                    if (bytesUntilNextFrame > 0)
                    {
                        frameData = new byte[bytesUntilNextFrame];
                        sb.Read(frameData, 0, frameData.Length);
                        dataBuffer.Write(frameData);
                    }
                    frameData = dataBuffer.ToByteArray();
                }
            }

            // At this point, we should have all the frame data read.
            // Check for encryption
            if (UseEncryption)
            {
                if (Cryptor != null)
                {
                    byte[] decryptedData = Cryptor.Decrypt(EncryptionType, frameData, frameData.Length);
                    if (decryptedData != null)
                    {
                        frameData = decryptedData;
                        _isEncrypted = false;
                    }
                }
                else
                {
                    EncryptedData = frameData;
                }
            }

            // See if we need to decompress the frame data; but only if the frame isn't encrypted.
            if (UseCompression && !_isEncrypted)
            {
                if (Compressor != null)
                {
                    byte[] decompressedData = Compressor.Decompress(frameData, frameData.Length);
                    if (decompressedData != null)
                    {
                        frameData = decompressedData;
                        if (frameData.Length != DecompressedFrameSize)
                        {
#if DEBUG
                            throw new InvalidDataException(
                                String.Format(
                                    "decompressed data size {0} does not match decompressed frame size {1}.",
                                    frameData.Length,
                                    DecompressedFrameSize));
#else
                            return false;
#endif
                        }
                        _isCompressed = false;
                    }
                }
                else
                {
                    CompressedData = frameData;
                }
            }

            // Make sure to call the frame's data, even though frameData might be empty.
            // When doing this the frame will be initialized 'properly'; otherwise fields might remain NULL and cause crashes.
            if ((EncryptedData == null) && (CompressedData == null))
                Data = frameData;

            return true;
        }

        /// <summary>
        /// Gets the amount of bytes until the next frame.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="maximumNextFrameSize">Maximum possible size of the next frame.</param>
        /// <returns>
        /// The amount of bytes between the current position of the stream and the the start of the next frame; and -1 if the next frame has not been found.
        /// </returns>
        /// <remarks>
        /// The <paramref name="maximumNextFrameSize"/> is usually the size indicated by the header/footer of the <see cref="Id3v2Tag"/>, minus the already read bytes.
        /// </remarks>
        private static long GetBytesUntilNextFrame(Id3v2Version version, Stream stream, long maximumNextFrameSize)
        {
            // identifier length + size + flags
            int identifierFieldLength = GetIdentifierFieldLength(version);
            int dataSizeFieldLength = GetDataSizeFieldLength(version);
            int flagSizeFieldLength = (version >= Id3v2Version.Id3v230) ? 2 : 0;
            int extraBytesToRead = identifierFieldLength + dataSizeFieldLength + flagSizeFieldLength;

            // This needs to be long...
            long nextFrameSizeValue = 0, nextFrameFlags = 0;
            int nextFrameHeaderLengthRead = 0;

            long startPosition = stream.Position;
            long initialPosition = startPosition;
            long streamLength = stream.Length - stream.Position;
            
            // ID3v2.2.0: does not have a flags field.
            // ID3v2.3.0: %abc00000 %ijk00000
            // ID3v2.4.0: %0abc0000 %0h00kmnp
            int allowedHeaderFlags = (version < Id3v2Version.Id3v240) ? 0x57568 : 0x28751;

            // Total bytes read.
            long totalBytesRead = 0;

            // Used to store the first invalid identifier byte; which might be needed later.
            while (totalBytesRead++ < Math.Min(stream.Length, maximumNextFrameSize))
            {
                // The current byte to check
                byte b = (byte)stream.ReadByte();

                // Identifier field
                if (nextFrameHeaderLengthRead < identifierFieldLength)
                {
                    // Some badly written ID3v2 programs write Id3v2.2.0 NULL-terminated identifiers in versions ID3v2.3.0 and higher (ie. LEN + \x00).
                    if (!IsValidIdentifierByte(b))
                    {
                        // Some even worse written ID3v2 programs write 0x20 instead of a NULL-terminator to terminated the identifier in versions ID3v2.3.0 and higher.
                        if ((nextFrameHeaderLengthRead == 3) && ((b == 0x00) || (b == 0x20)))
                        {
                            // In the exceptional case when the first byte is, even though invalid, actually part of the frame identifier.
                            // We consider this 'valid' only when it's the very first byte of the data right after the previous frame's data,
                            // the 3 bytes after it are valid frame identifier bytes, and the 5th byte is 0x00.
                            // For example: 0xFF 0x4C 0x45 0x43 0x00 (\xFF + LEN + \x00)
                            // In this case, only the first 3 bytes of the size needs to be read, as the 0x00 byte read here is actually the first byte of the next frame's size value.
                            if (initialPosition == startPosition + 1)
                            {
                                // Size field of the next frame is only 3 bytes instead of 4 as this frame identifier is only 3 bytes + 0x00
                                // Fake the missing byte here
                                nextFrameHeaderLengthRead++;
                            }
                        }
                        else
                        {
                            initialPosition++;
                            nextFrameSizeValue = nextFrameFlags = nextFrameHeaderLengthRead = 0;
                            continue;
                        }
                    }
                    nextFrameHeaderLengthRead++;
                    continue;
                }
                
                // Size field
                if (nextFrameHeaderLengthRead < (identifierFieldLength + dataSizeFieldLength))
                {
                    // This byte is part of the size field of the next frame.
                    // The first byte is the most left byte (size field length is an int)
                    int bits = ((identifierFieldLength + dataSizeFieldLength) - nextFrameHeaderLengthRead) - 1;
                    nextFrameSizeValue |= b << (8 * bits);
                    nextFrameHeaderLengthRead++;
                    continue;
                }
                
                // Flags field
                if (nextFrameHeaderLengthRead < extraBytesToRead)
                {
                    // This byte is part of the flags field of the next frame.
                    int bits = (extraBytesToRead - nextFrameHeaderLengthRead) - 1;
                    nextFrameFlags |= b << (8 * bits);
                    nextFrameHeaderLengthRead++;
                    continue;
                }

                // Get the proper frame size; the size that looks most reliable.
                int nextFrameSize = GetFrameSize(version, nextFrameSizeValue, maximumNextFrameSize);

                // See if the frame size is valid.
                long startOffsetNextFrame = (stream.Position - startPosition) - (nextFrameHeaderLengthRead + 1);
                if ((nextFrameSize >= 0) && (nextFrameSize <= Id3v2Tag.MaxAllowedSize) && (nextFrameSize < streamLength)
                    // The next frame is right after the current frame (and we don't care about the flags); or it isn't, but the flags set are allowed.
                    && ((startOffsetNextFrame == 0) || ((nextFrameFlags & ~allowedHeaderFlags) == 0x00)))
                {
                    // Found the start of the next frame; calculate the amount of bytes between the last frame and the next frame.
                    return startOffsetNextFrame;
                }
                initialPosition++;
                nextFrameSizeValue = nextFrameFlags = nextFrameHeaderLengthRead = 0;
            }
            return -1;
        }
    }
}
