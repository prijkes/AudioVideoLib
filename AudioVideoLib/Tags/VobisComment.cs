/*
 * Date: 2013-02-17
 * Sources used: 
 *  http://www.xiph.org/vorbis/doc/v-comment.html
 */
using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// A Vorbis comment is the second (of three) header packets that begin a Vorbis bit stream.
    /// It is meant for short, text comments, not arbitrary metadata; arbitrary metadata belongs in a separate logical bit stream (usually an XML stream type)
    /// that provides greater structure and machine parse ability.
    /// </summary>
    public class VorbisComment
    {
        private const char Delimiter = '=';

        private string _name;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        /// <remarks>
        /// A case-insensitive field name that may consist of ASCII 0x20 through 0x7D, 0x3D ('=') excluded.
        /// ASCII 0x41 through 0x5A inclusive (A-Z) is to be considered equivalent to ASCII 0x61 through 0x7A inclusive (a-z).
        /// </remarks>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidName(value))
                    throw new InvalidDataException("Value contains one or more invalid characters.");

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// A <see cref="VorbisComment"/> instance if a vorbis comment was found; otherwise, null.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public static VorbisComment ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            StreamBuffer sb = stream as StreamBuffer ?? new StreamBuffer(stream);
            int length = sb.ReadInt32();
            string[] s = sb.ReadString(length, Encoding.UTF8).Split(Delimiter);
            return s.Length >= 2
                       ? new VorbisComment { Name = s[0], Value = String.Join(String.Empty, s, 1, s.Length - 1) }
                       : null;
        }

        /// <summary>
        /// Places the <see cref="VorbisComment"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="VorbisComment"/>.
        /// </returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buf = new StreamBuffer())
            {
                string val = (Name ?? String.Empty) + Delimiter + Value;
                buf.WriteInt(Encoding.UTF8.GetByteCount(val));
                buf.WriteString(val, Encoding.UTF8);
                return buf.ToByteArray();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidName(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            return name.All(c => ((c >= 0x20) && (c <= 0x7D) && (c != 0x3D)));
        }
    }
}
