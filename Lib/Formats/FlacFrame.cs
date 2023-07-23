/*
 * Date: 2013-02-02
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Cryptography;
using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public sealed partial class FlacFrame : IAudioFrame
    {
        /// <summary>
        /// The frame sync.
        /// </summary>
        private const int FrameSync = 0x7FFE;

        private readonly List<FlacSubFrame> _subFrames = new List<FlacSubFrame>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="FlacFrame"/> class. 
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if stream is null.
        /// </exception>
        private FlacFrame(IO.FlacStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            FlacStream = stream;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public long StartOffset { get; private set; }

        /// <inheritdoc/>
        public long EndOffset { get; private set; }

        /// <summary>
        /// Gets the FLAC stream.
        /// </summary>
        /// <value>
        /// The FLAC stream.
        /// </value>
        public IO.FlacStream FlacStream { get; private set; }

        /// <summary>
        /// Gets the subframes in the frame.
        /// </summary>
        /// <value>
        /// A list of <see cref="FlacSubFrame"/>s in the frame.
        /// </value>
        public IEnumerable<FlacSubFrame> SubFrames
        {
            get { return _subFrames.AsReadOnly(); }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads a <see cref="FlacFrame" /> from a <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="flacStream">The FLAC stream.</param>
        /// <returns>
        /// true if found; otherwise, null.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public static FlacFrame ReadFrame(Stream stream, IO.FlacStream flacStream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (flacStream == null)
                throw new ArgumentNullException("flacStream");

            FlacFrame frame = new FlacFrame(flacStream);
            return frame.ReadFrame(stream as StreamBuffer ?? new StreamBuffer(stream)) ? frame : null;
        }

        /// <summary>
        /// Returns the frame in a byte array.
        /// </summary>
        /// <returns>The frame in a byte array.</returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer sb = new StreamBuffer())
            {
                sb.WriteBigEndianInt32(_header);
                sb.Write(_sampleFrameNumberBytes);
                int blockSize = (_header >> 12) & 0xF;
                switch (blockSize)
                {
                    case 0x06:
                        sb.WriteByte((byte)(BlockSize - 1));
                        break;

                    case 0x07:
                        sb.WriteBigEndianInt16((short)(BlockSize - 1));
                        break;
                }
                int samplingRate = (_header >> 8) & 0xF;
                switch (samplingRate)
                {
                    case 0x0C:
                        sb.WriteByte((byte)(SamplingRate / 1000));
                        break;

                    case 0x0D:
                        sb.WriteBigEndianInt16((short)SamplingRate);
                        break;

                    case 0x0E:
                        sb.WriteBigEndianInt16((short)(SamplingRate / 10));
                        break;
                }
                sb.WriteByte((byte)_crc8);

                foreach (FlacSubFrame subFrame in SubFrames)
                    sb.Write(subFrame.ToByteArray());

                sb.WriteBigEndianInt16((short)_crc16);
                return sb.ToByteArray();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private bool ReadFrame(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            StartOffset = sb.Position;

            if (!ReadHeader(sb))
                return false;

            for (int channel = 0; channel < Channels; channel++)
            {
                FlacSubFrame subFrame = FlacSubFrame.ReadFrame(sb, channel, this);
                _subFrames.Add(subFrame);
            }

            _crc16 = sb.ReadBigEndianInt16();
            int crc16 = Crc16.Calculate(new byte[0]);
            if (_crc16 != crc16)
                throw new InvalidDataException("Corrupt CRC16.");

            EndOffset = sb.Position;
            return true;
        }
    }
}
