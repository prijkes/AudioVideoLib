/*
 * Date: 2013-10-20
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
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeTag"/> item.
    /// </summary>
    public partial class ApeItem
    {
        /// <summary>
        /// Reads an <see cref="ApeItem" /> from a <see cref="Stream" /> at the current position.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="maximumItemSize">Maximum size of the item.</param>
        /// <returns>
        /// An <see cref="ApeItem" /> if found; otherwise, null.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static ApeItem ReadFromStream(ApeVersion version, Stream stream, long maximumItemSize)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return ReadItem(version, stream as StreamBuffer ?? new StreamBuffer(stream), maximumItemSize);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static ApeItem ReadItem(ApeVersion version, StreamBuffer sb, long maximumItemSize)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            // Find the item.
            long startPosition = sb.Position;
            long bytesToSkip = GetBytesUntilNextItem(version, sb, maximumItemSize);

            // Not found; return null.
            if (bytesToSkip == -1)
                return null;

            // Move the stream position to the start of the frame.
            sb.Position = startPosition + bytesToSkip;

            int valueSize = sb.ReadLittleEndianInt32();
            if ((valueSize <= 0) || (valueSize > ApeTag.MaxAllowedSize))
                return null;

            int flags = sb.ReadLittleEndianInt32();
            maximumItemSize -= 8;

            int itemKeyLengthBytes = 0;
            long maximumBytesToRead = maximumItemSize;
            if (maximumBytesToRead > 0)
            {
                for (int y = 0; y < maximumBytesToRead; y++)
                {
                    int character = sb.ReadByte();
                    if (character == 0x00) // 0x00 byte - string terminator.
                        break;

                    itemKeyLengthBytes++;
                }
            }

            // Name
            sb.Seek(-(itemKeyLengthBytes + 1), SeekOrigin.Current);
            string key = sb.ReadString(itemKeyLengthBytes, Encoding.UTF8);
            if ((key.Length < MinKeyLengthCharacters) || (itemKeyLengthBytes > MaxKeyLengthBytes))
                return null;

            // Skip Item key terminator (0x00 byte)
            int keyTerminator = sb.ReadByte();
            if (keyTerminator != 0x00)
                return null;

            ApeItem item = GetItem(version, key, flags);
            return item.ReadItem(version, valueSize, sb, maximumItemSize) ? item : null;
        }

        private bool ReadItem(ApeVersion version, int valueSize, StreamBuffer stream, long maximumItemSize)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            // Check if the item size indicated is really the size of the item.
            // Some files just aren't... properly written.
            byte[] data = new byte[valueSize];
            int dataBytesRead = stream.Read(data, valueSize);
            int bytesLeftInStream = (int)(maximumItemSize - dataBytesRead);
            if ((valueSize > 0) && (bytesLeftInStream > 0) && (dataBytesRead >= valueSize))
            {
                using (StreamBuffer dataBuffer = new StreamBuffer(data))
                {
                    // See if there's a next item.
                    // Try to find the start of the next item.
                    long startPositionNextItem = stream.Position;
                    long bytesUntilNextItem = GetBytesUntilNextItem(version, stream, maximumItemSize - valueSize);
                    stream.Position = startPositionNextItem;

                    // Seems that the size indicated by the item is not the total size of the item; read the extra bytes here.
                    if (bytesUntilNextItem > 0)
                    {
                        data = new byte[bytesUntilNextItem];
                        stream.Read(data, data.Length);
                        dataBuffer.Write(data);
                    }
                    data = dataBuffer.ToByteArray();
                }
            }
            Data = data;
            return true;
        }

        private static long GetBytesUntilNextItem(ApeVersion version, Stream stream, long maximumNextItemSize)
        {
            long startPosition = stream.Position;
            long initialPosition = startPosition;

            long nextItemSizeValue = 0;
            int nextItemKeyLength = 0;
            long nextItemFlags = 0;
            int nextItemHeaderLengthRead = 0;
            long totalBytesRead = 0;
            List<byte> nextItemKeyBytes = new List<byte>();
            while (totalBytesRead++ < Math.Min(stream.Length, maximumNextItemSize))
            {
                byte b = (byte)stream.ReadByte();
                if (nextItemHeaderLengthRead < 4)
                {
                    // This byte is part of the size field of the next item.
                    long val = b;

                    // The first byte is the most right byte (size field length is an int)
                    val <<= 8 * nextItemHeaderLengthRead;
                    nextItemSizeValue |= val;
                    nextItemHeaderLengthRead++;
                    continue;
                }
                else if (nextItemHeaderLengthRead < (4 + 4))
                {
                    // This byte is part of the flags field of the next item.
                    long val = b;

                    // The first byte is the most right byte (size field length is an int)
                    val <<= 8 * (nextItemHeaderLengthRead - 4);
                    nextItemFlags |= val;
                    nextItemHeaderLengthRead++;
                    continue;
                }
                else if (nextItemHeaderLengthRead <= (4 + 4 + Math.Max(nextItemKeyLength, MinKeyLengthCharacters)))
                {
                    if (b != 0x00)
                    {
                        nextItemKeyBytes.Add(b);
                        nextItemKeyLength++;
                        nextItemHeaderLengthRead++;
                        continue;
                    }

                    if ((b == 0x00) && (nextItemKeyLength >= 2))
                    {
                        nextItemHeaderLengthRead++;
                        continue;
                    }
                }
                else if (nextItemHeaderLengthRead == (4 + 4 + nextItemKeyLength + 1))
                {
                    if (IsValidUtf8(nextItemKeyBytes))
                    {
                        string nextItemKey = Encoding.UTF8.GetString(nextItemKeyBytes.ToArray());
                        bool isValidSizeValue = (nextItemSizeValue > 0) && (nextItemSizeValue < ApeTag.MaxAllowedSize);
                        bool isValidNextItemKey = (nextItemKey.Length >= MinKeyLengthCharacters) && (nextItemKeyBytes.Count <= MaxKeyLengthBytes);
                        bool areValidFlags = AreValidFlags((int)nextItemFlags);

                        // Flags - Bit 28...3: Undefined, must be zero
                        if (isValidSizeValue && isValidNextItemKey && areValidFlags)
                        {
                            // Found the start of the next frame; calculate the amount of bytes between the last item and the next item.
                            return (stream.Position - startPosition) - (nextItemHeaderLengthRead + 1);
                        }
                    }
                }
                stream.Position = ++initialPosition;
                nextItemSizeValue = 0;
                nextItemHeaderLengthRead = 0;
                nextItemKeyLength = 0;
                nextItemFlags = 0;
                nextItemKeyBytes.Clear();
            }
            return -1;
        }
    }
}
