/*
 * Date: 2013-02-17
 * Sources used: 
 *  http://www.xiph.org/vorbis/doc/v-comment.html
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store vorbis comments.
    /// </summary>
    public sealed class VorbisComments
    {
        private readonly EventList<VorbisComment> _comments = new EventList<VorbisComment>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VorbisComments"/> class.
        /// </summary>
        public VorbisComments()
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the vendor.
        /// </summary>
        /// <value>
        /// The vendor.
        /// </value>
        public string Vendor { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        /// <exception cref="System.ArgumentNullException">Thrown if value is null.</exception>
        public IList<VorbisComment> Comments
        {
            get
            {
                return _comments;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the <see cref="VorbisComments" /> from the stream.
        /// </summary>
        /// <param name="stream">The stream to read the <see cref="VorbisComments" /> from.</param>
        /// <returns>
        /// A <see cref="VorbisComments" /> instance if found; otherwise, null.
        /// <para />
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// stream can not be read
        /// or
        /// stream can not be seeked
        /// </exception>
        public static VorbisComments ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
                throw new InvalidOperationException("stream can not be read");

            if (!stream.CanSeek)
                throw new InvalidOperationException("stream can not be seeked");

            StreamBuffer sb = stream as StreamBuffer ?? new StreamBuffer(stream);

            long startPosition = sb.Position;

            VorbisComments vorbisComment = new VorbisComments();
            if (!vorbisComment.ReadStream(sb))
            {
                sb.Position = startPosition;
                return null;
            }
            return vorbisComment;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as VorbisComments);
        }

        /// <summary>
        /// Equals the specified <see cref="VorbisComments"/>.
        /// </summary>
        /// <param name="tag">The <see cref="VorbisComments"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(VorbisComments tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            return String.Equals(Vendor, tag.Vendor, StringComparison.OrdinalIgnoreCase) && Comments.SequenceEqual(tag.Comments);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Places the <see cref="VorbisComments"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="VorbisComments"/>.
        /// </returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buf = new StreamBuffer())
            {
                Vendor = Vendor ?? String.Empty;
                buf.WriteInt(Encoding.UTF8.GetByteCount(Vendor));
                buf.WriteString(Vendor, Encoding.UTF8);
                buf.WriteInt(Comments.Count);
                foreach (byte[] data in Comments.Where(c => c != null).Select(c => c.ToByteArray()))
                {
                    buf.WriteInt(data.Length);
                    buf.Write(data);
                }
                return buf.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Vorbis comment";
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private bool ReadStream(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            int length = sb.ReadInt32();
            Vendor = sb.ReadString(length);
            length = sb.ReadInt32();
            int commentsRead = 0;
            _comments.Clear();
            while ((commentsRead < length) && (sb.Position <= sb.Length))
            {
                _comments.Add(VorbisComment.ReadStream(sb));
                commentsRead++;
            }
            return true;
        }
    }
}
