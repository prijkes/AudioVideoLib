/*
 * Date: 2010-12-07
 * Sources used: 
 */

using System;
using System.Linq;
using System.Text;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Wrapper class around a stream to use as backing data with various ways to read and write.
    /// </summary>
    public sealed partial class StreamBuffer
    {
        /// <summary>
        /// Writes the buffer to the current stream.
        /// </summary>
        /// <param name="buffer">The buffer containing data to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            _stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a block of bytes to the current stream using data read from buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing data to write to the stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void Write(byte[] buffer, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            if (count > buffer.Length)
                throw new ArgumentOutOfRangeException("count", "count is > buffer.Length");

            _stream.Write(buffer, 0, count);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream 
        /// and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        /// <summary>
        /// Writes a short to the current position in the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <param name="value">The short value to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteShort(short value)
        {
            WriteBytes(value, Int16Size);
        }

        /// <summary>
        /// Writes bytes to the current position in the stream and advances the position within the stream by <paramref name="size"/> bytes.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        /// <param name="size">The size of <paramref name="value"/>, in bytes, to write to the stream.</param>
        /// <remarks>
        /// Use this function to write a specified number of bytes of a numeric value.
        /// If <paramref name="size"/> is bigger than eight bytes,  it will write eight bytes instead of the specified <paramref name="size"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBytes(long value, int size)
        {
            if (size < 1)
                return;

            if (size > Int64Size)
                size = Int64Size;

            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer, size);
        }

        /// <summary>
        /// Writes a 32-bit signed integer to the current position in the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="value">The 32-bit signed integer to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteInt(int value)
        {
            WriteBytes(value, Int32Size);
        }

        /// <summary>
        /// Writes a float to the current position in the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="value">The float value to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteFloat(float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer);
        }

        /// <summary>
        /// Writes a double-precision floating point number to the current position in the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="value">The double-precision floating point number to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteDouble(double value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer);
        }

        /// <summary>
        /// Writes a string to the buffer using the <see cref="Encoding.ASCII"/> encoding.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <remarks>
        /// This function does NOT write a string terminator character at the end if it's not appended to the string!
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            WriteString(value, Encoding.ASCII);
        }

        /// <summary>
        /// Writes the string to the buffer using the encoding supplied.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <param name="encoding">The encoding to encode the string as.</param>
        /// <remarks>
        /// This function does NOT write a string terminator character at the end if it's not appended to the string!
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteString(string value, Encoding encoding)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            byte[] buffer = encoding.GetBytes(value);
            Write(buffer);
        }

        /// <summary>
        /// Writes a truncated string to the buffer using the encoding provided.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <param name="encoding">The encoding to encode the string as.</param>
        /// <param name="maxBytesToWrite">The max amount of bytes allowed to write to the buffer.</param>
        /// <remarks>
        /// If the amount of bytes of the encoded string exceeds the <paramref name="maxBytesToWrite"/> parameter,
        /// the function will remove the last character of the string, and encode it again.
        /// It will do this as long as the amount of bytes of the encoded string exceeds the <paramref name="maxBytesToWrite"/> parameter.
        /// This is useful if you have a long string but are limited to the amount of bytes you can use.
        /// <para />
        /// This function does NOT write a string terminator character at the end if it's not appended to the string!
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteString(string value, Encoding encoding, int maxBytesToWrite)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            int subLength = value.Length;
            byte[] buffer = encoding.GetBytes(value);
            while ((buffer.Length > maxBytesToWrite) && (subLength > 0))
                buffer = encoding.GetBytes(value.Substring(0, --subLength));

            Write(buffer);
        }

        /// <summary>
        /// Writes a 16-bit signed integer as big endian to the current position in the stream and advances the position within the stream by 2 bytes.
        /// </summary>
        /// <param name="value">The value to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianBytes(short value)
        {
            WriteBigEndianBytes(value, Int16Size);
        }

        /// <summary>
        /// Writes a 32-bit signed integer as big endian to the current position in the stream and advances the position within the stream by 4 bytes.
        /// </summary>
        /// <param name="value">The value to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianBytes(int value)
        {
            WriteBigEndianBytes(value, Int32Size);
        }

        /// <summary>
        /// Writes a 64-bit signed integer as big endian to the current position in the stream and advances the position within the stream by 8 bytes.
        /// </summary>
        /// <param name="value">The value to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianBytes(long value)
        {
            WriteBigEndianBytes(value, Int64Size);
        }

        /// <summary>
        /// Writes a 64-bit signed integer as big endian to the current position in the stream 
        /// and advances the position within the stream by <paramref name="size"/> bytes.
        /// </summary>
        /// <param name="value">The value to write as big endian to the stream.</param>
        /// <param name="size">The size of <paramref name="value"/>, in bytes, to write to the stream.</param>
        /// <remarks>
        /// Use this function to write a specified number of bytes of a numeric value as big endian.
        /// If <paramref name="size"/> is bigger than eight bytes,  it will write eight bytes instead of the specified <paramref name="size"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianBytes(long value, int size)
        {
            if (size < 1)
                return;

            if (size > Int64Size)
                size = Int64Size;

            if (BitConverter.IsLittleEndian)
                value = SwitchEndianness(value, size);

            byte[] buffer = BitConverter.GetBytes(value).Take(size).ToArray();
            Write(buffer, size);
        }

        /// <summary>
        /// Writes a 16-bit signed integer as big endian to the current position in the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <param name="value">The 16-bit signed integer to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianInt16(short value)
        {
            if (BitConverter.IsLittleEndian)
                value = SwitchEndianness(value);

            byte[] buff = BitConverter.GetBytes(value);
            Write(buff);
        }

        /// <summary>
        /// Writes a 32-bit signed integer as big endian to the current position in the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="value">The 32-bit signed integer to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianInt32(int value)
        {
            if (BitConverter.IsLittleEndian)
                value = SwitchEndianness(value);

            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer);
        }

        /// <summary>
        /// Writes a 64-bit signed integer as big endian to the current position in the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="value">The 64-bit signed integer to write as big endian to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteBigEndianInt64(long value)
        {
            if (BitConverter.IsLittleEndian)
                value = SwitchEndianness(value);

            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer);
        }

        /// <summary>
        /// Writes a 32-bit signed integer as unary to the current position in the stream and advances the position by <paramref name="length" /> / 8 bytes.
        /// </summary>
        /// <param name="length">The amount of bits to write to the stream.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WriteUnaryInt(int length)
        {
            if (length == 0)
                return;

            int bytes = length / 8;
            int value = (1 << length) | 1;
            WriteBigEndianBytes(value, bytes);
        }

        /// <summary>
        /// Writes padding to the current position in the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="paddingByte">The byte to write as padding.</param>
        /// <param name="length">The amount of bytes to write as padding.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing, or the stream is already closed.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void WritePadding(byte paddingByte, int length)
        {
            for (int i = 0; i < length; i++)
                WriteByte(paddingByte);
        }
    }
}
