/*
 * Date: 2013-03-09
 * Sources used: 
 *  http://py.thoulon.free.fr/
 */

using System;
using System.IO;

namespace AudioVideoLib.IO
{
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
            if (stream == null)
                throw new ArgumentNullException("stream");

            _sb = new StreamBuffer(stream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitStream"/> class 
        /// based on the specified byte array with an expandable capacity, using <see cref="System.IO.MemoryStream"/> as backing data.
        /// </summary>
        /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
        public BitStream(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

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
        public override long Length
        {
            get
            {
                return _sb.Length;
            }
        }

        /// <summary>
        /// Gets or sets the current position within the stream, in bytes.
        /// </summary>
        /// <returns>
        /// The current position within the stream, in bytes.
        /// </returns>
        public override long Position
        {
            get
            {
                return _sb.Position;
            }

            set
            {
                _sb.Position = value;
            }
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
            get
            {
                return _bitPosition;
            }

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
        public override bool CanRead
        {
            get
            {
                return _sb.CanRead;
            }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get
            {
                return _sb.CanWrite;
            }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get
            {
                return _sb.CanSeek;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            return _sb.ReadByte();
        }

        /// <summary>
        /// Reads the bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>A 32-bit signed integer.</returns>
        public int ReadBytes(int bytes)
        {
            if (bytes <= 0)
                return 0;

            return _sb.ReadBigEndianInt(bytes);
        }

        /// <summary>
        /// Reads a 32-bit signed integer.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <returns>A 32-bit signed integer.</returns>
        public int ReadInt32(int width)
        {
            if (width <= 0)
                return 0;

            // New theoretical bit index; may be 8 or more.
            int newBitIndex = _bitPosition + width;

            long value;
            if (newBitIndex <= 8)
                value = ReadByte() >> (8 - newBitIndex);
            else if (newBitIndex <= 16)
                value = ReadBytes(2) >> (16 - newBitIndex);
            else if (newBitIndex <= 24)
                value = ReadBytes(3) >> (24 - newBitIndex);
            else if (newBitIndex <= 32)
                value = ReadBytes(4) >> (32 - newBitIndex);
            else
                value = ReadBytes(5) >> (40 - newBitIndex);

            value &= 0xffffffffff >> (40 - width);

            // New bit position.
            _bitPosition = newBitIndex % 8;

            // If we haven't consumed all bits in the last read byte, 
            // we need to go back to pointing to that last read byte.
            if (_bitPosition != 0)
                Position--;

            return Convert.ToInt32(value);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param><param name="count">The number of bytes to be written to the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support writing. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin loc)
        {
            return _sb.Seek(offset, loc);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            _sb.Flush();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            _sb.SetLength(value);
        }
    }
}
