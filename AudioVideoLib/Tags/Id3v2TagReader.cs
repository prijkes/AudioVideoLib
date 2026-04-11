/*
 * Date: 2013-10-16
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
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    public sealed partial class Id3v2TagReader : IAudioTagReader
    {
        private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(Id3v2Tag.HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = Encoding.ASCII.GetBytes(Id3v2Tag.FooterIdentifier);

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public IAudioTagOffset ReadFromStream(Stream stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
                throw new InvalidOperationException("stream can not be read");

            if (!stream.CanSeek)
                throw new InvalidOperationException("stream can not be seeked");

            StreamBuffer sb = stream as StreamBuffer ?? new StreamBuffer(stream);

            // Try to read the header.
            Id3v2Header headerOrFooter = ReadHeader(sb, tagOrigin);
            if (headerOrFooter == null)
                return null;

            // Don't throw an exception; just return here. The read header could be a false match.
            if (headerOrFooter.Size > Id3v2Tag.MaxAllowedSize)
                return null;

            // Copy header values.
            Id3v2Tag tag = new Id3v2Tag(headerOrFooter.Version, headerOrFooter.Flags);
            bool isHeader = String.Equals(Id3v2Tag.HeaderIdentifier, headerOrFooter.Identifier, StringComparison.OrdinalIgnoreCase);

            // startOffset: offset the header starts
            // endOffset: offset the footer ends if available, or when the tag ends
            long startOffset, endOffset;
            Id3v2Header header, footer = null;
            if (isHeader)
            {
                header = headerOrFooter;
                startOffset = header.Position;
                endOffset = Math.Min(startOffset + Id3v2Tag.HeaderSize + header.Size + (tag.UseFooter ? Id3v2Tag.FooterSize : 0), sb.Length);
                if (endOffset > sb.Length)
                {
#if DEBUG
                    throw new EndOfStreamException("Tag at start could not be read: stream is truncated.");
#else
                    return null;
#endif
                }
            }
            else
            {
                // We've read the footer.
                footer = headerOrFooter;
                endOffset = footer.Position + Id3v2Tag.FooterSize;
                startOffset = Math.Max(endOffset - Id3v2Tag.FooterSize - footer.Size - Id3v2Tag.HeaderSize, 0);

                // Seek to the start of the tag.
                sb.Seek(startOffset, SeekOrigin.Begin);

                // Read the header; it's location could be off.
                header = ReadHeader(sb, TagOrigin.Start);
                if (header == null)
                {
                    // Seek back a bit more and try again.
                    startOffset = Math.Max(startOffset - Id3v2Tag.HeaderSize, 0);
                    sb.Seek(startOffset, SeekOrigin.Begin);
                    header = ReadHeader(sb, TagOrigin.End);

                    // If we still didn't find a header, seek to the start offset; the header could be missing.
                    if (header == null)
                        sb.Seek(startOffset, SeekOrigin.Begin);
                }

                // If we've found a header.
                if (header != null)
                {
                    startOffset = header.Position;

                    // Calculate the tag size (i.e. all data, excluding the header and footer)
                    headerOrFooter.Size = (int)Math.Max(endOffset - (startOffset + Id3v2Tag.HeaderSize + Id3v2Tag.FooterSize), 0);
                }
                else
                {
                    // No header found.
                    tag.UseHeader = false;
                }

                // Return if this is the case.
                if (headerOrFooter.Size > Id3v2Tag.MaxAllowedSize)
                {
#if DEBUG
                    throw new InvalidDataException(String.Format("Size ({0}) is larger than the max allowed size ({1})", footer.Size, Id3v2Tag.MaxAllowedSize));
#else
                    return null;
#endif
                }

                // A footer is read at this point.
                tag.UseFooter = true;
            }

            // At this point, the stream position is located right at the start of the data after the header.
            // The size is the sum of the byte length of the extended header, the padding and the frames after unsynchronization.
            // This does not include the footer size nor the header size.
            int totalSizeItems = Math.Min(headerOrFooter.Size, (int)(sb.Length - sb.Position));

            // If the tag has been unsynchronized, synchronize it again.
            // For version Id3v2.4.0 and later we don't do the unsynch here:
            // unsynchronization [S:6.1] is done on frame level, instead of on tag level, making it easier to skip frames, increasing the stream ability of the tag.
            // The unsynchronization flag in the header [S:3.1] indicates if all frames has been unsynchronized, 
            // while the new unsynchronization flag in the frame header [S:4.1.2] indicates unsynchronization.
            if (tag.UseUnsynchronization && (tag.Version < Id3v2Version.Id3v240))
            {
                int unsynchronizedDataSize = totalSizeItems;
                byte[] unsynchronizedData = new byte[unsynchronizedDataSize];
                unsynchronizedDataSize = sb.Read(unsynchronizedData, 0, unsynchronizedDataSize);
                byte[] data = Id3v2Tag.GetSynchronizedData(unsynchronizedData, 0, unsynchronizedDataSize);
                sb = new StreamBuffer(data);

                // Update the total size of the items with the length of the synchronized data
                totalSizeItems = data.Length;
            }

            // Calculate the padding size.
            int paddingSize = 0;

            // Check for an extended header.
            if (tag.UseExtendedHeader)
            {
                int crc;
                tag.ExtendedHeader = ReadExtendedHeader(sb, tag, out crc);
                if (tag.ExtendedHeader != null)
                {
                    if (tag.UseFooter && (tag.ExtendedHeader.PaddingSize != 0))
                    {
                        // Id3v2 tag can not have padding when an Id3v2 footer has been added.
                        tag.ExtendedHeader.PaddingSize = 0;
                    }
                    paddingSize += tag.ExtendedHeader.PaddingSize;

                    // Calculate CRC if needed.
                    if (tag.ExtendedHeader.CrcDataPresent)
                    {
                        long currentPosition = sb.Position;

                        // Id3v2.3.0:
                        // The CRC should be calculated before unsynchronisation on the data between the extended header and the padding, i.e. the frames and only the frames.
                        // Id3v2.4.0:
                        // The CRC is calculated on all the data between the header and footer as indicated by the header's tag length field, minus the extended header.
                        // Note that this includes the padding (if there is any), but excludes the footer.
                        int dataLength = ((tag.Version >= Id3v2Version.Id3v240)
                                              ? totalSizeItems
                                              : totalSizeItems - (tag.PaddingSize + tag.ExtendedHeader.PaddingSize) - 4)
                                         - tag.ExtendedHeader.GetHeaderSize(tag.Version);
                        byte[] crcData = sb.ToByteArray().Skip((int)currentPosition).Take(dataLength).ToArray();
                        int calculatedCrc = Cryptography.Crc32.Calculate(crcData);
                        if (calculatedCrc != crc)
                            throw new InvalidDataException(String.Format("CRC {0:X} in tag does not match calculated CRC {1:X}", crc, calculatedCrc));

                        sb.Position = currentPosition;
                    }
                }
            }

            // Bytes we won't read or have already read, excluding the header/footer.
            totalSizeItems -= (tag.ExtendedHeader != null ? tag.ExtendedHeader.GetHeaderSize(tag.Version) + paddingSize : 0);

            // Now let's get to the good part, and start parsing the frames!
            List<Id3v2Frame> frames = new List<Id3v2Frame>();
            long bytesRead = 0;
            while (bytesRead < totalSizeItems)
            {
                long startPosition = sb.Position;

                // See if the next byte is a padding byte.
                int identifierByte = sb.ReadByte(false);
                if (identifierByte == 0x00)
                {
                    // Rest is padding.
                    // We add up to the padding rather than assigning it, because the ExtendedHeader might include padding as well.
                    tag.PaddingSize += (int)(totalSizeItems - bytesRead);
                    bytesRead += tag.PaddingSize;
                    break;
                }

                Id3v2Frame frame = Id3v2Frame.ReadFromStream(tag.Version, sb, totalSizeItems - bytesRead);
                if (frame != null)
                {
                    Id3v2FrameParseEventArgs frameParseEventArgs = new Id3v2FrameParseEventArgs(frame);
                    OnFrameParse(frameParseEventArgs);

                    if (!frameParseEventArgs.Cancel)
                    {
                        // If the event has 'replaced' the frame, assign the 'new' frame here and continue.
                        if (frameParseEventArgs.Frame != null)
                            frame = frameParseEventArgs.Frame;

                        // Assign the cryptor instance of the tag to the frame, and decrypt if necessary.
                        bool isEncrypted = frame.UseEncryption;
                        if (isEncrypted)
                            isEncrypted = frame.Decrypt();

                        // Do the same with decompressing, if necessary.
                        if (!isEncrypted && frame.UseCompression)
                            frame.Decompress();

                        // Call after read event.
                        Id3v2FrameParsedEventArgs frameParsedEventArgs = new Id3v2FrameParsedEventArgs(frame);
                        OnFrameParsed(frameParsedEventArgs);

                        // Add the 'final' frame from the event.
                        frames.Add(frameParsedEventArgs.Frame);
                    }
                }
                else
                {
                    // Skip next byte.
                    sb.Position = (startPosition + 1);
                }

                //Reading is always done forward; not backwards.
                bytesRead += (sb.Position - startPosition);
            }
            paddingSize += tag.PaddingSize;

#if DEBUG
            if (frames.Count() > Id3v2Tag.MaxAllowedFrames)
            {
                throw new InvalidDataException(
                    String.Format("Tag has more frames ('{0}') than the allowed max frames count ('{1}').", frames.Count(), Id3v2Tag.MaxAllowedFrames));
            }

            if (bytesRead != totalSizeItems)
            {
                throw new InvalidDataException(
                    String.Format("Amount of bytes read ({0}) does not match expected size ({1}).", bytesRead, totalSizeItems));
            }

            // Id3v2.4.0 and later do not have a field for padding size.
            if (!isHeader && (paddingSize > 0))
                throw new InvalidDataException("Id3v2 tag can not have padding when footer is set.");
#endif

            // If the tag is at the start of the stream, is the tag allowed to have a footer?
            if (isHeader && tag.UseFooter)
            {
                sb.Position += Id3v2Tag.FooterSize;
                footer = ReadHeader(sb, TagOrigin.End);
            }

            // Validate the padding.
            if (paddingSize > 0)
            {
                byte[] padding = new byte[paddingSize];
                sb.Read(padding, paddingSize);
#if DEBUG
                if (!padding.Any(b => b == 0x00))
                    throw new InvalidDataException("Padding contains one or more invalid padding bytes.");
#endif
            }
            ValidateHeader(tag, header, footer);
            
            // Sort frames and add them to the tag.
            AddRequiredFrames(tag.Version, frames);
            tag.SetFrames(frames);

            return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static void ValidateHeader(Id3v2Tag tag, Id3v2Header header, Id3v2Header footer)
        {
#if DEBUG
            if (tag == null)
                return;

            if (header != null && footer != null)
            {
                if (header.Version != footer.Version)
                {
                    throw new InvalidDataException(
                        String.Format("The APE header version {0} does not match footer version {1}.", header.Version, footer.Version));
                }

                if (tag.PaddingSize != 0)
                    throw new InvalidDataException("Id3v2 tag can not have padding when footer is set.");
            }

            if (header != null)
            {
            }

            if (footer != null)
            {
                // If the tag is after the MPEG frames and there's no footer.
                if (!tag.UseFooter)
                    throw new InvalidDataException("Footer not found; footer is required for Id3v2 tags at the end of a stream.");
            }
#else
            return;
#endif
        }

        private static Id3v2Header ReadHeader(StreamBuffer stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            if (streamLength < Id3v2Tag.HeaderSize)
                return null;

            if (tagOrigin == TagOrigin.Start)
            {
                // Look for a header at the current position
                long startPositionHeader = startPosition;
                long endPositionHeader = Math.Min(startPositionHeader + Id3v2Tag.HeaderSize, streamLength);
                Id3v2Header header = ReadHeader(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
                if (header != null)
                    return header;

                // Look for a header past the current position
                startPositionHeader = endPositionHeader;
                endPositionHeader = Math.Min(startPositionHeader + Id3v2Tag.HeaderSize, streamLength);
                header = ReadHeader(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
                if (header != null)
                    return header;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                // Look for a footer before the current position
                long startPositionHeader = Math.Max(startPosition - Id3v2Tag.FooterSize, 0);
                long endPositionHeader = Math.Min(startPositionHeader + Id3v2Tag.FooterSize, streamLength);
                Id3v2Header footer = ReadHeader(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
                if (footer != null)
                    return footer;

                // Look for a footer before the previous start position
                startPositionHeader = Math.Max(startPositionHeader - Id3v2Tag.FooterSize, 0);
                endPositionHeader = Math.Min(startPositionHeader + Id3v2Tag.FooterSize, streamLength);
                footer = ReadHeader(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
                if (footer != null)
                    return footer;
            }
            return null;
        }

        private static Id3v2Header ReadHeader(StreamBuffer stream, long startHeaderPosition, long endHeaderPosition, byte[] identifierBytes)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            stream.Position = startHeaderPosition;

            // Look for a header/footer between the start position and end position.
            while (startHeaderPosition < endHeaderPosition)
            {
                int y = 0;

                // identifierBytes is the HeaderIdentifier or the FooterIdentifier encoded.
                while (stream.ReadByte() == identifierBytes[y++])
                {
                    startHeaderPosition++;
                    if (y != identifierBytes.Length)
                        continue;

                    // The first byte of ID3 version is it's major version, while the second byte is its revision number.
                    // All revisions are backwards compatible while major versions are not.
                    // If software with ID3v2 and below support should encounter version three or higher it should simply ignore the whole tag.
                    // Version and revision will never be $FF.
                    int majorRevision = stream.ReadByte();
                    int revisionNumber = stream.ReadByte();
                    if ((majorRevision >= 0x10) || (revisionNumber >= 0x10))
                    {
                        stream.Position -= 2;
                        break;
                    }

                    // Return header/footer found.
                    return new Id3v2Header
                               {
                                   Position = stream.Position - identifierBytes.Length - 2,
                                   Identifier = Encoding.ASCII.GetString(identifierBytes),
                                   Version = (Id3v2Version)((majorRevision * 10) + revisionNumber),

                                   // One byte of flags.
                                   // ID3v2.2.0: %xx000000
                                   Flags = (byte)stream.ReadByte(),

                                   // The ID3 tag size is encoded with four bytes where the first bit (bit 7) is set to zero in every byte, making a total of 28 bits.
                                   // The zeroed bits are ignored, so a 257 bytes long tag is represented as $00 00 02 01.
                                   // ID3v2.2.0: The ID3 tag size is the size of the complete tag after unsychronisation, including padding, excluding the header (total tag size - 10).
                                   Size = Id3v2Tag.GetUnsynchedValue(stream.ReadBigEndianInt32())
                               };
                }
                startHeaderPosition++;
            }
            return null;    
        }

        /// <summary>
        /// Reads the extended header.
        /// </summary>
        /// <param name="streamBuffer">The stream buffer.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="crcData">The CRC data.</param>
        /// <returns>
        /// The extended header if used; otherwise, null.
        /// </returns>
        /// <exception cref="System.IO.InvalidDataException">Thrown if the extended header size does not match the amount of bytes read for the extended header.</exception>
        private static Id3v2ExtendedHeader ReadExtendedHeader(StreamBuffer streamBuffer, Id3v2Tag tag, out int crcData)
        {
            crcData = 0;
            if (!tag.UseExtendedHeader)
                return null;

            // The extended header contains information that can provide further insight in the structure of the tag, 
            // but is not vital to the correct parsing of the tag information; hence the extended header is optional.
            int extendedHeaderSize = 0;
            long startPosition = streamBuffer.Position;
            Id3v2ExtendedHeader extendedHeader = null;
            if ((tag.Version >= Id3v2Version.Id3v230) && (tag.Version < Id3v2Version.Id3v240))
            {
                // Where the 'Extended header size', currently 6 or 10 bytes, excludes itself.
                extendedHeaderSize = streamBuffer.ReadBigEndianInt32() + 4;
                int extendedFlags = streamBuffer.ReadBigEndianInt16();
                extendedHeader = Id3v2ExtendedHeader.InitExtendedHeader(tag.Version, extendedFlags);
                extendedHeader.PaddingSize = streamBuffer.ReadBigEndianInt32();
                if (extendedHeader.CrcDataPresent)
                    crcData = streamBuffer.ReadBigEndianInt32();
            }
            else if (tag.Version >= Id3v2Version.Id3v240)
            {
                // Where the 'Extended header size' is the size of the whole extended header, stored as a 32 bit synchsafe integer.
                // An extended header can thus never have a size of fewer than six bytes.
                extendedHeaderSize = streamBuffer.ReadBigEndianInt32();
                extendedHeaderSize = Id3v2Tag.GetUnsynchedValue(extendedHeaderSize);
                int extendedFlagsFieldLength = streamBuffer.ReadByte();

                // The extended flags field, with its size described by 'number of flag bytes', is defined as: %0bcd0000
                int extendedFlags = streamBuffer.ReadInt(extendedFlagsFieldLength);
                extendedHeader = Id3v2ExtendedHeader.InitExtendedHeader(tag.Version, extendedFlags);
                extendedHeader.SetExtendedFlagsFieldLength(extendedFlagsFieldLength);

                // Each flag that is set in the extended header has data attached, 
                // which comes in the order in which the flags are encountered (i.e. the data for flag 'b' comes before the data for flag 'c').
                // Unset flags cannot have any attached data. All unknown flags MUST be unset and their corresponding data removed when a tag is modified.
                //
                // Every set flag's data starts with a length byte, which contains a value between 0 and 127 ($00 - $7f), 
                // followed by data that has the field length indicated by the length byte.
                // If a flag has no attached data, the value $00 is used as length byte.

                // If this flag is set, the present tag is an update of a tag found earlier in the present file or stream.
                // If frames defined as unique are found in the present tag, they are to override any corresponding ones found in the earlier tag.
                // This flag has no corresponding data.
                if (extendedHeader.TagIsUpdate)
                {
                    // If a flag has no attached data, the value $00 is used as length byte.
                    streamBuffer.ReadByte();
                }

                // If this flag is set, a CRC-32 [ISO-3309] data is included in the extended header.
                // The CRC is calculated on all the data between the header and footer as indicated by the header's tag length field, minus the extended header.
                // Note that this includes the padding (if there is any), but excludes the footer.
                // The CRC-32 is stored as an 35 bit synchsafe integer, leaving the upper four bits always zeroed.
                if (extendedHeader.CrcDataPresent)
                {
                    long crcBytes = streamBuffer.ReadBigEndianInt64(5);
                    crcData = (int)Id3v2Tag.GetUnsynchedValue(crcBytes, 5);
                }

                // For some applications it might be desired to restrict a tag in more ways than imposed by the ID3v2 specification.
                // Note that the presence of these restrictions does not affect how the tag is decoded, merely how it was restricted before encoding.
                // If this flag is set the tag is restricted as follows:
                if (extendedHeader.TagIsRestricted)
                {
                    // Restrictions: %ppqrrstt
                    byte tagRestrictions = (byte)streamBuffer.ReadByte();
                    extendedHeader.TagRestrictions = new Id3v2TagRestrictions
                    {
                        // p - Tag size restrictions
                        TagSizeRestriction = (Id3v2TagSizeRestriction)(tagRestrictions & Id3v2TagRestrictions.TagSizeRestrictionFlags),

                        // q - Text encoding restrictions
                        TextEncodingRestriction = (Id3v2TextEncodingRestriction)(tagRestrictions & Id3v2TagRestrictions.TextEncodingRestrictionFlags),

                        // r - Text fields size restrictions
                        TextFieldsSizeRestriction = (Id3v2TextFieldsSizeRestriction)(tagRestrictions & Id3v2TagRestrictions.TextFieldsSizeRestrictionFlags),

                        // s - Image encoding restrictions
                        ImageEncodingRestriction = (Id3v2ImageEncodingRestriction)(tagRestrictions & Id3v2TagRestrictions.ImageEncodingRestrictionFlags),

                        // t - Image size restrictions
                        ImageSizeRestriction = (Id3v2ImageSizeRestriction)(tagRestrictions & Id3v2TagRestrictions.ImageSizeRestrictionFlags)
                    };
                }
            }

            if ((streamBuffer.Position - startPosition) != extendedHeaderSize)
            {
                throw new InvalidDataException(
                    string.Format(
                        "ExtendedHeaderSize does not match amount of bytes read: expected {0} but got {1} bytes.",
                        extendedHeaderSize,
                        streamBuffer.Position - startPosition));
            }
            return extendedHeader;
        }

        private static void AddRequiredFrames(Id3v2Version version, ICollection<Id3v2Frame> frames)
        {
            if (frames == null)
                return;

            string trackNumberIdentifier = Id3v2TextFrame.GetIdentifier(version, Id3v2TextFrameIdentifier.TrackNumber);
            Id3v2Frame trackNumberFrame =
                frames.OfType<Id3v2TextFrame>()
                    .FirstOrDefault(f => String.Equals(f.Identifier, trackNumberIdentifier, StringComparison.OrdinalIgnoreCase));

            string musicCdIdentifier = Id3v2Frame.GetIdentifier<Id3v2MusicCdIdentifierFrame>(version);
            Id3v2Frame musicCdFrame =
                frames.FirstOrDefault(
                    f => f is Id3v2MusicCdIdentifierFrame || String.Equals(f.Identifier, musicCdIdentifier, StringComparison.OrdinalIgnoreCase));

            if ((musicCdFrame != null) && (trackNumberFrame == null))
            {
                // The 'Music CD Identifier' frame requires a present and valid TRCK frame.
                frames.Add(new Id3v2TextFrame(version, trackNumberIdentifier));
            }

            if (version >= Id3v2Version.Id3v240)
            {
                string lengthIdentifier = Id3v2TextFrame.GetIdentifier(version, Id3v2TextFrameIdentifier.Length);
                Id3v2Frame lengthFrame =
                    frames.OfType<Id3v2TextFrame>()
                        .FirstOrDefault(f => String.Equals(f.Identifier, lengthIdentifier, StringComparison.OrdinalIgnoreCase));

                string audioSeekPointIndexIdentifier = Id3v2Frame.GetIdentifier<Id3v2AudioSeekPointIndexFrame>(version);
                Id3v2Frame audioSeekPointIndexFrame =
                    frames.FirstOrDefault(
                        f =>
                        f is Id3v2AudioSeekPointIndexFrame
                        || String.Equals(f.Identifier, audioSeekPointIndexIdentifier, StringComparison.OrdinalIgnoreCase));

                if ((audioSeekPointIndexFrame != null) && (lengthFrame == null))
                {
                    // The presence of an 'Audio seek point index' frame requires the existence of a TLEN frame, indicating the duration of the file in milliseconds.
                    frames.Add(new Id3v2TextFrame(version, lengthIdentifier));
                }
            }
        }
    }
}
