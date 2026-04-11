/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTagReader : IAudioTagReader
    {    
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
            
            ApeHeader headerOrFooter = ReadHeader(sb, tagOrigin);
            if (headerOrFooter == null)
                return null;

            // Don't throw an exception; just return here. The read header could be a false match.
            if (headerOrFooter.Size > ApeTag.MaxAllowedSize)
                return null;

            ApeTag tag = new ApeTag(headerOrFooter.Version, headerOrFooter.Flags);
            long startOffset, endOffset;
            ApeHeader header = null, footer = null;
            if (tag.IsHeader)
            {
                header = headerOrFooter;
                startOffset = header.Position;
                endOffset = Math.Min(startOffset + ApeTag.HeaderSize + header.Size, sb.Length);
                if (endOffset > sb.Length)
                {
#if DEBUG
                    throw new EndOfStreamException("Tag at start could not be read: stream is truncated.");
#else
                    return null;
#endif
                }
                tag.UseHeader = true;
            }
            else
            {
                footer = headerOrFooter;
                endOffset = Math.Min(footer.Position + ApeTag.FooterSize, sb.Length);
                startOffset = Math.Max(endOffset - footer.Size - (tag.UseHeader ? ApeTag.HeaderSize : 0), 0);
                if (endOffset > sb.Length)
                {
#if DEBUG
                    throw new EndOfStreamException("Tag at end could not be read: stream is truncated.");
#else
                    return null;
#endif
                }
                tag.UseFooter = true;

                // Seek to the start of the tag.
                sb.Seek(startOffset, SeekOrigin.Begin);

                if (tag.UseHeader)
                {
                    // Read the 'header' as if it was the start of the tag
                    header = ReadHeader(sb, TagOrigin.Start);
                    if (header == null)
                    {
                        // Size in the header could be off.
                        // Read the 'footer' as if it was at the end of the tag
                        sb.Seek(startOffset, SeekOrigin.Begin);
                        header = ReadHeader(sb, TagOrigin.End);
                        if (header == null)
                        {
                            tag.UseHeader = false;
                            sb.Seek(startOffset, SeekOrigin.Begin);
                        }
                    }

                    if (header != null)
                    {
                        startOffset = header.Position;
                        header.Size = (int)Math.Max(endOffset - (startOffset + ApeTag.HeaderSize), 0);
                        footer.Size = Math.Max(header.Size, footer.Size);
                    }

                    if (footer.Size > ApeTag.MaxAllowedSize)
                    {
#if DEBUG
                        throw new InvalidDataException(
                            String.Format("Size ({0}) is larger than the max allowed size ({1})", footer.Size, ApeTag.MaxAllowedSize));
#else
                        return null;
#endif
                    }
                }
                ValidateHeader(header, footer);
            }

            int totalSizeItems = Math.Max(headerOrFooter.Size - ApeTag.FooterSize, 0);
            if ((sb.Length - sb.Position) < totalSizeItems)
            {
#if DEBUG
                throw new IndexOutOfRangeException(
                    String.Format("Ape field ({0}) is bigger than amount of bytes left in stream ({1}).", totalSizeItems, sb.Length - sb.Position));
#else
                return null;
#endif
            }

            // Parse the individual frames.
            List<ApeItem> items = new List<ApeItem>();
            long bytesRead = 0;
            while (bytesRead < totalSizeItems)
            {
                long startPosition = sb.Position;

                ApeItem item = ApeItem.ReadFromStream(tag.Version, sb, totalSizeItems - bytesRead);
                if (item != null)
                {
                    ApeItemParseEventArgs parseEventArgs = new ApeItemParseEventArgs(item);
                    OnItemParse(parseEventArgs);

                    if (!parseEventArgs.Cancel)
                    {
                        if (parseEventArgs.Item != null)
                            item = parseEventArgs.Item;

                        if (item != null)
                        {
                            // Call after read event.
                            ApeItemParsedEventArgs parsedEventArgs = new ApeItemParsedEventArgs(item);
                            OnItemParsed(parsedEventArgs);

                            items.Add(parsedEventArgs.Item);
                        }
                    }
                }
                bytesRead += (sb.Position - startPosition);
            }

#if DEBUG
            //if (items.Count != headerOrFooter.FrameCount)
                //throw new InvalidDataException("items.Count does not match FrameCount");

            if (items.Count > ApeTag.MaxAllowedFields)
            {
                throw new InvalidDataException(
                    String.Format("Tag has more fields ('{0}') than the allowed max fields count ('{1}').", items.Count, ApeTag.MaxAllowedFields));
            }

            //if (bytesRead != totalSizeItems)
            //{
            //    throw new InvalidDataException(
            //        String.Format("Amount of bytes read ({0}) does not match expected size ({1}).", bytesRead, totalSizeItems));
            //}
#endif

            if (tag.IsHeader && tag.UseFooter)
            {
                sb.Position += ApeTag.FooterSize;
                footer = ReadHeader(sb, TagOrigin.End);
            }

            ValidateHeader(header, footer);

            return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static ApeHeader ReadHeader(StreamBuffer stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            if (streamLength < ApeTag.HeaderSize)
                return null;

            // Look for a header at the current position
            if (tagOrigin == TagOrigin.Start)
            {
                // Look for a header at the current position
                long startPositionHeader = startPosition;
                long endPositionHeader = Math.Min(startPositionHeader + ApeTag.HeaderSize, streamLength);
                ApeHeader hdr = ReadHeader(stream, startPositionHeader, endPositionHeader);
                if (hdr != null)
                    return hdr;

                // Look for a header past the current position
                startPositionHeader = endPositionHeader;
                endPositionHeader = Math.Min(startPositionHeader + ApeTag.HeaderSize, streamLength);
                hdr = ReadHeader(stream, startPositionHeader, endPositionHeader);
                if (hdr != null)
                    return hdr;
            }
            else if (tagOrigin == TagOrigin.End)
            {
                // Look for a footer before the current position
                long startPositionHeader = Math.Max(startPosition - ApeTag.FooterSize, 0);
                long endPositionHeader = Math.Min(startPositionHeader + ApeTag.FooterSize, streamLength);
                ApeHeader hdr = ReadHeader(stream, startPositionHeader, endPositionHeader);
                if (hdr != null)
                    return hdr;

                // Look for a footer before the previous start position
                startPositionHeader = Math.Max(startPositionHeader - ApeTag.FooterSize, 0);
                endPositionHeader = Math.Min(startPositionHeader + ApeTag.FooterSize, streamLength);
                hdr = ReadHeader(stream, startPositionHeader, endPositionHeader);
                if (hdr != null)
                    return hdr;
            }
            return null;
        }

        private static ApeHeader ReadHeader(StreamBuffer stream, long startHeaderPosition, long endHeaderPosition)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            stream.Position = startHeaderPosition;
            while (startHeaderPosition < endHeaderPosition)
            {
                int y = 0;
                while (stream.ReadByte() == TagIdentifierBytes[y++])
                {
                    startHeaderPosition++;
                    if (y != TagIdentifierBytes.Length)
                        continue;

                    ApeHeader hdr = new ApeHeader
                    {
                        Position = stream.Position - TagIdentifierBytes.Length,
                        Version = (ApeVersion)(stream.ReadLittleEndianInt32() / 1000),
                        Size = stream.ReadLittleEndianInt32(),
                        FrameCount = stream.ReadLittleEndianInt32(),
                        Flags = stream.ReadLittleEndianInt32(),
                        ReservedBytes = new byte[8]
                    };

                    stream.Read(hdr.ReservedBytes, 8);
                    if (IsValidTag(hdr))
                        return hdr;

                    startHeaderPosition -= ApeTag.HeaderSize;
                    stream.Position = startHeaderPosition + 1;
                    break;
                }
                startHeaderPosition++;
            }
            return null;
        }
    }
}
