/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a MusicMatch tag.
    /// </summary>
    /// The MusicMatch Tagging Format was designed to store specific types of audio meta-data inside the audio file itself.
    /// As the format was used exclusively by the MusicMatch Jukebox application, it is used only with MPEG-1/2 layer III files encoded with that program.
    /// However, its tagging format is not inherently exclusive of other audio formats, and could conceivably be used with other types of encodings.
    /// <para />
    /// MusicMatch tags were originally designed to come at the very end of MP3 files, after all of the MP3 audio frames.
    /// Starting with Jukebox version 3.1, the application became more ID3-friendly and started placing ID3v1 tags after the MusicMatch tag as well.
    /// In practice, since very few applications outside of the MusicMatch Jukebox are capable of reading and understanding this format, 
    /// it is not unusual to find MusicMatch tags "buried" within mp3 files, coming before other types of tagging formats in a file, 
    /// such as Lyrics3 or ID3v2.4.0.
    /// Such "relocations" are not uncommon, and therefore any software application that intends to find, read, 
    /// and parse MusicMatch tags should be flexible in this endeavor, despite the apparent intentions of the original specification.
    /// <para />
    /// Although various sections of a MusicMatch tag are fixed in length, other sections are not, and so tag lengths can vary from one file to another.
    /// A valid MusicMatch tag will be at least 8 kilobytes (8192 bytes) in length.
    /// Those tags with image data will often be much larger.
    /// <para />
    /// The byte-order in 4-byte pointers and multibyte numbers for MusicMatch tags is least-significant byte (LSB) first, also known as "little endian".
    /// For example, 0x12345678 is encoded as 0x78 0x56 0x34 0x12.
    public sealed partial class MusicMatchTagReader : IAudioTagReader
    {
        private static readonly int[] AudioMetaDataSizes = MusicMatchTag.AudioMetaDataSizes;

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
            MusicMatchTag tag = new MusicMatchTag();

            // Should we read the header or footer?
            MusicMatchHeader headerOrFooter = (tagOrigin == TagOrigin.Start) ? ReadHeader(sb, tagOrigin) : ReadFooter(sb, tagOrigin);
            if (headerOrFooter == null)
                return null;

            long startOffset = 0, endOffset = 0, streamLength = sb.Length;
            MusicMatchHeader header = null, footer = null, versionInformation = null;
            if (tagOrigin == TagOrigin.Start)
            {
                header = headerOrFooter;
                tag.UseHeader = true;
                startOffset = header.Position;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                footer = headerOrFooter;
                endOffset = Math.Min(footer.Position + MusicMatchTag.FooterSize, streamLength);

                // Seek to the Audio metadata offset struct
                sb.Position = Math.Max(endOffset - MusicMatchTag.FooterSize - MusicMatchTag.DataOffsetFieldsSize, 0);

                // Read the Audio Meta-data Offsets
                MusicMatchDataOffsets dataOffsets = ReadDataOffsets(sb);

                long startPosition = Math.Max(sb.Position + MusicMatchTag.FooterSize, 0);
                foreach (int audioMetaDataOffset in AudioMetaDataSizes.OrderBy(a => a))
                {
                    // Seek to the start of the version information, based on the audioMetaDataOffset
                    long startPositionHeader = Math.Max(startPosition - audioMetaDataOffset - MusicMatchTag.HeaderSize, 0);
                    long endPositionHeader = startPositionHeader + HeaderIdentifierBytes.Length;

                    // Read the version information
                    versionInformation = ReadHeaderFooter(sb, startPositionHeader, endPositionHeader, HeaderIdentifierBytes, TagOrigin.Start);
                    if (versionInformation == null)
                        continue;

                    startOffset = (versionInformation.Position + MusicMatchTag.HeaderSize) - dataOffsets.TotalDataOffsetsSize;
                    sb.Position = Math.Max(startOffset - MusicMatchTag.HeaderSize, 0);
                    header = ReadHeader(sb, TagOrigin.Start);
                    break;
                }

                // We've got a problem, Houston.
                if (versionInformation == null)
                    return null;

                // Header found? set new startPosition
                if (header != null)
                {
                    if (header.Position == versionInformation.Position)
                    {
                        header = null;
                    }
                    else
                    {
                        tag.UseHeader = true;
                        startOffset = header.Position;
                    }
                }
            }

            sb.Position = startOffset + (tag.UseHeader ? MusicMatchTag.HeaderSize : 0);

            // Read image
            tag.Image = MusicMatchImage.ReadFromStream(sb);

            // Unused, null padding
            byte[] nullPadding = new byte[4];
            sb.Read(nullPadding, nullPadding.Length);

            // Read version info
            if (versionInformation == null)
            {
                versionInformation = ReadHeader(sb, TagOrigin.Start);
                if (versionInformation == null)
                {
#if DEBUG
                    throw new InvalidDataException("Version information not found in the tag.");
#else
                    return null;
#endif
                }
            }
            else
            {
                // version information has the same size as the header size.
                sb.Position += MusicMatchTag.HeaderSize;
            }

            if (tag.UseHeader && !versionInformation.Equals(header))
            {
#if DEBUG
                throw new InvalidDataException("Version information field(s) mismatch the header fields.");
#else
                return null;
#endif
            }

            tag.XingEncoderVersion = versionInformation.XingEncoderVersion;
            tag.Version = versionInformation.MusicMatchVersion;

            // Read the Audio meta-data
            ReadAudioMetaFields(sb, tag);

            // Padding
            sb.Position += 16;

            if (tagOrigin == TagOrigin.Start)
            {
                long startPosition = sb.Position;
                foreach (int audioMetaDataOffset in AudioMetaDataSizes.OrderBy(a => a))
                {
                    //Look for the start of the footer.
                    long startPositionFooter = Math.Max(startPosition + (audioMetaDataOffset - (sb.Position - startOffset) - (tag.UseHeader ? MusicMatchTag.HeaderSize : 0)), 0);
                    long endPositionFooter = startPositionFooter + FooterIdentifierBytes.Length;

                    // Read the footer.
                    footer = ReadHeaderFooter(sb, startPositionFooter, endPositionFooter, FooterIdentifierBytes, TagOrigin.End);
                    if (footer != null)
                        break;
                }

                // We've got a problem, Houston.
                if (footer == null)
                    return null;
            }
            return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static int GetAudioMetaDataSize(float version)
        {
            if (version <= 3.00)
                return 7868;

            return 0;
        }

        private static MusicMatchHeader ReadHeader(StreamBuffer stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            if (streamLength < MusicMatchTag.HeaderSize)
                return null;

            if (tagOrigin == TagOrigin.Start)
            {
                // Look for a header at the current position
                long startPositionHeader = startPosition;
                long endPositionHeader = Math.Min(startPositionHeader + HeaderIdentifierBytes.Length, streamLength);
                MusicMatchHeader header = ReadHeaderFooter(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes, TagOrigin.Start);
                if (header != null)
                    return header;

                // Look for a header past the current position
                startPositionHeader = endPositionHeader;
                endPositionHeader = Math.Min(startPositionHeader + HeaderIdentifierBytes.Length, streamLength);
                header = ReadHeaderFooter(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes, TagOrigin.Start);
                if (header != null)
                    return header;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                // Look for a footer before the current position
                long startPositionHeader = Math.Max(startPosition - HeaderIdentifierBytes.Length, 0);
                long endPositionHeader = Math.Min(startPositionHeader + HeaderIdentifierBytes.Length, streamLength);
                MusicMatchHeader footer = ReadHeaderFooter(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;

                // Look for a footer before the previous start position
                startPositionHeader = Math.Max(startPositionHeader - HeaderIdentifierBytes.Length, 0);
                endPositionHeader = Math.Min(startPositionHeader + HeaderIdentifierBytes.Length, streamLength);
                footer = ReadHeaderFooter(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;
            }
            return null;
        }

        private static MusicMatchHeader ReadFooter(StreamBuffer stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            if (streamLength < MusicMatchTag.HeaderSize)
                return null;

            if (tagOrigin == TagOrigin.Start)
            {
                // Look for a footer before the current position
                long startPositionFooter = Math.Max(startPosition, 0);
                long endPositionFooter = Math.Min(startPosition + FooterIdentifierBytes.Length, streamLength);
                MusicMatchHeader footer = ReadHeaderFooter(stream, startPositionFooter, endPositionFooter, FooterIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;

                // Look for a footer before the previous start position
                startPositionFooter = Math.Max(startPositionFooter - FooterIdentifierBytes.Length, 0);
                endPositionFooter = Math.Min(startPosition, streamLength);
                footer = ReadHeaderFooter(stream, startPositionFooter, endPositionFooter, FooterIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                // Look for a footer before the current position
                long startPositionFooter = Math.Max(startPosition - MusicMatchTag.FooterSize, 0);
                long endPositionFooter = Math.Min(startPosition, streamLength);
                MusicMatchHeader footer = ReadHeaderFooter(stream, startPositionFooter, endPositionFooter, FooterIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;

                // Look for a footer before the previous start position
                startPositionFooter = Math.Max(startPositionFooter - MusicMatchTag.FooterSize - FooterIdentifierBytes.Length, 0);
                endPositionFooter = Math.Min(startPosition, streamLength);
                footer = ReadHeaderFooter(stream, startPositionFooter, endPositionFooter, FooterIdentifierBytes, TagOrigin.End);
                if (footer != null)
                    return footer;
            }
            return null;
        }

        private static MusicMatchHeader ReadHeaderFooter(StreamBuffer stream, long startHeaderPosition, long endHeaderPosition, IList<byte> identifierBytes, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            stream.Position = startHeaderPosition;
            while (startHeaderPosition < endHeaderPosition)
            {
                int y = 0;
                while (stream.ReadByte() == identifierBytes[y++])
                {
                    startHeaderPosition++;
                    if (y != identifierBytes.Count)
                        continue;

                    if (tagOrigin == TagOrigin.Start)
                    {
                        MusicMatchHeader header = new MusicMatchHeader
                        {
                            Position = stream.Position - identifierBytes.Count,
                            Padding1 = new byte[2],
                            Padding2 = new byte[2],
                            Padding3 = new byte[2],
                            SpacePadding2 = new byte[226]
                        };

                        // 0x00 0x00 Padding
                        stream.Read(header.Padding1, header.Padding1.Length);

                        // <8-byte numerical ASCII string>
                        header.XingEncoderVersion = stream.ReadString(8);

                        // 0x00 0x00 Padding
                        stream.Read(header.Padding2, header.Padding2.Length);

                        // Xing encoder version <8-byte numerical ASCII string>
                        header.MusicMatchVersion = stream.ReadString(8);

                        // 0x00 0x00 Padding
                        stream.Read(header.Padding3, header.Padding3.Length);

                        // Space padding <226 * 0x20 >                    
                        stream.Read(header.SpacePadding2, header.SpacePadding2.Length);

                        ValidateHeader(header, null);
                        return header;
                    }

                    if (tagOrigin == TagOrigin.End)
                    {
                        MusicMatchHeader footer = new MusicMatchHeader
                        {
                            Position = stream.Position - identifierBytes.Count,
                            SpacePadding1 = new byte[13],
                            SpacePadding2 = new byte[12]
                        };

                        // Space padding <13 * 0x20>
                        stream.Read(footer.SpacePadding1, 13);

                        // version <4-byte numerical ASCII string> e.g. 3.05
                        footer.MusicMatchVersion = stream.ReadString(4);

                        // Space padding <12 * 0x20>
                        stream.Read(footer.SpacePadding2, 12);

                        ValidateHeader(null, footer);
                        return footer;
                    }
                }
                startHeaderPosition++;
            }
            return null;
        }

        private static string ReadTextField(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            int size = sb.ReadInt16();
            return sb.ReadString(size);
        }

        private static void ReadAudioMetaFields(StreamBuffer sb, MusicMatchTag tag)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            // Single-line text fields
            tag.SongTitle = ReadTextField(sb);
            tag.AlbumTitle = ReadTextField(sb);
            tag.ArtistName = ReadTextField(sb);
            tag.Genre = ReadTextField(sb);
            tag.Tempo = ReadTextField(sb);
            tag.Mood = ReadTextField(sb);
            tag.Situation = ReadTextField(sb);
            tag.Preference = ReadTextField(sb);

            // Non-text fields
            tag.SongDuration = ReadTextField(sb);
            tag.CreationDate = DateTime.FromOADate(sb.ReadDouble());
            tag.PlayCounter = sb.ReadInt32();
            tag.OriginalFilename = ReadTextField(sb);
            tag.SerialNumber = ReadTextField(sb);
            tag.TrackNumber = sb.ReadInt16();

            // Multi-line text fields
            tag.Notes = ReadTextField(sb);
            tag.ArtistBio = ReadTextField(sb);
            tag.Lyrics = ReadTextField(sb);

            // Internet addresses
            tag.ArtistUrl = ReadTextField(sb);
            tag.BuyCdUrl = ReadTextField(sb);
            tag.ArtistEmail = ReadTextField(sb);
        }

        private static MusicMatchDataOffsets ReadDataOffsets(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            return new MusicMatchDataOffsets
                       {
                           ImageExtensionOffset = sb.ReadInt32(),
                           ImageBinaryOffset = sb.ReadInt32(),
                           UnusedOffset = sb.ReadInt32(),
                           VersionInfoOffset = sb.ReadInt32(),
                           AudioMetaDataOffset = sb.ReadInt32()
                       };
        }
    }
}
