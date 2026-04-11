/*
 * Date: 2010-05-25
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.iis.fraunhofer.de/bf/amm/download/sw/index.jsp
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// VBRI VBR Header used to store VBR information.
    /// </summary>
    public sealed class VbriHeader : VbrHeader
    {
        /// <summary>
        /// The string indicating a VBRI header.
        /// </summary>
        public const string HeaderIndicator = "VBRI";

        // 32 bytes data indicating silence
        private const int SilenceDataSize = 32;

        // These values exist only in VBRI headers
        private readonly float _delay;

        private readonly long _totalLengthMilliseconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="VbriHeader"/> class.
        /// </summary>
        /// <param name="firstFrame">The first frame.</param>
        /// <param name="firstFrameBuffer">The first frame buffer.</param>
        /// <param name="offset">The offset.</param>
        public VbriHeader(MpaFrame firstFrame, StreamBuffer firstFrameBuffer, long offset) : base(firstFrame, firstFrameBuffer, offset, VbrHeaderType.Vbri)
        {
            /*
            FhG VBRI Header
            size    description
            4       'VBRI' (ID)
            2       version
            2       delay
            2       quality
            4       # bytes
            4       # frames
            2       table size (for TOC)
            2       table scale (for TOC)
            2       size of a table entry (max. size = 4 byte (must be stored in an integer))
            2       frames per table entry
            ??      dynamic table consisting out of frames with size 1-4 whole length in table size! (for TOC)
            */

            // name
            Name = firstFrameBuffer.ReadString(4);

            // version
            Version = (short)firstFrameBuffer.ReadBigEndianInt16();

            // delay
            _delay = firstFrameBuffer.ReadBigEndianInt16();

            // quality
            Quality = firstFrameBuffer.ReadBigEndianInt16();

            // size of the file, in bytes, of all the data
            FileSize = firstFrameBuffer.ReadBigEndianInt32();

            // amount of frames
            FrameCount =　firstFrameBuffer.ReadBigEndianInt32();

            // number of entries in the table (for TOC)
            TableEntries = (short)firstFrameBuffer.ReadBigEndianInt16();

            // table scale (for TOC)
            TableScale = (short)firstFrameBuffer.ReadBigEndianInt16();

            // size of a table entry (in bytes)
            TableEntrySize = (short)firstFrameBuffer.ReadBigEndianInt16();

            // frames per table entry
            FramesPerTableEntry = (short)firstFrameBuffer.ReadBigEndianInt16();

            // dynamic table consisting out of frames
            TableLength = TableEntries * TableEntrySize;
            Toc = new int[TableEntries + 1];
            for (int i = 0; i <= TableEntries; i++)
            {
                int value = firstFrameBuffer.ReadBigEndianInt(TableEntrySize);
                Toc[i] = value * TableScale;
            }
            _totalLengthMilliseconds = firstFrame.AudioLength * FrameCount;
        }

        /// <summary>
        /// Finds the <see cref="VbriHeader"/> header within the <paramref name="firstFrame"/>.
        /// </summary>
        /// <param name="firstFrame">The first frame.</param>
        /// <returns>The VBRI header if found; otherwise, null.</returns>
        /// <remarks>
        /// The VBRI header is located exactly 32 bytes after the end of the first MPEG audio header in the file.
        /// It will compare the first 4 bytes against the <see cref="HeaderIndicator"/> 
        /// to see if the header contains a <see cref="VbriHeader"/> or not.
        /// </remarks>
        public static new VbriHeader FindHeader(MpaFrame firstFrame)
        {
            if (firstFrame == null)
                throw new ArgumentNullException("firstFrame");

            using (StreamBuffer buffer = new StreamBuffer())
            {
                byte[] data = firstFrame.ToByteArray();
                buffer.Write(data);

                // 32 bytes = data indicating silence
                const long Offset = MpaFrame.FrameHeaderSize + SilenceDataSize;
                buffer.Seek(Offset, SeekOrigin.Begin);
                string tagName = buffer.ReadString(4, false, false);
                return String.Compare(tagName, HeaderIndicator, StringComparison.OrdinalIgnoreCase) == 0
                           ? new VbriHeader(firstFrame, buffer, Offset)
                           : null;
            }
        }

        /// <inheritdoc/>
        public override int SeekPositionByTime(float entryTimeMilliseconds)
        {
            float durationInMillisecondsPerTocEntry = (float)_totalLengthMilliseconds / (TableEntries + 1);

            if (entryTimeMilliseconds > _totalLengthMilliseconds)
                entryTimeMilliseconds = _totalLengthMilliseconds;

            int i = 0, seekPoint = 0;
            float accumaltedTimeMilliseconds = 0.0f;
            while (accumaltedTimeMilliseconds <= entryTimeMilliseconds)
            {
                seekPoint += Toc[i++];
                accumaltedTimeMilliseconds += durationInMillisecondsPerTocEntry;
            }

            // searched too far; correct result
            int fraction =
                Convert.ToInt32(
                    (((accumaltedTimeMilliseconds - entryTimeMilliseconds) / durationInMillisecondsPerTocEntry) +
                     (1.0f / (2.0f * FrameCount))) * FrameCount);
            seekPoint -= Convert.ToInt32((float)Toc[i - 1] * fraction / FrameCount);
            return seekPoint;
        }

        /// <inheritdoc/>
        public override float SeekTimeByPosition(int entryPointInBytes)
        {
            int i = 0, accumulatedBytes = 0;

            float seekTime = 0.0f;
            float totalLengthSeconds = _totalLengthMilliseconds / 1000.0f;
            float lengthSecPerTocEntry = totalLengthSeconds / (TableEntries + 1);

            while (accumulatedBytes <= entryPointInBytes)
            {
                accumulatedBytes += Toc[i++];
                seekTime += lengthSecPerTocEntry;
            }

            // searched too far; correct result
            int fraction = (int)((((accumulatedBytes - entryPointInBytes) / (float)Toc[i - 1]) + (1.0f / (2.0f * FrameCount))) * FrameCount);
            seekTime -= lengthSecPerTocEntry * ((float)fraction / FrameCount);
            return seekTime;
        }

        /// <inheritdoc/>
        public override long SeekPositionByPercent(float percentage)
        {
            if (percentage >= 100.0f)
                percentage = 100.0f;

            if (percentage <= 0.0f)
                percentage = 0.0f;

            return SeekPositionByTime((percentage / 100.0f) * _totalLengthMilliseconds);
        }

        /// <inheritdoc/>
        public override byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                buffer.WriteString(Name);
                buffer.WriteBigEndianInt16(Version);
                buffer.WriteBigEndianInt16((short)_delay);
                buffer.WriteBigEndianInt16((short)Quality);
                buffer.WriteBigEndianInt32(FileSize);
                buffer.WriteBigEndianInt32(FrameCount);
                buffer.WriteBigEndianInt16(TableEntries);
                buffer.WriteBigEndianInt16(TableScale);
                buffer.WriteBigEndianInt16(TableEntrySize);
                buffer.WriteBigEndianInt16(FramesPerTableEntry);
                for (int i = 0; i <= TableEntries; i++)
                    buffer.WriteBigEndianBytes(Toc[i] / TableScale, TableEntrySize);

                return buffer.ToByteArray();
            }
        }
    }
}
