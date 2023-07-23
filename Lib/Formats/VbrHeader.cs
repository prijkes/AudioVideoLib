/*
 * Date: 2010-05-25
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 */
using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// VBRHeader base class for VBR headers. VBR classes MUST derive from this class.
    /// </summary>
    public abstract class VbrHeader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VbrHeader"/> class.
        /// </summary>
        /// <param name="firstFrame">The first frame.</param>
        /// <param name="firstFrameBuffer">The first frame buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="headerType">Type of the header.</param>
        protected VbrHeader(MpaFrame firstFrame, StreamBuffer firstFrameBuffer, long offset, VbrHeaderType headerType)
        {
            if (firstFrame == null)
                throw new ArgumentNullException("firstFrame");

            if (firstFrameBuffer == null)
                throw new ArgumentNullException("firstFrameBuffer");

            ////if (headerType == null)
            ////throw new ArgumentNullException("headerType");

            // first frame contains the vbr header
            FirstFrame = firstFrame;

            // Offset of this header in the first frame.
            Offset = offset;

            // VBR Header type, currently only XING and VBRI
            HeaderType = headerType;
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="VbrHeader"/> as it is found in the stream.
        /// </summary>
        /// <value>The name of the <see cref="VbrHeader"/>.</value>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the version of the <see cref="VbrHeader"/>.
        /// </summary>
        /// <value>The version of the <see cref="VbrHeader"/>.</value>
        public short Version { get; protected set; }

        /// <summary>
        /// Gets or sets the flags set in the header, if applied.
        /// </summary>
        /// <value>The flags of the <see cref="VbrHeader"/>.</value>
        public int Flags { get; protected set; }

        /// <summary>
        /// Gets or sets the VBR quality of the file. From 0 - best quality to 100 - worst quality.
        /// </summary>
        /// <value>The quality of the VBR audio in the file.</value>
        public int Quality { get; protected set; }

        /// <summary>
        /// Gets or sets the absolute offset of the <see cref="VbrHeader"/> in the firstFrame.
        /// </summary>
        /// <value>The absolute offset of the <see cref="VbrHeader"/> in the firstFrame.</value>
        public long Offset { get; protected set; }

        /// <summary>
        /// Gets or sets the frame count of the VBR header.
        /// </summary>
        /// <value>The frame count, as it's stored in the <see cref="VbrHeader"/> field.</value>
        /// <remarks>The frame count is stored as a field in the <see cref="VbrHeader"/>.
        /// For the <see cref="XingHeader"/>, the frame count excludes itself (frameCount - 1).</remarks>
        public int FrameCount { get; set; }

        /// <summary>
        /// Gets or sets the size of the file, in bytes, of the VBR header.
        /// </summary>
        /// <value>The size of the file in bytes, as it's stored in the <see cref="VbrHeader"/> field.</value>
        /// <remarks>The size only indicates the size of the audio (headers + data).
        /// This means that the size indicated could be less than the actual size of the file.</remarks>
        public int FileSize { get; set; }

        /// <summary>
        /// Gets or sets the type of the VBR header. See <see cref="VbrHeaderType"/> for possible types.
        /// </summary>
        /// <value>The VBR header type. See <see cref="VbrHeaderType"/> for possible types.</value>
        public VbrHeaderType HeaderType { get; protected set; }

        /// <summary>
        /// Gets or sets the LAME tag, if found.
        /// </summary>
        /// <value>The LAME tag in the firstFrame, if found.</value>
        public LameTag LameTag { get; protected set; }

        /// <summary>
        /// Gets or sets the frame which contains the VBR header.
        /// </summary>
        /// <value>
        /// The first frame.
        /// </value>
        protected MpaFrame FirstFrame { get; set; }

        /// <summary>
        /// Gets or sets the TOC table, used for seeking in the stream.
        /// </summary>
        /// <value>
        /// The TOC table.
        /// </value>
        protected int[] Toc { get; set; }

        /// <summary>
        /// Gets or sets the number of entries in the <see cref="Toc"/> table.
        /// </summary>
        /// <value>
        /// The number of table entries in the <see cref="Toc"/> table.
        /// </value>
        protected short TableEntries { get; set; }

        /// <summary>
        /// Gets or sets the size of a table entry, in bytes, in the <see cref="Toc"/> table.
        /// </summary>
        /// <value>
        /// The size of a table entry in the <see cref="Toc"/> table.
        /// </value>
        protected short TableEntrySize { get; set; }

        /// <summary>
        /// Gets or sets the total size of the <see cref="Toc"/> table, in bytes.
        /// </summary>
        /// <value>
        /// The total size of the <see cref="Toc"/> table.
        /// </value>
        protected int TableLength { get; set; }

        /// <summary>
        /// Gets or sets the table scale, in bytes, of the <see cref="Toc"/> table.
        /// </summary>
        /// <value>
        /// The table scale of the <see cref="Toc"/> table.
        /// </value>
        protected short TableScale { get; set; }

        /// <summary>
        /// Gets or sets the amount of frames in a <see cref="Toc"/> table entry.
        /// </summary>
        /// <value>
        /// The amount of frames per <see cref="Toc"/> table entry.
        /// </value>
        protected short FramesPerTableEntry { get; set; }

        /// <summary>
        /// Find a <see cref="VbrHeader"/> in the supplied frame.
        /// A <see cref="VbrHeader"/> could follow the MPA header in the first frame.
        /// </summary>
        /// <remarks>
        /// Only the first frame can contain a <see cref="VbrHeader"/>.
        /// Possible VBR headers could be <see cref="XingHeader"/> or <see cref="VbriHeader"/>.
        /// </remarks>
        /// <param name="firstFrame">The first frame.</param>
        /// <returns>A VBR header if found; otherwise null.</returns>
        public static VbrHeader FindHeader(MpaFrame firstFrame)
        {
            if (firstFrame == null)
                throw new ArgumentNullException("firstFrame");

            return XingHeader.FindHeader(firstFrame) ?? (VbrHeader)VbriHeader.FindHeader(firstFrame);
        }

        /// <summary>
        /// Seeks the position by time.
        /// </summary>
        /// <param name="entryTimeMilliseconds">The entry time in milliseconds used to seek the time.</param>
        /// <returns>A point in the file to decode in bytes that is nearest to the given time in milliseconds.</returns>
        public virtual int SeekPositionByTime(float entryTimeMilliseconds)
        {
            throw new NotSupportedException("VBRHeader does not support seeking position by milliseconds.");
        }

        /// <summary>
        /// Seeks the time by position.
        /// </summary>
        /// <param name="entryPointInBytes">The entry point in bytes used to seek for the time.</param>
        /// <returns>Returns a time in the file to decode in seconds that is nearest to a given point in bytes.</returns>
        public virtual float SeekTimeByPosition(int entryPointInBytes)
        {
            throw new NotSupportedException("VBRHeader does not support seeking time by position.");
        }

        /// <summary>
        /// Seeks the position by percentage.
        /// </summary>
        /// <param name="percentage">The percentage used to seek for the position.</param>
        /// <returns>
        /// Returns a point in the file to decode in bytes that is nearest to a given percentage of the time of the stream.
        /// </returns>
        /// <exception cref="System.NotSupportedException">VBRHeader does not support seeking position by percentage.</exception>
        public virtual long SeekPositionByPercent(float percentage)
        {
            throw new NotSupportedException("VBRHeader does not support seeking position by percentage.");
        }

        /// <summary>
        /// Places the <see cref="VbrHeader"/> into a byte array.
        /// </summary> 
        /// <returns>A byte array that represents the <see cref="VbrHeader"/> instance.</returns>
        public virtual byte[] ToByteArray()
        {
            throw new NotSupportedException("VBRHeader does not support writing its data to a byte array.");
        }
    }
}
