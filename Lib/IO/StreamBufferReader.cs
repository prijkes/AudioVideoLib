/*
 * Date: 2010-12-07
 * Sources used: 
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Wrapper class around a stream to use as backing data with various ways to read and write.
    /// </summary>
    public sealed partial class StreamBuffer
    {
        /// <summary>
        /// Read specified amount of bytes from the stream, and stores it into the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the read data in.</param>
        /// <param name="count">The maximum number of bytes to be read from the stream.</param>
        /// <returns>
        /// The amount of bytes written into the buffer.
        /// This can be less than the number of bytes requested if that number of bytes are not currently available, 
        /// or zero if the end of the stream is reached before any bytes are read.
        /// </returns>
        public int Read(byte[] buffer, int count)
        {
            return Read(buffer, count, true);
        }

        /// <summary>
        /// Read specified amount of bytes from the stream, and stores it into the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the read data in.</param>
        /// <param name="count">The maximum number of bytes to be read from the stream.</param>
        /// <param name="movePosition">If set to <c>true</c>, moves the position in the stream by the amount of bytes read.</param>
        /// <returns>
        /// The amount of bytes written into the buffer.
        /// This can be less than the number of bytes requested if that number of bytes are not currently available, 
        /// or zero if the end of the stream is reached before any bytes are read.
        /// </returns>
        public int Read(byte[] buffer, int count, bool movePosition)
        {
            int bytesRead = Read(buffer, 0, count);
            if (!movePosition)
                Seek(-bytesRead, SeekOrigin.Current);

            return bytesRead;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Read specified amount of bytes from the stream, and stores it into the given buffer starting at offset.
        /// If buffer is null, it will just move the position in the stream with count bytes, or till the end of the stream
        /// when count is bigger than the length of the stream.
        /// </summary>
        /// <param name="buffer">The buffer to store the read data in.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the stream.</param>
        /// <param name="movePosition">If set to <c>true</c>, moves the position in the stream by the amount of bytes read.</param>
        /// <returns>
        /// The amount of bytes written into the buffer.
        /// This can be less than the number of bytes requested if that number of bytes are not currently available, 
        /// or zero if the end of the stream is reached before any bytes are read.
        /// </returns>
        public int Read(byte[] buffer, int offset, int count, bool movePosition)
        {
            int bytesRead;
            if (buffer == null)
            {
                long curPosition = Position;
                long newPosition = _stream.Seek(count, SeekOrigin.Current);
                bytesRead = Convert.ToInt32(newPosition - curPosition);
            }
            else
                bytesRead = _stream.Read(buffer, offset, count);

            if (!movePosition)
                Seek(-bytesRead, SeekOrigin.Current);

            return bytesRead;
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            return ReadByte(true);
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// The unsigned byte cast to an Int32, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadByte(out int bytesRead)
        {
            return ReadByte(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 8-bit signed integer from the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by one byte.</param>
        /// <returns>
        /// A 8-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadByte(bool movePosition)
        {
            int b = _stream.ReadByte();
            if (!movePosition)
                Seek(-1, SeekOrigin.Current);

            return b;
        }

        /// <summary>
        /// Reads a 8-bit signed integer from the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by one byte.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 8-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadByte(bool movePosition, out int bytesRead)
        {
            int b = _stream.ReadByte();
            if (!movePosition)
                Seek(-1, SeekOrigin.Current);

            bytesRead = (b == -1) ? 0: 1;
            return b;
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadInt16()
        {
            return ReadInt16(true);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadInt16(out int bytesRead)
        {
            return ReadInt16(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream and advances the position within the stream by two bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by two bytes.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadInt16(bool movePosition)
        {
            int bytesRead;
            return ReadInt16(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream and advances the position within the stream by two bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by two bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadInt16(bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[Int16Size];
            bytesRead = Read(buffer, Int16Size, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt32()
        {
            return ReadInt32(true);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt32(out int bytesRead)
        {
            return ReadInt32(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit integer from the stream and advances the position within the stream by four bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt32(bool movePosition)
        {
            int bytesRead;
            return ReadInt32(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit integer from the stream and advances the position within the stream by four bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt32(bool movePosition, out int bytesRead)
        {
            byte[] value = new byte[Int32Size];
            bytesRead = Read(value, Int32Size, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt(int length)
        {
            return ReadInt(length, true);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream by <paramref name="length" /> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt(int length, out int bytesRead)
        {
            return ReadInt(length, true, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream 
        /// by <paramref name="length"/> bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by <paramref name="length"/> bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt(int length, bool movePosition)
        {
            int bytesRead;
            return ReadInt(length, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream and advances the position within the stream
        /// by <paramref name="length" /> bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by <paramref name="length" /> bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadInt(int length, bool movePosition, out int bytesRead)
        {
            if ((length > Int32Size) || (length < 1))
                length = Int32Size;

            byte[] value = new byte[Int32Size];
            bytesRead = Read(value, length, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64()
        {
            return ReadInt64(true);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(int length)
        {
            return ReadInt64(length, true);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(out int bytesRead)
        {
            return ReadInt64(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(int length, out int bytesRead)
        {
            return ReadInt64(length, true, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by eight bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(bool movePosition)
        {
            int bytesRead;
            return ReadInt64(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(int length, bool movePosition)
        {
            int bytesRead;
            return ReadInt64(length, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by eight bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(bool movePosition, out int bytesRead)
        {
            return ReadInt64(Int64Size, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The amount of bytes to read from the stream.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadInt64(int length, bool movePosition, out int bytesRead)
        {
            if (length > Int64Size)
                length = Int64Size;

            byte[] value = new byte[Int64Size];
            bytesRead = Read(value, length, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToInt64(value, 0);
        }

        /// <summary>
        /// Reads a floating point number from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public float ReadFloat()
        {
            return ReadFloat(true);
        }

        /// <summary>
        /// Reads a floating point number from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public float ReadFloat(out int bytesRead)
        {
            return ReadFloat(true, out bytesRead);
        }

        /// <summary>
        /// Reads a floating point number from the stream and advances the position within the stream by four bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <returns>
        /// A floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public float ReadFloat(bool movePosition)
        {
            int bytesRead;
            return ReadFloat(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a floating point number from the stream and advances the position within the stream by four bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public float ReadFloat(bool movePosition, out int bytesRead)
        {
            byte[] value = new byte[Int32Size];
            bytesRead = Read(value, Int32Size, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToSingle(value, 0);
        }

        /// <summary>
        /// Reads a double-precision floating point number from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public double ReadDouble()
        {
            return ReadDouble(true);
        }

        /// <summary>
        /// Reads a double-precision floating point number from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A double-precision floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public double ReadDouble(out int bytesRead)
        {
            return ReadDouble(true, out bytesRead);
        }

        /// <summary>
        /// Reads a double-precision floating point number from the stream 
        /// and advances the position within the stream by eight bytes if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// A double-precision floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public double ReadDouble(bool movePosition)
        {
            int bytesRead;
            return ReadDouble(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a double-precision floating point number from the stream
        /// and advances the position within the stream by eight bytes if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A double-precision floating point number, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public double ReadDouble(bool movePosition, out int bytesRead)
        {
            byte[] value = new byte[Int64Size];
            bytesRead = Read(value, Int64Size, movePosition);
            if (bytesRead == 0)
                return -1;

            return BitConverter.ToDouble(value, 0);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII"/> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII"/> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// If there's a byte order marker at the start of the stream, it will be used to indicate the encoding to use.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes)
        {
            return ReadString(lengthBytes, false);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII" /> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII" /> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// If there's a byte order marker at the start of the stream, it will be used to indicate the encoding to use.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, out int bytesRead)
        {
            return ReadString(lengthBytes, false, out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII"/> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII"/> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, bool ignoreByteOrderMarker)
        {
            return ReadString(lengthBytes, ignoreByteOrderMarker, true);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII" /> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII" /> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, bool ignoreByteOrderMarker, out int bytesRead)
        {
            return ReadString(lengthBytes, ignoreByteOrderMarker, true,out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII"/> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use.</param>
        /// <param name="movePosition">if set to <c>true</c>; 
        /// advances the position in the stream by <paramref name="lengthBytes"/> bytes.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII"/> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, bool ignoreByteOrderMarker, bool movePosition)
        {
            return ReadString(lengthBytes, Encoding.ASCII, ignoreByteOrderMarker, movePosition);
        }

        /// <summary>
        /// Read amount of bytes from the stream as a string using the <see cref="Encoding.ASCII" /> encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use.</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by <paramref name="lengthBytes" /> bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <see cref="Encoding.ASCII" /> encoding.
        /// Returns an empty string if the end of file has been reached.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, bool ignoreByteOrderMarker, bool movePosition, out int bytesRead)
        {
            return ReadString(lengthBytes, Encoding.ASCII, ignoreByteOrderMarker, movePosition, out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <returns>A string from the stream.</returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// If there's a byte order marker at the start of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, Encoding encoding)
        {
            return ReadString(lengthBytes, encoding, false);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// If there's a byte order marker at the start of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        public string ReadString(int lengthBytes, Encoding encoding, out int bytesRead)
        {
            return ReadString(lengthBytes, encoding, false, out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <returns>A string from the stream.</returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(int lengthBytes, Encoding encoding, bool ignoreByteOrderMarker)
        {
            return ReadString(lengthBytes, encoding, ignoreByteOrderMarker, true);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// </remarks>
        public string ReadString(int lengthBytes, Encoding encoding, bool ignoreByteOrderMarker, out int bytesRead)
        {
            return ReadString(lengthBytes, encoding, ignoreByteOrderMarker, true, out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by <paramref name="lengthBytes" /> bytes.</param>
        /// <returns>
        /// A string from the stream.
        /// </returns>
        /// Read the comments below <see cref="ReadString(Encoding, char, bool)" /> why so much code for a simple function.
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// </remarks>
        public string ReadString(int lengthBytes, Encoding encoding, bool ignoreByteOrderMarker, bool movePosition)
        {
            int bytesRead;
            return ReadString(lengthBytes, encoding, ignoreByteOrderMarker, movePosition, out bytesRead);
        }

        /// <summary>
        /// Read amount of bytes from the stream as the specified encoding.
        /// </summary>
        /// <param name="lengthBytes">The amount of bytes to read.</param>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by <paramref name="lengthBytes" /> bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream.
        /// </returns>
        /// Read the comments below <see cref="ReadString(Encoding, char, bool)" /> why so much code for a simple function.
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// </remarks>
        public string ReadString(int lengthBytes, Encoding encoding, bool ignoreByteOrderMarker, bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[lengthBytes];
            bytesRead = Read(buffer, lengthBytes, movePosition);
            if (bytesRead == 0)
                return String.Empty;

            // Get preamble of the encoding
            byte[] encodingPreambleBytes = encoding.GetPreamble();
            int preambleLength = encodingPreambleBytes.Length;
            if (bytesRead < preambleLength)
            {
                preambleLength = 0;
            }
            else if (preambleLength > 0)
            {
                // If we have enough bytes for the preamble of the given encoding
                // See if we can match it
                byte[] preamble = encoding.GetPreamble();
                for (int i = 0; i < preambleLength; i++)
                {
                    if (preamble[i] != buffer[i])
                    {
                        preambleLength = 0;
                        break;
                    }
                }
            }

            // See if there's a preamble we know the encoding of
            if (preambleLength == 0)
            {
                // For each encoding...
                foreach (Encoding e in EncodingsWithPreambleList)
                {
                    // Get the preamble
                    encodingPreambleBytes = e.GetPreamble();

                    // See if it matches
                    if (SequenceEqual(encodingPreambleBytes, buffer))
                    {
                        // Set the preamble length
                        preambleLength = encodingPreambleBytes.Length;

                        // If we should not ignore the encoding
                        if (!ignoreByteOrderMarker)
                            encoding = e;

                        break;
                    }
                }
            }

            byte[] finalBuffer = buffer;

            // If there's a preamble
            if (preambleLength > 0)
            {
                int preambleBytesToRead = bytesRead - preambleLength;
                finalBuffer = new byte[preambleBytesToRead];
                Buffer.BlockCopy(buffer, preambleLength, finalBuffer, 0, preambleBytesToRead);
            }
            return encoding.GetString(finalBuffer);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string NULL-terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <returns>A string from the stream decoded using the <paramref name="encoding"/> encoding.</returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding)
        {
            return ReadString(encoding, false);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string NULL-terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        public string ReadString(Encoding encoding, out int bytesRead)
        {
            return ReadString(encoding, false, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string NULL-terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <returns>A string from the stream decoded using the <paramref name="encoding"/> encoding.</returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, bool ignoreByteOrderMarker)
        {
            return ReadString(encoding, ignoreByteOrderMarker, true);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, bool ignoreByteOrderMarker, out int bytesRead)
        {
            return ReadString(encoding, ignoreByteOrderMarker, true, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string NULL-terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by the amount of bytes read.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, bool ignoreByteOrderMarker, bool movePosition)
        {
            return ReadString(encoding, '\0', ignoreByteOrderMarker, movePosition);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by the amount of bytes read.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The string NULL-terminator is also consumed but not appended to the string.
        /// The string NULL-terminator is the '\0' character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, bool ignoreByteOrderMarker, bool movePosition, out int bytesRead)
        {
            return ReadString(encoding, '\0', ignoreByteOrderMarker, movePosition, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        public string ReadString(Encoding encoding, char customStringTerminator)
        {
            return ReadString(encoding, customStringTerminator, false);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        public string ReadString(Encoding encoding, char customStringTerminator, out int bytesRead)
        {
            return ReadString(encoding, customStringTerminator, false, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding"/> encoding.
        /// </returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, char customStringTerminator, bool ignoreByteOrderMarker)
        {
            return ReadString(encoding, customStringTerminator, ignoreByteOrderMarker, true);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, char customStringTerminator, bool ignoreByteOrderMarker, out int bytesRead)
        {
            return ReadString(encoding, customStringTerminator, ignoreByteOrderMarker, true, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by the amount of bytes read.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding"/> encoding.
        /// </returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, char customStringTerminator, bool ignoreByteOrderMarker, bool movePosition)
        {
            return ReadString(encoding, customStringTerminator.ToString(CultureInfo.InvariantCulture), ignoreByteOrderMarker, movePosition);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>;
        /// advances the position in the stream by the amount of bytes read.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, char customStringTerminator, bool ignoreByteOrderMarker, bool movePosition, out int bytesRead)
        {
            return ReadString(encoding, customStringTerminator.ToString(CultureInfo.InvariantCulture), ignoreByteOrderMarker, movePosition, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding"/> encoding.
        /// </returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, string customStringTerminator)
        {
            return ReadString(encoding, customStringTerminator, false);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// If there's a byte order marker at the current position of the stream it will be used to indicate the encoding to use (overriding the given encoding).
        /// </remarks>
        public string ReadString(Encoding encoding, string customStringTerminator, out int bytesRead)
        {
            return ReadString(encoding, customStringTerminator, false, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding"/> encoding.
        /// </returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, string customStringTerminator, bool ignoreByteOrderMarker)
        {
            return ReadString(encoding, customStringTerminator, ignoreByteOrderMarker, true);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        public string ReadString(Encoding encoding, string customStringTerminator, bool ignoreByteOrderMarker, out int bytesRead)
        {
            return ReadString(encoding, customStringTerminator, ignoreByteOrderMarker, true, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position; 
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>; advances the position in the stream by the amount of bytes read.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding"/> encoding.
        /// </returns>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// The custom string terminator is the <paramref name="customStringTerminator"/> character encoded with the <paramref name="encoding"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string ReadString(Encoding encoding, string customStringTerminator, bool ignoreByteOrderMarker, bool movePosition)
        {
            int bytesRead;
            return ReadString(encoding, customStringTerminator, ignoreByteOrderMarker, movePosition, out bytesRead);
        }

        /// <summary>
        /// Read a string from the stream in the specified encoding until the custom string terminator is read or the end of file is reached.
        /// </summary>
        /// <param name="encoding">The encoding used to read the bytes as.</param>
        /// <param name="customStringTerminator">The custom text terminator.</param>
        /// <param name="ignoreByteOrderMarker">if set to <c>true</c> ignores the byte order marker if found in the stream at the current position;
        /// otherwise, uses it to determine the encoding to use (overriding the given encoding).</param>
        /// <param name="movePosition">if set to <c>true</c>; advances the position in the stream by the amount of bytes read.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A string from the stream decoded using the <paramref name="encoding" /> encoding.
        /// </returns>
        /// A whole lot of stuff here. Reason is that StreamReader() does detects and uses the byte order marker if found, but it 'eats' the position.
        /// Because it 'eats' the position, we lose track of it, as it reads till the end of the stream / internal buffer when it can.
        /// See http://www.daniweb.com/software-development/csharp/threads/35078 for the problem and a simple StreamReader class example.
        /// <para />
        /// The GetString() functions of the encoding classes on the other side also 'consumes' the byte order marker, but it tells the string it returns
        /// to include the byte order marker in the instance. Result: comparing the same string with and without a byte order marker leads to false results.
        /// See http://stackoverflow.com/questions/2915182/how-do-i-ignore-the-utf-8-byte-order-marker-in-string-comparisons
        /// <para />
        /// If you can refactor this without losing functionality, please do so. This needs to be fast, not clean.
        /// <exception cref="System.ArgumentNullException">encoding</exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// Returns an empty string if the end of file has been reached.
        /// The custom string terminator is also consumed but not appended to the string.
        /// </remarks>
        public string ReadString(Encoding encoding, string customStringTerminator, bool ignoreByteOrderMarker, bool movePosition, out int bytesRead)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            long startPositionStream = Position;
            if (startPositionStream >= Length)
            {
                bytesRead = 0;
                return String.Empty;
            }

            byte[] byteOrderMarkerBuffer = new byte[8];
            bytesRead = Read(byteOrderMarkerBuffer, byteOrderMarkerBuffer.Length);
            if (bytesRead == 0)
                return String.Empty;

            // See if the possible preamble matches that of the encoding.
            byte[] preamble = encoding.GetPreamble();
            int preambleLength = ((preamble.Length > 0) && (bytesRead >= preamble.Length)) ? preamble.Length : 0;
            if (preambleLength > 0)
            {
                for (int i = 0; i < preambleLength; i++)
                {
                    if (preamble[i] == byteOrderMarkerBuffer[i])
                        continue;

                    preambleLength = 0;
                    break;
                }
            }

            // Else we must guess the encoding - i.e. check if the first few bytes are a preamble.
            if (preambleLength == 0)
            {
                foreach (Encoding e in EncodingsWithPreambleList)
                {
                    byte[] encodingPreamble = e.GetPreamble();
                    if (SequenceEqual(encodingPreamble, byteOrderMarkerBuffer))
                    {
                        preambleLength = encodingPreamble.Length;
                        if (!ignoreByteOrderMarker)
                            encoding = e;

                        break;
                    }
                }
            }

            // Get the maximum amount of bytes possible for one char.
            int maxByteCount = encoding.GetMaxByteCount(1);

            // Calculate the bufferSize - make this fit exactly.
            int remainder = MinBufferSize % maxByteCount;
            remainder = (remainder == 0) ? maxByteCount : remainder;
            int bufferSize = (MinBufferSize - remainder) * remainder;

            // Skip the preamble.
            byte[] buffer = new byte[bufferSize];
            Buffer.BlockCopy(byteOrderMarkerBuffer, preambleLength, buffer, 0, byteOrderMarkerBuffer.Length - preambleLength);
            bytesRead -= preambleLength;

            // Get the decoder to store data in
            Decoder decoder = encoding.GetDecoder();

            // Character buffer
            char[] charBuffer = new char[bufferSize];

            // String buffer
            StringBuilder sb = new StringBuilder(bufferSize);

            // Parse the data, byte by byte.
            long totalBytesRead = bytesRead, totalCharsRead = 0;
            while (bytesRead > 0)
            {
                // Read as much characters as possible from the buffer.
                int charsRead = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0);

                // Loop through each character
                for (int z = 0, m = 0; z < charsRead; z++, m = 0, totalCharsRead++)
                {
                    // If we have enough characters to check for the string terminator
                    if ((z + customStringTerminator.Length) <= charsRead)
                    {
                        // While the current character matches the current character in the custom string terminator
                        while ((m < customStringTerminator.Length) && ((m + z) < charsRead) && (charBuffer[z + m] == customStringTerminator[m]))
                            m++;
                    }

                    // If the characters read match the custom string terminator
                    if (m == customStringTerminator.Length)
                    {
                        // Add the amount of characters read (which is equal to customStringTerminator.Length) to the total amount of characters read
                        totalCharsRead += m;

                        // Subtract the amount of bytes read into the buffer from the total amount of bytes read
                        totalBytesRead -= bytesRead;

                        // Add the amount of bytes until after the string terminator to the total amount of bytes read
                        totalBytesRead += encoding.GetByteCount(charBuffer, 0, z + m);

                        // We're done looping
                        charsRead = 0;
                        break;
                    }

                    // Add the current character to the StringBuilder.
                    sb.Append(charBuffer[z]);
                }

                // If we've found the string terminator
                if (charsRead == 0)
                    break;

                // Save the amount of bytes read in the buffer
                bytesRead = Read(buffer, buffer.Length);

                // Ad the amount of bytes read in the buffer to the total amount of bytes read
                totalBytesRead += bytesRead;
            }

            // Move the position if required by the amount of bytes read, starting at the position we started at
            Position = movePosition ? startPositionStream + preambleLength + totalBytesRead : startPositionStream;

            // Set the correct amount of bytes read
            bytesRead = (int)totalBytesRead;

            // Return the string found
            return sb.ToString();
        }

        /// <summary>
        /// Reads a 16-bit signed integer as little endian from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadLittleEndianInt16()
        {
            return ReadLittleEndianInt16(true);
        }

        /// <summary>
        /// Reads a 16-bit signed integer as little endian from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadLittleEndianInt16(out int bytesRead)
        {
            return ReadLittleEndianInt16(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer as little endian from the stream and advances the position within the stream by two bytes 
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadLittleEndianInt16(bool movePosition)
        {
            int bytesRead;
            return ReadLittleEndianInt16(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer as little endian from the stream and advances the position within the stream by two bytes
        /// if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public short ReadLittleEndianInt16(bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[Int16Size];
            bytesRead = Read(buffer, Int16Size);
            if (bytesRead == 0)
                return -1;

            int number = 0;
            for (int i = 0; i < bytesRead; i++)
                number |= buffer[i] << (8 * i);

            return (short)number;
        }

        /// <summary>
        /// Reads a 32-bit signed integer as little endian from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadLittleEndianInt32()
        {
            return ReadLittleEndianInt32(true);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as little endian from the stream and advances the position within the stream by four bytes 
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadLittleEndianInt32(bool movePosition)
        {
            int bytesRead;
            return ReadLittleEndianInt32(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as little endian from the stream and advances the position within the stream by four bytes
        /// if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadLittleEndianInt32(bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[Int32Size];
            bytesRead = Read(buffer, Int32Size, movePosition);
            if (bytesRead == 0)
                return -1;

            int number = 0;
            for (int i = 0; i < bytesRead; i++)
                number |= buffer[i] << (8 * i);

            return number;
        }

        /// <summary>
        /// Reads a 64-bit signed integer as little endian from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadLittleEndianInt64()
        {
            return ReadLittleEndianInt64(true);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as little endian from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadLittleEndianInt64(out int bytesRead)
        {
            return ReadLittleEndianInt64(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as little endian from the stream and advances the position within the stream by eight bytes 
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadLittleEndianInt64(bool movePosition)
        {
            int bytesRead;
            return ReadLittleEndianInt64(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as little endian from the stream and advances the position within the stream by eight bytes
        /// if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadLittleEndianInt64(bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[Int64Size];
            bytesRead = Read(buffer, Int64Size, movePosition);
            if (bytesRead == 0)
                return -1;

            long number = 0;
            for (int i = 0; i < bytesRead; i++)
                number |= ((long)buffer[i]) << (8 * i);

            return number;
        }

        /// <summary>
        /// Reads a 16-bit signed integer as big endian from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt16()
        {
            return ReadBigEndianInt16(true);
        }

        /// <summary>
        /// Reads a 16-bit signed integer as big endian from the stream and advances the position within the stream by two bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt16(out int bytesRead)
        {
            return ReadBigEndianInt16(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer big endian from the stream and advances the position within the stream by two bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by two bytes.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt16(bool movePosition)
        {
            int bytesRead;
            return ReadBigEndianInt16(movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 16-bit signed integer big endian from the stream and advances the position within the stream by two bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by two bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 16-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt16(bool movePosition, out int bytesRead)
        {
            byte[] buffer = new byte[Int16Size];
            bytesRead = Read(buffer, Int16Size, movePosition);
            if (bytesRead == 0)
                return -1;

            int number = 0;
            for (int i = 0; i < bytesRead; i++)
            {
                int bits = bytesRead - 1 - i;
                number |= buffer[i] << (8 * bits);
            }
            return number;
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt32()
        {
            return ReadBigEndianInt32(true);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by four bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt32(out int bytesRead)
        {
            return ReadBigEndianInt32(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by four bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt32(bool movePosition)
        {
            return ReadBigEndianInt(Int32Size, movePosition);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by four bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt32(bool movePosition, out int bytesRead)
        {
            return ReadBigEndianInt(Int32Size, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The number of bytes to read; this can not be more than four bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than four bytes, four bytes will be used as <paramref name="length"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt(int length)
        {
            return ReadBigEndianInt(length, true);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The number of bytes to read; this can not be more than four bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than four bytes, four bytes will be used as <paramref name="length"/>.
        /// </remarks>
        public int ReadBigEndianInt(int length, out int bytesRead)
        {
            return ReadBigEndianInt(length, true, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length" /> bytes.
        /// if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="length">The number of bytes to read, can not be more than <see cref="Int32Size" />.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by <paramref name="length"/> bytes.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than four bytes, four bytes will be used as <paramref name="length"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadBigEndianInt(int length, bool movePosition)
        {
            int bytesRead;
            return ReadBigEndianInt(length, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="length">The number of bytes to read, can not be more than <see cref="Int32Size"/>.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by <paramref name="length"/> bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than four bytes, four bytes will be used as <paramref name="length"/>.
        /// </remarks>
        public int ReadBigEndianInt(int length, bool movePosition, out int bytesRead)
        {
            if (length > Int32Size)
                length = Int32Size;

            byte[] buffer = new byte[length];
            bytesRead = Read(buffer, length, movePosition);
            if (bytesRead == 0)
                return -1;

            int number = 0;
            for (int i = 0; i < bytesRead; i++)
            {
                int bits = bytesRead - 1 - i;
                number |= buffer[i] << (8 * bits);
            }
            return number;
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadBigEndianInt64()
        {
            return ReadBigEndianInt64(true);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by eight bytes.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadBigEndianInt64(out int bytesRead)
        {
            return ReadBigEndianInt64(true, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by eight bytes
        /// if <paramref name="movePosition" /> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadBigEndianInt64(bool movePosition)
        {
            return ReadBigEndianInt64(Int64Size, movePosition);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by eight bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadBigEndianInt64(bool movePosition, out int bytesRead)
        {
            return ReadBigEndianInt64(Int64Size, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The number of bytes to read; this can not be more than eight bytes.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than eight bytes, eight bytes will be used as <paramref name="length"/>.
        /// </remarks>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public long ReadBigEndianInt64(int length)
        {
            return ReadBigEndianInt64(length, true);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The number of bytes to read; this can not be more than eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than eight bytes, eight bytes will be used as <paramref name="length"/>.
        /// </remarks>
        public long ReadBigEndianInt64(int length, out int bytesRead)
        {
            return ReadBigEndianInt64(length, true, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="length">The number of bytes to read, can not be more than eight bytes.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <returns>
        /// a 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than eight bytes, eight bytes will be used as <paramref name="length"/>.
        /// </remarks>
        public long ReadBigEndianInt64(int length, bool movePosition)
        {
            int bytesRead;
            return ReadBigEndianInt64(length, movePosition, out bytesRead);
        }

        /// <summary>
        /// Reads a 64-bit signed integer as big endian from the stream and advances the position within the stream by <paramref name="length"/> bytes
        /// if <paramref name="movePosition"/> is set to true.
        /// </summary>
        /// <param name="length">The number of bytes to read, can not be more than eight bytes.</param>
        /// <param name="movePosition">if set to <c>true</c>, advances the position within the stream by eight bytes.</param>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// a 64-bit signed integer, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <remarks>
        /// If <paramref name="length"/> is bigger than eight bytes, eight bytes will be used as <paramref name="length"/>.
        /// </remarks>
        public long ReadBigEndianInt64(int length, bool movePosition, out int bytesRead)
        {
            if (length > Int64Size)
                length = Int64Size;

            byte[] buffer = new byte[length];
            bytesRead = Read(buffer, length, movePosition);
            if (bytesRead == 0)
                return -1;

            long number = 0;
            for (int i = 0; i < bytesRead; i++)
            {
                int bits = bytesRead - 1 - i;
                number |= ((long)buffer[i]) << (8 * bits);
            }
            return number;
        }

        /// <summary>
        /// Reads a 32-bit signed integer as unary.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadUnaryInt()
        {
            int bytesRead;
            return ReadUnaryInt(out bytesRead);
        }

        /// <summary>
        /// Reads a 32-bit signed integer as unary.
        /// </summary>
        /// <param name="bytesRead">The amount of bytes read from the stream.</param>
        /// <returns>
        /// A 32-bit signed integer.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int ReadUnaryInt(out int bytesRead)
        {
            int val = 0;
            int mask = 0x80;
            int b = ReadByte();
            bytesRead = 1;
            while (true)
            {
                if ((b & mask) == mask)
                    return val;

                val++;
                if (mask == 0x01)
                {
                    b = ReadByte();
                    bytesRead++;
                    mask = 0x80;
                }
                else
                    mask >>= 1;
            }
        }
    }
}
