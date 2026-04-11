/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://www.id3.org/lyrics3.html
 */

namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

/// <summary>
/// Class to store a Lyrics3 tag.
/// </summary>
public sealed partial class Lyrics3TagReader : IAudioTagReader
{
    private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(Lyrics3Tag.HeaderIdentifier);

    private static readonly byte[] FooterIdentifierBytes = Encoding.ASCII.GetBytes(Lyrics3Tag.FooterIdentifier);

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the <see cref="Encoding"/> used to read and write text to a byte array.
    /// </summary>
    public Encoding Encoding
    {
        get;

        set
        {
            field = value ?? throw new ArgumentNullException("value");
        }
    } = Encoding.Default;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("stream can not be read");
        }

        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("stream can not be seeked");
        }

        // This tag can only be located at the end; the header does not contain a size field, so we're not able to calculate the end.
        if (tagOrigin == TagOrigin.Start)
        {
            return null;
        }

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var tag = new Lyrics3Tag { Encoding = Encoding };
        var footerOffset = FindFooterIdentifier(sb);
        if (footerOffset < 0)
        {
            return null;
        }

        sb.Position = Math.Max(footerOffset - Lyrics3Tag.MaxLyricsSize, 0);
        var headerOffset = FindHeaderIdentifier(sb);
        if (headerOffset < 0)
        {
            return null;
        }

        // Read lyrics
        var lyricsLength = (int)(footerOffset - (headerOffset + HeaderIdentifierBytes.Length));
        var lyrics = sb.ReadString(lyricsLength);
        tag.Lyrics = GetLyrics(lyrics);

        // Read footer
        sb.ReadString(FooterIdentifierBytes.Length);

        return new AudioTagOffset(tagOrigin, headerOffset, footerOffset, tag);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static long FindHeaderIdentifier(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        var startPosition = stream.Position;
        var streamLength = stream.Length;
        var headerSize = HeaderIdentifierBytes.Length;
        if (streamLength < headerSize)
        {
            return -1;
        }

        // Look for a footer before the current position
        var startPositionHeader = startPosition;
        var endPositionHeader = Math.Min(startPosition + Lyrics3Tag.MaxLyricsSize, streamLength);
        var tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
        return (tagPosition >= 0) ? tagPosition : -1;
    }

    private static long FindFooterIdentifier(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        var startPosition = stream.Position;
        var streamLength = stream.Length;
        var footerSize = FooterIdentifierBytes.Length;
        if (streamLength < footerSize)
        {
            return -1;
        }

        // Look for a footer at the current position
        var startPositionHeader = Math.Max(startPosition - footerSize, 0);
        var endPositionHeader = startPositionHeader + footerSize;
        var tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
        if (tagPosition >= 0)
        {
            return tagPosition;
        }

        // Look for a footer before the current position
        startPositionHeader = Math.Max(startPositionHeader - footerSize, 0);
        endPositionHeader = startPositionHeader + footerSize;
        tagPosition = ReadIdentifier(stream, endPositionHeader, endPositionHeader, FooterIdentifierBytes);
        return (tagPosition >= 0) ? tagPosition : -1;
    }

    private static long ReadIdentifier(Stream sb, long startPosition, long endPosition, byte[] identifierBytes)
    {
        if (sb == null)
        {
            throw new ArgumentNullException("sb");
        }

        sb.Position = startPosition;
        while (startPosition < endPosition)
        {
            var y = 0;
            for (var b = sb.ReadByte(); b == identifierBytes[y++]; b = sb.ReadByte())
            {
                startPosition++;
                if (y == identifierBytes.Length)
                {
                    return sb.Position - identifierBytes.Length;
                }
            }
            startPosition++;
        }
        return -1;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private string GetLyrics(string value)
    {
        var lyricBytes = StreamBuffer.GetTruncatedEncodedBytes(value, Encoding, Lyrics3Tag.MaxLyricsSize);

        for (var i = 0; i < lyricBytes.Length; i++)
        {
            // A byte in the text must not have the binary value 255.
            if (lyricBytes[i] != 0xFF)
            {
                continue;
            }

            // Otherwise, replace the invalid value with a space character.
            lyricBytes[i] = 0x20;
        }
        return Encoding.GetString(lyricBytes);
    }
}
