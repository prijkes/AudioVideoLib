/*
 * Date: 2010-12-07
 * Sources used: 
 */

using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;

namespace AudioVideoLib.IO
{
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

        private static readonly Encoding[] EncodingsWithPreambleList = Encoding.GetEncodings().Select(e => e.GetEncoding()).Where(e => e.GetPreamble().Length > 0).ToArray();

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
            if (stream == null)
                throw new ArgumentNullException("stream");

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
            if (buffer == null)
                throw new ArgumentNullException("buffer");

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
        public Stream BaseStream
        {
            get
            {
                return _isExternalStream ? _stream : null;
            }
        }

        /// <summary>
        /// Gets the number of bytes within the stream.
        /// </summary>
        /// <returns>
        /// The number of bytes within the stream.
        /// </returns>
        public override long Length
        {
            get { return _stream.Length; }
        }

        /// <inheritdoc />
        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Switches the endianness of the value.
        /// </summary>
        /// <param name="value">The value to switch its endianness.</param>
        /// <returns>The value, in switched endianness.</returns>
        public static short SwitchEndianness(short value)
        {
            return (short)SwitchEndianness(value, Int16Size);
        }

        /// <summary>
        /// Switches the endianness of the value.
        /// </summary>
        /// <param name="value">The value to switch its endianness.</param>
        /// <returns>The value, in switched endianness.</returns>
        public static int SwitchEndianness(int value)
        {
            return (int)SwitchEndianness(value, Int32Size);
        }

        /// <summary>
        /// Switches the endianness of the value.
        /// </summary>
        /// <param name="value">The value to switch its endianness.</param>
        /// <returns>The value, in switched endianness.</returns>
        public static long SwitchEndianness(long value)
        {
            return SwitchEndianness(value, Int64Size);
        }

        /// <summary>
        /// Switches the endianness of the value.
        /// </summary>
        /// <param name="value">The value to switch its endianness.</param>
        /// <param name="bytes">The amount of bytes of value to switch.</param>
        /// <returns>The value, in switched endianness.</returns>
        public static long SwitchEndianness(long value, int bytes)
        {
            if (bytes < 1)
                return value;

            if (bytes > Int64Size)
                bytes = Int64Size;

            long number = 0;
            for (int i = 0; i < bytes; i++)
            {
                byte bits = (byte)(bytes - 1 - i);
                number |= ((value >> (8 * i)) & 0xFF) << (8 * bits);
            }
            return number;
        }

        /// <summary>
        /// Switches the endianness of the value.
        /// </summary>
        /// <param name="bytes">An array of bytes to switch the endianness in.</param>
        /// <returns>The value, in switched endianness.</returns>
        public static void SwitchEndianness(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            Array.Reverse(bytes);
        }

        /// <summary>
        /// Determines whether the specified arrays are equal by comparing the elements.
        /// </summary>
        /// <param name="array1">The array1.</param>
        /// <param name="array2">The array2.</param>
        /// <returns>
        /// true if the specified arrays are equal; otherwise, false.
        /// </returns>
        /// This function is supposed to be FAST, according to the link below.
        /// http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net
        public static unsafe bool SequenceEqual(byte[] array1, byte[] array2)
        {
            if (ReferenceEquals(array2, array1))
                return true;

            if (ReferenceEquals(null, array1))
                return false;

            if ((array2 == null) || (array1.Length != array2.Length))
                return false;

            fixed (byte* pointer1 = array1, pointer2 = array2)
            {
                byte* bp1 = pointer1, bp2 = pointer2;
                int array1Length = array1.Length;

                // Check long
                for (int i = 0; i < array1Length / 8; i++, bp1 += 8, bp2 += 8)
                {
                    if (*((long*)bp1) != *((long*)bp2))
                        return false;
                }

                // Check int
                if ((array1Length & 4) != 0)
                {
                    if (*((int*)bp1) != *((int*)bp2))
                        return false;

                    bp1 += 4;
                    bp2 += 4;
                }

                // Check short
                if ((array1Length & 2) != 0)
                {
                    if (*((short*)bp1) != *((short*)bp2))
                        return false;

                    bp1 += 2;
                    bp2 += 2;
                }

                // Check byte
                return ((array1Length & 1) == 0) || (*bp1 == *bp2);
            }
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
        public static byte[] GetTruncatedEncodedBytes(string value, Encoding encoding, int maxBytesAllowed)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (encoding == null)
                throw new ArgumentNullException("encoding");

            if (maxBytesAllowed <= 0)
                return new byte[] { };

            value = value.Replace("\0", String.Empty);
            int subLength = value.Length;
            while ((subLength > 0) && (encoding.GetByteCount(value.Substring(0, subLength)) > maxBytesAllowed))
                subLength--;

            return encoding.GetBytes(value.Substring(0, subLength));
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
            if (value == null)
                throw new ArgumentNullException("value");

            if (encoding == null)
                throw new ArgumentNullException("encoding");

            if (maxBytesAllowed <= 0)
                return String.Empty;

            value = value.Replace("\0", String.Empty);
            int subLength = value.Length;
            while ((subLength > 0) && (encoding.GetByteCount(value.Substring(0, subLength)) > maxBytesAllowed))
                subLength--;

            return value.Substring(0, subLength);
        }


        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanTimeout
        {
            get
            {
                return _stream.CanTimeout;
            }
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            return _stream.CreateObjRef(requestedType);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
        }

        public override bool Equals(object obj)
        {
            return _stream.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        public override object InitializeLifetimeService()
        {
            return _stream.InitializeLifetimeService();
        }

        public override int ReadTimeout
        {
            get
            {
                return _stream.ReadTimeout;
            }

            set
            {
                _stream.ReadTimeout = value;
            }
        }

        public override string ToString()
        {
            return _stream.ToString();
        }

        public override int WriteTimeout
        {
            get
            {
                return _stream.WriteTimeout;
            }

            set
            {
                _stream.WriteTimeout = value;
            }
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
                _stream.Close();
        }

        /// <inheritdoc />
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the whole stream to a byte array; starting from the beginning of the stream.
        /// </summary>
        /// <returns>A byte array representing the whole stream.</returns>
        /// <remarks>The current <see cref="Position"/> is saved before reading the stream, and restored after having written the stream.</remarks>
        public byte[] ToByteArray()
        {
            long curPosition = _stream.Position;
            byte[] buffer = new byte[_stream.Length];
            _stream.Position = 0;
            Read(buffer, (int)_stream.Length);
            _stream.Position = curPosition;
            return buffer;
        }
    }
}
