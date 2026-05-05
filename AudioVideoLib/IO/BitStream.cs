namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// Wrapper class around a stream to use as backing data with various ways to read and write.
/// </summary>
public sealed class BitStream : Stream
{
    private readonly StreamBuffer _sb;

    private int _bitPosition;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="BitStream"/> class
    /// with an expandable capacity initialized to 1024 bytes, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    public BitStream()
    {
        _sb = new StreamBuffer(1024);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitStream"/> class and sets the specified stream to use as backing data.
    /// </summary>
    /// <param name="stream">The stream to use as backing data.</param>
    public BitStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _sb = new StreamBuffer(stream);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitStream"/> class
    /// based on the specified byte array with an expandable capacity, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    public BitStream(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _sb = new StreamBuffer(buffer.Length);
        _sb.Write(buffer, 0, buffer.Length);
        _sb.Position -= buffer.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitStream"/> class
    /// with an expandable capacity initialized as specified, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    /// <param name="capacity">The initial size of the internal array in bytes.</param>
    public BitStream(int capacity)
    {
        _sb = new StreamBuffer(capacity);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the number of bytes within the stream.
    /// </summary>
    /// <returns>
    /// The number of bytes within the stream.
    /// </returns>
    public override long Length => _sb.Length;

    /// <summary>
    /// Gets or sets the current position within the stream, in bytes.
    /// </summary>
    /// <returns>
    /// The current position within the stream, in bytes.
    /// </returns>
    public override long Position
    {
        get => _sb.Position;
        set => _sb.Position = value;
    }

    /// <summary>
    /// Gets or sets the bit position within the current byte.
    /// </summary>
    /// <value>
    /// The bit position within the current byte.
    /// </value>
    /// <remarks>
    /// 0 is the left-most bit, while 7 the right-most.
    /// </remarks>
    public int BitPosition
    {
        get => _bitPosition;
        set
        {
            if (value > 7)
            {
                _bitPosition = value % 8;
                Position += value / 8;
            }
            else if (value < 0)
            {
                _bitPosition = 8 + (value % 8);
                Position += (value / 8) - 1;
            }
            else
            {
                _bitPosition = value;
            }
        }
    }

    /// <inheritdoc/>
    public override bool CanRead => _sb.CanRead;

    /// <inheritdoc/>
    public override bool CanWrite => _sb.CanWrite;

    /// <inheritdoc/>
    public override bool CanSeek => _sb.CanSeek;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => 0;

    /// <inheritdoc />
    public override int ReadByte() => _sb.ReadByte();

    /// <summary>
    /// Reads the bytes.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>A 32-bit signed integer.</returns>
    public int ReadBytes(int bytes) => bytes <= 0 ? 0 : _sb.ReadBigEndianInt(bytes);

    /// <summary>
    /// Reads a single bit (MSB-first within the current byte).
    /// </summary>
    /// <returns>The bit value, 0 or 1.</returns>
    public int ReadBit() => ReadInt32(1);

    /// <summary>
    /// Reads a unary-coded non-negative integer: counts leading zero bits up to and
    /// including the terminating 1 bit, returning the number of zeros consumed.
    /// </summary>
    /// <returns>The number of leading zero bits before the terminating 1.</returns>
    /// <remarks>
    /// MSB-first within the current byte. Bit-aligned (does not skip to a byte
    /// boundary). Used by RFC 9639 §11.25 (wasted-bits) and §11.30 (Rice MSBs).
    /// </remarks>
    public int ReadUnaryInt()
    {
        var count = 0;
        while (ReadInt32(1) == 0)
        {
            count++;

            // Defensive bound: a malformed stream of all-zero bits (or a stream that has
            // run out and returns 0 from ReadByte()) would otherwise loop forever. 64 bits
            // is far more than any FLAC-legal unary value needs.
            if (count > 64)
            {
                throw new InvalidDataException("Unary count exceeds reasonable bound (>64).");
            }
        }

        return count;
    }

    /// <summary>
    /// Advances the bit cursor to the next byte boundary, if not already aligned.
    /// </summary>
    /// <remarks>
    /// FLAC uses bit-packed subframe payloads that must be padded with zero bits
    /// up to the next byte boundary before the byte-aligned frame footer; this
    /// helper performs that alignment.
    /// </remarks>
    public void AlignToByteBoundary()
    {
        if (_bitPosition == 0)
        {
            return;
        }

        // Currently positioned on the byte that still has unread bits; advance past it.
        Position++;
        _bitPosition = 0;
    }

    /// <summary>
    /// Reads a signed integer of the specified width, sign-extending from the MSB.
    /// </summary>
    /// <param name="width">The width in bits (1..32).</param>
    /// <returns>A sign-extended 32-bit integer.</returns>
    public int ReadSignedInt32(int width)
    {
        if (width <= 0)
        {
            return 0;
        }

        var value = ReadInt32(width);
        if (width >= 32)
        {
            return value;
        }

        // Sign-extend: if the MSB of the width-bit value is set, OR-in the high bits.
        var signBit = 1 << (width - 1);
        return (value & signBit) != 0 ? value | (-1 << width) : value;
    }

    /// <summary>
    /// Reads a 32-bit signed integer.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <returns>A 32-bit signed integer.</returns>
    public int ReadInt32(int width)
    {
        if (width <= 0)
        {
            return 0;
        }

        // New theoretical bit index; may be 8 or more.
        var newBitIndex = _bitPosition + width;

        var value = newBitIndex switch
        {
            <= 8 => (long)(ReadByte() >> (8 - newBitIndex)),
            <= 16 => ReadBytes(2) >> (16 - newBitIndex),
            <= 24 => ReadBytes(3) >> (24 - newBitIndex),
            <= 32 => ReadBytes(4) >> (32 - newBitIndex),
            _ => ReadBytes(5) >> (40 - newBitIndex)
        };

        value &= 0xffffffffff >> (40 - width);

        // New bit position.
        _bitPosition = newBitIndex % 8;

        // If we haven't consumed all bits in the last read byte,
        // we need to go back to pointing to that last read byte.
        if (_bitPosition != 0)
        {
            Position--;
        }

        // Preserve the bit pattern for the full 32-bit case (e.g., a 32-bit signed sample
        // whose top bit is 1). Convert.ToInt32(long) range-checks and would throw on values
        // >= 2^31, but for fixed-width bit reads we want pure bit-pattern preservation.
        return unchecked((int)value);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin loc) => _sb.Seek(offset, loc);

    /// <summary>
    /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// </summary>
    public override void Flush() => _sb.Flush();

    /// <inheritdoc/>
    public override void SetLength(long value) => _sb.SetLength(value);
}
