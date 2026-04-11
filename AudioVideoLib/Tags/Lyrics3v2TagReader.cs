/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 *  http://www.mpx.cz/mp3manager/tags.htm
 */

namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class to store a Lyrics3v2 tag.
/// </summary>
public sealed partial class Lyrics3v2TagReader : IAudioTagReader
{
    private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(Lyrics3v2Tag.HeaderIdentifier);

    private static readonly byte[] FooterIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(Lyrics3v2Tag.FooterIdentifier);

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("stream can not be read");
        }

        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("stream can not be seeked");
        }

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var startOffset = FindIdentifier(sb, tagOrigin);
        if (startOffset < 0)
        {
            return null;
        }

        long endOffset = 0;
        if (tagOrigin == TagOrigin.End)
        {
            endOffset = startOffset + FooterIdentifierBytes.Length;

            sb.Position = Math.Max(sb.Position - FooterIdentifierBytes.Length - Lyrics3v2Tag.TagSizeLength, 0);
            var strTagSize = sb.ReadString(Lyrics3v2Tag.TagSizeLength);
            long tagSize;
            if (!long.TryParse(strTagSize, out tagSize))
            {
#if DEBUG
                throw new InvalidDataException(string.Format("Can't parse size value {0} before footer.", strTagSize));
#else
                return null;
#endif
            }
            var currentPosition = sb.Position;

            // The Lyrics3 block ends with a six character size descriptor and the string "LYRICS200".
            // The size value includes the "LYRICSBEGIN" string, but does not include the 6 character size descriptor and the trailing "LYRICS200" string.
            sb.Position = Math.Max(sb.Position - tagSize - Lyrics3v2Tag.TagSizeLength, 0);

            startOffset = FindIdentifier(sb, TagOrigin.Start);
            if (startOffset < 0)
            {
                // Header could be off
                sb.Position = Math.Max(sb.Position - (tagSize - 5), 0);

                if (sb.Position > 0)
                {
                    startOffset = FindIdentifier(sb, TagOrigin.End);
                    if (startOffset < 0)
                    {
                        startOffset = sb.Position = currentPosition;
                    }
                }
            }
        }

        // Read the fields.
        List<Lyrics3v2Field> fields = [];
        long bytesRead = HeaderIdentifierBytes.Length;
        var streamLength = (endOffset > 0) ? endOffset : Math.Max(sb.Length, endOffset);
        while (startOffset + bytesRead < streamLength)
        {
            var startPosition = sb.Position;

            // Read field if needed.
            var field = Lyrics3v2Field.ReadFromStream(sb, streamLength - bytesRead);
            if (field != null)
            {
                var fieldParseEventArgs = new Lyrics3v2FieldParseEventArgs(field);
                OnFieldParse(fieldParseEventArgs);

                if (!fieldParseEventArgs.Cancel)
                {
                    // Call after read event.
                    var parsedEventArgs = new Lyrics3v2FieldParsedEventArgs(field);
                    OnFieldParsed(parsedEventArgs);

                    fields.Add(field);
                }
            }
            else
            {
                sb.Position = startPosition;
                break;
            }
            bytesRead += sb.Position - startPosition;
        }

        if (endOffset == 0)
        {
            // The Lyrics3 block ends with a six character size descriptor and the string "LYRICS200".
            // The size value includes the "LYRICSBEGIN" string, but does not include the 6 character size descriptor and the trailing "LYRICS200" string.
            var strTagSize = sb.ReadString(6);
            long tagSize;
            if (!long.TryParse(strTagSize, out tagSize))
            {
#if DEBUG
                throw new InvalidDataException(string.Format("Can't parse size value {0} before footer.", strTagSize));
#else
                return null;
#endif
            }

            int bRead;
            var footerIdentifier = sb.ReadString(FooterIdentifierBytes.Length, true, out bRead);
            if (!string.Equals(footerIdentifier, Lyrics3v2Tag.FooterIdentifier))
            {
#if DEBUG
                throw new InvalidDataException(
                    string.Format("Footer identifier {0} not recognized; expected {1}", footerIdentifier, Lyrics3v2Tag.FooterIdentifier));
#else
                sb.Position -= bRead;
#endif
            }
            endOffset = sb.Position;
        }

#if DEBUG
        var totalDataSize = endOffset - startOffset - Lyrics3v2Tag.TagSizeLength - FooterIdentifierBytes.Length;
        //if (totalDataSize != bytesRead)
        //{
        //    // If the amount of bytes we read doesn't match the size of the tag indicated in the header,
        //    throw new InvalidDataException(
        //        String.Format(
        //            "Amount of bytes read ({0}) does not match size indicated before footer ({1}).",
        //            totalDataSize,
        //            bytesRead));
        //}
#endif

        var tag = new Lyrics3v2Tag();
        tag.SetFields(fields);
        return new AudioTagOffset(tagOrigin, startOffset, endOffset, tag);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static long FindIdentifier(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var startPosition = stream.Position;
        var streamLength = stream.Length;
        var headerSize = HeaderIdentifierBytes.Length;
        if (streamLength < headerSize)
        {
            return -1;
        }

        if (tagOrigin == TagOrigin.Start)
        {
            // Look for a header at the current position
            var startPositionHeader = startPosition;
            var endPositionHeader = Math.Min(startPositionHeader + headerSize, streamLength);
            var tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }

            // Look for a header past the current position
            startPositionHeader = endPositionHeader;
            endPositionHeader = Math.Min(startPositionHeader + headerSize, streamLength);
            tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }
        }
        else if (tagOrigin == TagOrigin.End)
        {
            // The Lyrics3 block ends with a six character size descriptor and the string "LYRICS200".
            // The size value includes the "LYRICSBEGIN" string, but does not include the 6 character size descriptor and the trailing "LYRICS200" string.
            var footerSize = FooterIdentifierBytes.Length;

            // Look for a footer at before current position
            var startPositionHeader = Math.Max(startPosition - footerSize, 0);
            var endPositionHeader = Math.Min(startPosition, streamLength);
            var tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }

            // Look for a footer before the previous start position
            startPositionHeader = Math.Max(startPositionHeader - footerSize, 0);
            endPositionHeader = Math.Min(startPositionHeader, streamLength);
            tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
            if (tagPosition >= 0)
            {
                return tagPosition;
            }
        }
        return -1;
    }

    private static long ReadIdentifier(Stream sb, long startPosition, long endPosition, IList<byte> identifierBytes)
    {
        ArgumentNullException.ThrowIfNull(sb);

        sb.Position = startPosition;
        while (startPosition < endPosition)
        {
            var y = 0;
            for (var b = sb.ReadByte(); b == identifierBytes[y++]; b = sb.ReadByte())
            {
                startPosition++;
                if (y == identifierBytes.Count)
                {
                    return sb.Position - identifierBytes.Count;
                }
            }
            startPosition++;
        }
        return -1;
    }
}
