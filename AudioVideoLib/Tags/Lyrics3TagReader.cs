/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://www.id3.org/lyrics3.html
 */

using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3 tag.
    /// </summary>
    public sealed partial class Lyrics3TagReader : IAudioTagReader
    {
        private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(Lyrics3Tag.HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = Encoding.ASCII.GetBytes(Lyrics3Tag.FooterIdentifier);

        private Encoding _encoding = Encoding.Default;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> used to read and write text to a byte array.
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _encoding = value;
            }
        }

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

            // This tag can only be located at the end; the header does not contain a size field, so we're not able to calculate the end.
            if (tagOrigin == TagOrigin.Start)
                return null;

            StreamBuffer sb = stream as StreamBuffer ?? new StreamBuffer(stream);
            Lyrics3Tag tag = new Lyrics3Tag { Encoding = Encoding };
            long footerOffset = FindFooterIdentifier(sb);
            if (footerOffset < 0)
                return null;

            sb.Position = Math.Max(footerOffset - Lyrics3Tag.MaxLyricsSize, 0);
            long headerOffset = FindHeaderIdentifier(sb);
            if (headerOffset < 0)
                return null;

            // Read lyrics
            int lyricsLength = (int)(footerOffset - (headerOffset + HeaderIdentifierBytes.Length));
            string lyrics = sb.ReadString(lyricsLength);
            tag.Lyrics = GetLyrics(lyrics);

            // Read footer
            sb.ReadString(FooterIdentifierBytes.Length);

            return new AudioTagOffset(tagOrigin, headerOffset, footerOffset, tag);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static long FindHeaderIdentifier(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            int headerSize = HeaderIdentifierBytes.Length;
            if (streamLength < headerSize)
                return -1;

            // Look for a footer before the current position
            long startPositionHeader = startPosition;
            long endPositionHeader = Math.Min(startPosition + Lyrics3Tag.MaxLyricsSize, streamLength);
            long tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, HeaderIdentifierBytes);
            return (tagPosition >= 0) ? tagPosition : -1;
        }

        private static long FindFooterIdentifier(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long startPosition = stream.Position;
            long streamLength = stream.Length;
            int footerSize = FooterIdentifierBytes.Length;
            if (streamLength < footerSize)
                return -1;
        
            // Look for a footer at the current position
            long startPositionHeader = Math.Max(startPosition - footerSize, 0);
            long endPositionHeader = startPositionHeader + footerSize;
            long tagPosition = ReadIdentifier(stream, startPositionHeader, endPositionHeader, FooterIdentifierBytes);
            if (tagPosition >= 0)
                return tagPosition;

            // Look for a footer before the current position
            startPositionHeader = Math.Max(startPositionHeader - footerSize, 0);
            endPositionHeader = startPositionHeader + footerSize;
            tagPosition = ReadIdentifier(stream, endPositionHeader, endPositionHeader, FooterIdentifierBytes);
            return (tagPosition >= 0) ? tagPosition : -1;
        }

        private static long ReadIdentifier(Stream sb, long startPosition, long endPosition, byte[] identifierBytes)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            sb.Position = startPosition;
            while (startPosition < endPosition)
            {
                int y = 0;
                for (int b = sb.ReadByte(); b == identifierBytes[y++]; b = sb.ReadByte())
                {
                    startPosition++;
                    if (y == identifierBytes.Length)
                        return sb.Position - identifierBytes.Length;
                }
                startPosition++;
            }
            return -1;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private string GetLyrics(string value)
        {
            byte[] lyricBytes = StreamBuffer.GetTruncatedEncodedBytes(value, _encoding, Lyrics3Tag.MaxLyricsSize);

            for (int i = 0; i < lyricBytes.Length; i++)
            {
                // A byte in the text must not have the binary value 255.
                if (lyricBytes[i] != 0xFF)
                    continue;

                // Otherwise, replace the invalid value with a space character.
                lyricBytes[i] = 0x20;
            }
            return _encoding.GetString(lyricBytes);
        }
    }
}
