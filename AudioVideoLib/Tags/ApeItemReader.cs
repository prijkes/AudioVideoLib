namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

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
    public static ApeItem? ReadFromStream(ApeVersion version, Stream stream, long maximumItemSize)
    {
        return stream == null
            ? throw new ArgumentNullException("stream")
            : ReadItem(version, stream as StreamBuffer ?? new StreamBuffer(stream), maximumItemSize);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static ApeItem? ReadItem(ApeVersion version, StreamBuffer sb, long maximumItemSize)
    {
        ArgumentNullException.ThrowIfNull(sb);

        // Find the item.
        var startPosition = sb.Position;
        var bytesToSkip = GetBytesUntilNextItem(sb, maximumItemSize);

        // Not found; return null.
        if (bytesToSkip == -1)
        {
            return null;
        }

        // Move the stream position to the start of the frame.
        sb.Position = startPosition + bytesToSkip;

        var valueSize = sb.ReadLittleEndianInt32();
        if (valueSize is <= 0 or > ApeTag.MaxAllowedSize)
        {
            return null;
        }

        var flags = sb.ReadLittleEndianInt32();
        maximumItemSize -= 8;

        var itemKeyLengthBytes = 0;
        var maximumBytesToRead = maximumItemSize;
        if (maximumBytesToRead > 0)
        {
            for (var y = 0; y < maximumBytesToRead; y++)
            {
                var character = sb.ReadByte();
                if (character == 0x00) // 0x00 byte - string terminator.
                {
                    break;
                }

                itemKeyLengthBytes++;
            }
        }

        // Name
        sb.Seek(-(itemKeyLengthBytes + 1), SeekOrigin.Current);
        var key = sb.ReadString(itemKeyLengthBytes, Encoding.UTF8);
        if ((key.Length < MinKeyLengthCharacters) || (itemKeyLengthBytes > MaxKeyLengthBytes))
        {
            return null;
        }

        // Skip Item key terminator (0x00 byte)
        var keyTerminator = sb.ReadByte();
        if (keyTerminator != 0x00)
        {
            return null;
        }

        var item = GetItem(version, key, flags);
        return item.ReadItem(valueSize, sb, maximumItemSize) ? item : null;
    }

    private bool ReadItem(int valueSize, StreamBuffer stream, long maximumItemSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Reject values whose declared size exceeds what the enclosing tag can contain;
        // an unchecked allocation here would turn a 4-byte malformed length into an OOM.
        if (valueSize < 0 || valueSize > maximumItemSize)
        {
            return false;
        }

        // Check if the item size indicated is really the size of the item.
        // Some files just aren't... properly written.
        var data = new byte[valueSize];
        var dataBytesRead = stream.Read(data, valueSize);
        var bytesLeftInStream = maximumItemSize - dataBytesRead;
        if ((valueSize > 0) && (bytesLeftInStream > 0) && (dataBytesRead >= valueSize))
        {
            var dataBuffer = new StreamBuffer(data);
            // See if there's a next item.
            // Try to find the start of the next item.
            var startPositionNextItem = stream.Position;
            var bytesUntilNextItem = GetBytesUntilNextItem(stream, maximumItemSize - valueSize);
            stream.Position = startPositionNextItem;

            // Seems that the size indicated by the item is not the total size of the item; read the extra bytes here.
            if (bytesUntilNextItem is > 0 and <= int.MaxValue)
            {
                data = new byte[bytesUntilNextItem];
                stream.Read(data, data.Length);
                dataBuffer.Write(data);
            }
            data = dataBuffer.ToByteArray();
        }
        Data = data;
        return true;
    }

    private static long GetBytesUntilNextItem(Stream stream, long maximumNextItemSize)
    {
        var startPosition = stream.Position;
        var initialPosition = startPosition;

        long nextItemSizeValue = 0;
        var nextItemKeyLength = 0;
        long nextItemFlags = 0;
        var nextItemHeaderLengthRead = 0;
        long totalBytesRead = 0;
        List<byte> nextItemKeyBytes = [];
        while (totalBytesRead++ < Math.Min(stream.Length, maximumNextItemSize))
        {
            var b = (byte)stream.ReadByte();
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
                    var nextItemKey = Encoding.UTF8.GetString([.. nextItemKeyBytes]);
                    var isValidSizeValue = nextItemSizeValue is > 0 and < ApeTag.MaxAllowedSize;
                    var isValidNextItemKey = (nextItemKey.Length >= MinKeyLengthCharacters) && (nextItemKeyBytes.Count <= MaxKeyLengthBytes);
                    var areValidFlags = AreValidFlags((int)nextItemFlags);

                    // Flags - Bit 28...3: Undefined, must be zero
                    if (isValidSizeValue && isValidNextItemKey && areValidFlags)
                    {
                        // Found the start of the next frame; calculate the amount of bytes between the last item and the next item.
                        return stream.Position - startPosition - (nextItemHeaderLengthRead + 1);
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
