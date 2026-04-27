namespace AudioVideoLib.IO;

using System;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Wrapper class around a stream to use as backing data with various ways to read and write.
/// </summary>
public sealed partial class StreamBuffer : Stream
{
    /// <summary>
    /// Size of the <see cref="byte"/> data type.
    /// </summary>
    public const int Int8Size = sizeof(byte);

    /// <summary>
    /// Size of the <see cref="short"/> data type.
    /// </summary>
    public const int Int16Size = sizeof(short);

    /// <summary>
    /// Size of the <see cref="int"/> data type.
    /// </summary>
    public const int Int32Size = sizeof(int);

    /// <summary>
    /// Size of the <see cref="long"/> data type.
    /// </summary>
    public const int Int64Size = sizeof(long);

    private const int MinBufferSize = 1024;

    private static readonly Encoding[] EncodingsWithPreambleList = [.. Encoding.GetEncodings().Select(e => e.GetEncoding()).Where(e => e.GetPreamble().Length > 0)];

    private readonly Stream _stream;

    private readonly bool _isExternalStream;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBuffer"/> class
    /// with an expandable capacity initialized to 1024 bytes, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    public StreamBuffer()
    {
        _stream = new MemoryStream(1024);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBuffer"/> class and sets the specified stream to use as backing data.
    /// </summary>
    /// <param name="stream">The stream to use as backing data.</param>
    /// <remarks>
    /// The <see cref="Close"/> function will not call the Close function on the <paramref name="stream"/> when called.
    /// </remarks>
    public StreamBuffer(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _isExternalStream = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBuffer"/> class
    /// based on the specified byte array with an expandable capacity, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <remarks>
    /// The position in the stream will be set to the beginning of the stream.
    /// </remarks>
    public StreamBuffer(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _stream = new MemoryStream(buffer.Length);
        _stream.Write(buffer, 0, buffer.Length);
        _stream.Position -= buffer.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBuffer"/> class
    /// with an expandable capacity initialized as specified, using <see cref="System.IO.MemoryStream"/> as backing data.
    /// </summary>
    /// <param name="capacity">The initial size of the internal array in bytes.</param>
    public StreamBuffer(int capacity)
    {
        _stream = new MemoryStream(capacity);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the underlying base stream, if set.
    /// </summary>
    /// <value>
    /// The base stream, if set.
    /// </value>
    /// <remarks>
    /// The underlying stream will only be returned if this instance has been constructed with <see cref="StreamBuffer(Stream)"/>.
    /// </remarks>
    public Stream? BaseStream => _isExternalStream ? _stream : null;

    /// <summary>
    /// Gets the number of bytes within the stream.
    /// </summary>
    /// <returns>
    /// The number of bytes within the stream.
    /// </returns>
    public override long Length => _stream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc/>
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc/>
    public override bool CanSeek => _stream.CanSeek;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Switches the endianness of the value.
    /// </summary>
    /// <param name="value">The value to switch its endianness.</param>
    /// <returns>The value, in switched endianness.</returns>
    public static short SwitchEndianness(short value) => (short)SwitchEndianness(value, Int16Size);

    /// <summary>
    /// Switches the endianness of the value.
    /// </summary>
    /// <param name="value">The value to switch its endianness.</param>
    /// <returns>The value, in switched endianness.</returns>
    public static int SwitchEndianness(int value) => (int)SwitchEndianness(value, Int32Size);

    /// <summary>
    /// Switches the endianness of the value.
    /// </summary>
    /// <param name="value">The value to switch its endianness.</param>
    /// <returns>The value, in switched endianness.</returns>
    public static long SwitchEndianness(long value) => SwitchEndianness(value, Int64Size);

    /// <summary>
    /// Switches the endianness of the value.
    /// </summary>
    /// <param name="value">The value to switch its endianness.</param>
    /// <param name="bytes">The amount of bytes of value to switch.</param>
    /// <returns>The value, in switched endianness.</returns>
    public static long SwitchEndianness(long value, int bytes)
    {
        if (bytes < 1)
        {
            return value;
        }

        if (bytes > Int64Size)
        {
            bytes = Int64Size;
        }

        long number = 0;
        for (var i = 0; i < bytes; i++)
        {
            var bits = (byte)(bytes - 1 - i);
            number |= ((value >> (8 * i)) & 0xFF) << (8 * bits);
        }
        return number;
    }

    /// <summary>
    /// Reverses the byte order of an array in place (e.g. for big-endian / little-endian conversion).
    /// </summary>
    /// <param name="bytes">An array of bytes to reverse.</param>
    public static void SwitchEndianness(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        Array.Reverse(bytes);
    }

    /// <summary>
    /// Reverses the byte order of a span in place (e.g. for big-endian / little-endian conversion).
    /// </summary>
    /// <param name="bytes">A span of bytes to reverse.</param>
    /// <remarks>
    /// Span-based overload: lets callers operate on a stack-allocated buffer or a slice of an
    /// existing array without allocating an intermediate <see cref="T:byte[]"/>.
    /// </remarks>
    public static void SwitchEndianness(Span<byte> bytes) => bytes.Reverse();

    /// <summary>
    /// Determines whether the specified arrays are equal by comparing the elements.
    /// </summary>
    /// <param name="array1">The array1.</param>
    /// <param name="array2">The array2.</param>
    /// <returns>
    /// true if the specified arrays are equal; otherwise, false.
    /// </returns>
    public static bool SequenceEqual(byte[] array1, byte[] array2) =>
        SequenceEqual((ReadOnlySpan<byte>)array1, (ReadOnlySpan<byte>)array2);

    /// <summary>
    /// Determines whether two byte spans are equal.
    /// </summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><c>true</c> if the spans are byte-for-byte equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Span-based overload: lets callers compare slices of existing buffers without
    /// allocating intermediate arrays.
    /// </remarks>
    public static bool SequenceEqual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.SequenceEqual(right);

    /// <summary>
    /// Gets an encoded string using no more than the specified allowed bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="maxBytesAllowed">The maximum bytes allowed.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">
    /// value
    /// or
    /// encoding
    /// </exception>
    public static byte[] GetTruncatedEncodedBytes(string value, Encoding encoding, int maxBytesAllowed)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);

        if (maxBytesAllowed <= 0)
        {
            return [];
        }

        value = value.Replace("\0", string.Empty);
        var subLength = value.Length;
        while (subLength > 0 && encoding.GetByteCount(value[..subLength]) > maxBytesAllowed)
        {
            subLength--;
        }

        return encoding.GetBytes(value[..subLength]);
    }

    /// <summary>
    /// Gets an encoded string using no more than the specified allowed bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="maxBytesAllowed">The maximum bytes allowed.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">
    /// value
    /// or
    /// encoding
    /// </exception>
    public static string GetTruncatedEncodedString(string value, Encoding encoding, int maxBytesAllowed)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);

        if (maxBytesAllowed <= 0)
        {
            return string.Empty;
        }

        value = value.Replace("\0", string.Empty);
        var subLength = value.Length;
        while (subLength > 0 && encoding.GetByteCount(value[..subLength]) > maxBytesAllowed)
        {
            subLength--;
        }

        return value[..subLength];
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

    /// <inheritdoc />
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        _stream.BeginRead(buffer, offset, count, callback, state);

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        _stream.BeginWrite(buffer, offset, count, callback, state);

    /// <inheritdoc />
    public override bool CanTimeout => _stream.CanTimeout;

    /// <inheritdoc />
    public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

    /// <inheritdoc />
    public override bool Equals(object? obj) => _stream.Equals(obj);

    /// <inheritdoc />
    public override int GetHashCode() => _stream.GetHashCode();

    /// <inheritdoc />
    public override int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    /// <inheritdoc />
    public override string? ToString() => _stream.ToString();

    /// <inheritdoc />
    public override int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    /// <summary>
    /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
    /// </summary>
    /// <remarks>
    /// The <see cref="Close"/> function will not call the Close function on the <see cref="BaseStream"/> when called.
    /// </remarks>
    public override void Close()
    {
        if (!_isExternalStream)
        {
            _stream.Close();
        }
    }

    /// <inheritdoc />
    public override void Flush() => _stream.Flush();

    /// <inheritdoc/>
    public override void SetLength(long value) => _stream.SetLength(value);

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Writes the whole stream to a byte array; starting from the beginning of the stream.
    /// </summary>
    /// <returns>A byte array representing the whole stream.</returns>
    /// <remarks>The current <see cref="Position"/> is saved before reading the stream, and restored after having written the stream.</remarks>
    public byte[] ToByteArray()
    {
        var curPosition = _stream.Position;
        var buffer = new byte[_stream.Length];
        _stream.Position = 0;
        Read(buffer, (int)_stream.Length);
        _stream.Position = curPosition;
        return buffer;
    }
}
