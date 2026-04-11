/*
 * Date: 2012-11-02
 * Sources used: 
 *  http://www.id3.org/lyrics3.html
 */

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3 tag.
    /// </summary>
    public sealed class Lyrics3Tag : IAudioTag
    {
        /// <summary>
        /// The header identifier for a <see cref="Lyrics3Tag"/>.
        /// </summary>
        public const string HeaderIdentifier = "LYRICSBEGIN";

        /// <summary>
        /// The footer identifier for a <see cref="Lyrics3Tag"/>.
        /// </summary>
        public const string FooterIdentifier = "LYRICSEND";

        /// <summary>
        /// The maximum size of the lyrics, in bytes.
        /// </summary>
        public const int MaxLyricsSize = 5100;

        private static readonly byte[] HeaderIdentifierBytes = Encoding.ASCII.GetBytes(HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = Encoding.ASCII.GetBytes(FooterIdentifier);

        private byte[] _lyrics;

        private Encoding _encoding = Encoding.Default;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> used to read and write text to a byte array.
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _encoding = value;
            }
        }

        /// <summary>
        /// Gets or sets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        /// <remarks>
        /// The keywords "<see cref="HeaderIdentifier">LYRICSBEGIN</see>" and "<see cref="FooterIdentifier">LYRICSEND</see>" may not be present in the lyrics.
        /// A byte in the lyrics must not have the binary value 255.
        /// The maximum length of the lyrics is 5100 bytes.
        /// Newlines are made with CR+LF sequence. 
        /// <para />
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds <see cref="MaxLyricsSize"/> bytes, 
        /// the value will be cut to the maximum character count which fits within <see cref="MaxLyricsSize"/> bytes.
        /// </remarks>
        public string Lyrics
        {
            get
            {
                return _lyrics != null ? _encoding.GetString(_lyrics) : null;
            }

            set
            {
                SetLyrics(value);
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Lyrics3Tag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as Lyrics3Tag);
        }

        /// <summary>
        /// Equals the specified <see cref="Lyrics3Tag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="Lyrics3Tag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Lyrics3Tag tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            return String.Equals(tag.Lyrics, Lyrics, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return 0;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                buffer.Write(HeaderIdentifierBytes);
                if (Lyrics != null)
                    buffer.WriteString(Lyrics, Encoding);

                buffer.Write(FooterIdentifierBytes);
                return buffer.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Lyrics3";
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Sets the lyrics.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.IO.InvalidDataException">
        /// A byte in the lyrics may not have the binary value 255
        /// </exception>
        /// <remarks>
        /// The keywords "<see cref="HeaderIdentifier">LYRICSBEGIN</see>" and "<see cref="FooterIdentifier">LYRICSEND</see>" may not be present in the lyrics.
        /// A byte in the lyrics must not have the binary value 255.
        /// <para />
        /// If encoding the value in the specified <see cref="Encoding"/> exceeds <see cref="MaxLyricsSize"/> bytes, 
        /// the value will be cut to the maximum character count which fits within <see cref="MaxLyricsSize"/> bytes.
        /// </remarks>
        private void SetLyrics(string value)
        {
            // The keywords "LYRICSBEGIN" and "LYRICSEND" must not be present in the lyrics.
            if ((value.Contains(HeaderIdentifier) || value.Contains(FooterIdentifier)))
                throw new InvalidDataException(String.Format("The lyrics may not contain the string {0} or {1}", HeaderIdentifier, FooterIdentifier));

            // A byte in the text must not have the binary value 255.
            _lyrics = StreamBuffer.GetTruncatedEncodedBytes(value, _encoding, MaxLyricsSize);

            if (_lyrics.Any(l => l == 0xFF))
                throw new InvalidDataException("A byte in the lyrics may not have the binary value 255");
        }
    }
}
