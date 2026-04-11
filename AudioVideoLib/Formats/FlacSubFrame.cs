/*
 * Date: 2013-03-22
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio sub frames.
    /// </summary>
    public partial class FlacSubFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlacSubFrame"/> class.
        /// </summary>
        /// <param name="flacFrame">The FLAC frame.</param>
        protected FlacSubFrame(FlacFrame flacFrame)
        {
            if (flacFrame == null)
                throw new ArgumentNullException("flacFrame");

            FlacFrame = flacFrame;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the FLAC frame.
        /// </summary>
        /// <value>
        /// The FLAC frame.
        /// </value>
        public FlacFrame FlacFrame { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads a <see cref="FlacSubFrame" /> from a <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="flacFrame">The FLAC frame.</param>
        /// <returns>
        /// true if found; otherwise, null.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public static FlacSubFrame ReadFrame(Stream stream, int channel, FlacFrame flacFrame)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (flacFrame == null)
                throw new ArgumentNullException("flacFrame");

            return ReadSubFrame(stream as StreamBuffer ?? new StreamBuffer(stream), channel, flacFrame);
        }

        /// <summary>
        /// Returns the frame in a byte array.
        /// </summary>
        /// <returns>The frame in a byte array.</returns>
        public virtual byte[] ToByteArray()
        {
            using (StreamBuffer sb = new StreamBuffer())
            {
                sb.WriteBigEndianInt32(Header);
                sb.WriteUnaryInt(WastedBits);
                return sb.ToByteArray();
            }
        }

        /// <summary>
        /// Reads the specified stream buffer.
        /// </summary>
        /// <param name="sb">The stream buffer.</param>
        /// <param name="sampeSize">Size of the sample.</param>
        /// <param name="blockSize">Size of the block.</param>
        protected virtual void Read(StreamBuffer sb, int sampeSize, int blockSize)
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static FlacSubFrame ReadSubFrame(StreamBuffer sb, int channel, FlacFrame flacFrame)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            if (flacFrame == null)
                throw new ArgumentNullException("flacFrame");

            FlacSubFrame frame;
            int header = sb.ReadBigEndianInt32(false);
            int type = (header >> 1) & 0x7E;
            switch (type)
            {
                case 0x00:
                    frame = new FlacConstantSubFrame(flacFrame);
                    break;

                case 0x01:
                    frame = new FlacVerbatimSubFrame(flacFrame);
                    break;

                default:
                    if ((type >= 0x08) && (type <= 0x0C))
                        frame = new FlacFixedSubFrame(flacFrame);
                    else if (type >= 0x20)
                        frame = new FlacLinearPredictorSubFrame(flacFrame);
                    else
                        frame = new FlacSubFrame(flacFrame);
                    break;
            }
            frame.ReadSubFrame(sb, channel);
            return frame;
        }

        private void ReadSubFrame(StreamBuffer sb, int channel)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            int sampleSize = FlacFrame.SampleSize;
            if ((((FlacFrame.ChannelAssignment == FlacChannelAssignment.LeftSide) || (FlacFrame.ChannelAssignment == FlacChannelAssignment.MidSide)) && (channel == 1)) || ((FlacFrame.ChannelAssignment == FlacChannelAssignment.RightSide) && (channel == 0)))
                sampleSize++;

            ReadHeader(sb);
            SampleSize = sampleSize - WastedBits;
            ////Read(sb);
        }
    }
}
