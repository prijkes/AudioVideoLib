namespace AudioVideoLib.Formats;

using System;
using System.IO;

/// <summary>
/// A single element discovered while walking an EBML (Matroska / WebM) container.
/// </summary>
/// <param name="Id">The raw EBML element id, including its VINT marker bits.</param>
/// <param name="HeaderStartOffset">Absolute offset of the element id's first byte.</param>
/// <param name="PayloadStartOffset">Absolute offset where the element payload begins.</param>
/// <param name="PayloadEndOffset">Absolute offset immediately past the element payload.</param>
/// <param name="IsUnknownSize">
/// <c>true</c> when the element was encoded with the EBML "unknown size" sentinel
/// (all data bits 1 after the VINT marker bit).
/// </param>
public sealed record EbmlElement(
    long Id,
    long HeaderStartOffset,
    long PayloadStartOffset,
    long PayloadEndOffset,
    bool IsUnknownSize)
{
    /// <summary>
    /// Gets the element's payload size in bytes (independent of any padding around it).
    /// </summary>
    public long PayloadSize => PayloadEndOffset - PayloadStartOffset;

    /// <summary>
    /// Gets the element's total length in bytes, including its id and size VINTs.
    /// </summary>
    public long Size => PayloadEndOffset - HeaderStartOffset;

    /// <summary>
    /// Reads an EBML variable-length integer used for element ids — the marker bit is preserved.
    /// </summary>
    /// <param name="stream">The stream to read from. Position is advanced past the VINT on success.</param>
    /// <param name="length">The total VINT length in bytes (1..8).</param>
    /// <param name="value">The decoded id, including the marker bit.</param>
    /// <returns><c>true</c> if a well-formed VINT was decoded; <c>false</c> on EOF or invalid encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public static bool TryReadVintId(Stream stream, out int length, out long value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return TryReadVint(stream, out length, out value, stripMarker: false);
    }

    /// <summary>
    /// Reads an EBML variable-length integer used for sizes / unsigned ints — the marker bit is stripped.
    /// </summary>
    /// <param name="stream">The stream to read from. Position is advanced past the VINT on success.</param>
    /// <param name="length">The total VINT length in bytes (1..8).</param>
    /// <param name="value">The decoded numeric value, with the marker bit removed.</param>
    /// <param name="isUnknown">
    /// <c>true</c> when all data bits after the marker are 1 — the EBML "unknown size" sentinel.
    /// </param>
    /// <returns><c>true</c> if a well-formed VINT was decoded; <c>false</c> on EOF or invalid encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public static bool TryReadVintSize(Stream stream, out int length, out long value, out bool isUnknown)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!TryReadVint(stream, out length, out value, stripMarker: true))
        {
            isUnknown = false;
            return false;
        }

        // "Unknown size" sentinel: all data bits after the marker are 1.
        // For length L, the maximum representable value is (1 << (7*L)) - 1.
        var maxValue = length == 8 ? long.MaxValue : (1L << (7 * length)) - 1;
        isUnknown = value == maxValue;
        return true;
    }

    /// <summary>
    /// Encodes a non-negative integer as an EBML VINT using the smallest legal length (1..8 bytes).
    /// The marker bit is included in the output.
    /// </summary>
    /// <param name="value">The non-negative value to encode.</param>
    /// <returns>The VINT bytes ready to write to a stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative or too large for 8 bytes.</exception>
    public static byte[] EncodeVintSize(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "VINT size must be non-negative.");
        }

        for (var length = 1; length <= 8; length++)
        {
            // For length L the marker bit consumes 1 bit and the data is 7*L - 1 bits + 1 marker = 7*L bits total.
            // The maximum representable size value is (1 << (7*L)) - 1, but we reserve the all-ones value as
            // the "unknown size" sentinel, so a normal value can be at most ((1 << (7*L)) - 2).
            var maxValue = length == 8 ? long.MaxValue : (1L << (7 * length)) - 2;
            if (value <= maxValue)
            {
                return EncodeVint(value, length, includeMarker: true);
            }
        }

        throw new ArgumentOutOfRangeException(nameof(value), "VINT size does not fit in 8 bytes.");
    }

    /// <summary>
    /// Encodes a non-negative integer as an EBML VINT of the requested length (1..8 bytes), with marker bit.
    /// Useful when forcing a specific encoding length (e.g. for round-trip tests).
    /// </summary>
    /// <param name="value">The non-negative value to encode.</param>
    /// <param name="length">The total VINT length in bytes.</param>
    /// <returns>The VINT bytes ready to write to a stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when arguments are out of range.</exception>
    public static byte[] EncodeVintSize(long value, int length)
    {
        if (length is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "VINT length must be 1..8.");
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "VINT size must be non-negative.");
        }

        var maxValue = length == 8 ? long.MaxValue : (1L << (7 * length)) - 2;
        return value > maxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), "Value does not fit in the requested VINT length.")
            : EncodeVint(value, length, includeMarker: true);
    }

    /// <summary>
    /// Encodes a raw EBML element id (including its existing marker bit) as its on-disk VINT bytes.
    /// </summary>
    /// <param name="id">The element id, marker bit included (e.g. <c>0x1A45DFA3</c> for the EBML root).</param>
    /// <returns>The id bytes ready to write to a stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the id is invalid.</exception>
    public static byte[] EncodeId(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Element id must be positive.");
        }

        // The id length is determined by the position of the marker bit in the highest byte:
        // 1 byte → top bit set, 2 bytes → second-from-top of the high byte set, etc.
        for (var length = 1; length <= 8; length++)
        {
            // Highest byte for this length: shift right by (length-1)*8.
            var highByte = (id >> ((length - 1) * 8)) & 0xFF;
            // No bits should be set above this length.
            if ((id >> (length * 8)) != 0)
            {
                continue;
            }

            // Marker bit for length L is at bit position (8 - L) of the high byte.
            var markerMask = (byte)(0x80 >> (length - 1));
            if ((highByte & markerMask) == markerMask && (highByte & ~((markerMask << 1) - 1)) == 0)
            {
                var buf = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    buf[i] = (byte)((id >> ((length - 1 - i) * 8)) & 0xFF);
                }

                return buf;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(id), "Element id is not a valid EBML VINT.");
    }

    /// <summary>
    /// Encodes a non-negative unsigned integer payload using the minimum number of bytes (1..8).
    /// This is the EBML payload encoding for uint elements (e.g. <c>TargetTypeValue</c>).
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The big-endian payload bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public static byte[] EncodeUInt(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == 0)
        {
            return [0];
        }

        var length = 1;
        var shifted = value;
        while ((shifted >>= 8) != 0)
        {
            length++;
        }

        var buf = new byte[length];
        for (var i = 0; i < length; i++)
        {
            buf[i] = (byte)((value >> ((length - 1 - i) * 8)) & 0xFF);
        }

        return buf;
    }

    /// <summary>
    /// Decodes a big-endian unsigned integer payload (1..8 bytes) read from an EBML uint element.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <returns>The decoded value, or 0 for an empty payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    public static long DecodeUInt(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        long v = 0;
        var n = Math.Min(payload.Length, 8);
        for (var i = 0; i < n; i++)
        {
            v = (v << 8) | payload[i];
        }

        return v;
    }

    /// <summary>
    /// Decodes a big-endian IEEE 754 floating-point payload of length 4 or 8 bytes.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <returns>The decoded value, or 0 for unsupported lengths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    public static double DecodeFloat(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length == 4)
        {
            Span<byte> tmp = stackalloc byte[4];
            payload.AsSpan(0, 4).CopyTo(tmp);
            tmp.Reverse();
            return System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(tmp);
        }

        if (payload.Length == 8)
        {
            Span<byte> tmp = stackalloc byte[8];
            payload.AsSpan(0, 8).CopyTo(tmp);
            tmp.Reverse();
            return System.Buffers.Binary.BinaryPrimitives.ReadDoubleLittleEndian(tmp);
        }

        return 0;
    }

    private static bool TryReadVint(Stream stream, out int length, out long value, bool stripMarker)
    {
        length = 0;
        value = 0;

        var first = stream.ReadByte();
        if (first < 0)
        {
            return false;
        }

        if (first == 0)
        {
            // No marker bit anywhere in the first byte → invalid VINT (we don't extend past 8 bytes).
            return false;
        }

        // Count leading zero bits to determine total length (1..8).
        var len = 1;
        var mask = (byte)0x80;
        while ((first & mask) == 0 && len < 8)
        {
            len++;
            mask >>= 1;
        }

        // mask now points at the marker bit.
        var data = stripMarker ? (long)(first & (mask - 1)) : first;
        for (var i = 1; i < len; i++)
        {
            var b = stream.ReadByte();
            if (b < 0)
            {
                return false;
            }

            data = (data << 8) | (long)b;
        }

        length = len;
        value = data;
        return true;
    }

    private static byte[] EncodeVint(long value, int length, bool includeMarker)
    {
        var buf = new byte[length];
        for (var i = 0; i < length; i++)
        {
            buf[length - 1 - i] = (byte)((value >> (8 * i)) & 0xFF);
        }

        if (includeMarker)
        {
            // Marker bit position in the high byte for length L is (8 - L).
            buf[0] |= (byte)(0x80 >> (length - 1));
        }

        return buf;
    }
}
