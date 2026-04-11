/*
 * Date: 2011-01-08
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeTag"/> item.
    /// </summary>
    public partial class ApeItem : IAudioTagFrame, IEquatable<ApeItem>
    {
        private const int MinKeyLengthCharacters = 2;

        private const int MaxKeyLengthBytes = 255;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeItem"/> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
        /// <param name="key">The key of the item.</param>
        /// <remarks>
        /// All characters in the key should be in the range of 0x20 to 0x7E, and may not be one of the following: ID3, TAG, OggS or MP+
        /// <para />
        /// If encoding the key in the <see cref="Encoding.UTF8"/> encoding exceeds 255 bytes, 
        /// the key will be cut to the max character count which fits within 255 bytes.
        /// </remarks>
        public ApeItem(ApeVersion version, string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (!IsValidItemKey(key))
                throw new InvalidDataException("key");

            Version = version;
            Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeItem"/> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
        /// <param name="key">The key.</param>
        public ApeItem(ApeVersion version, ApeItemKey key)
        {
            Version = version;
            Key = GetItemKeys(key).FirstOrDefault();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeItem"/> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
        protected ApeItem(ApeVersion version)
        {
            Version = version;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ApeItem"/> class from being created.
        /// </summary>
        private ApeItem()
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public virtual byte[] Data { get; protected set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the item keys as string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// The keys as string array for the specified <see cref="ApeItemKey"/>, or null if not found.
        /// </returns>
        public static IEnumerable<string> GetItemKeys(ApeItemKey key)
        {
            string[] itemKeys;
            return ItemKeys.TryGetValue(key, out itemKeys) ? itemKeys : Enumerable.Empty<string>();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ApeItem);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTagFrame audioFrame)
        {
            return Equals(audioFrame as ApeItem);
        }

        /// <summary>
        /// Equals the specified <see cref="ApeItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="ApeItem"/>.</param>
        /// <returns>
        /// true if equal; false otherwise.
        /// </returns>
        public virtual bool Equals(ApeItem item)
        {
            if (ReferenceEquals(null, item))
                return false;

            if (ReferenceEquals(this, item))
                return true;

            // Notes:
            // - APE Tags Item Key are case sensitive.
            // - Nevertheless it is forbidden to use APE Tags Item Key which only differs in case. 
            // - And nevertheless Tag readers are recommended to be case insensitive.
            // - Every Tag Item Key can only occur (at most) once. It is not possible to transmit a Tag Key multiple time to change it contents. 
            // - Tags can be partially or complete repeated in the streaming format.
            return (item.Version == Version) && String.Equals(item.Key, Key, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return (Version.GetHashCode() * 397) ^ (((Key != null) ? Key.GetHashCode() : 0) * 397);
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                byte[] data = Data;
                buffer.WriteInt(data != null ? data.Length : 0);
                buffer.WriteInt(Flags);
                buffer.WriteString(Key, Encoding.UTF8);
                buffer.WriteByte(0x00);
                if (data != null)
                    buffer.Write(data);

                return buffer.ToByteArray();
            }
        }
    }
}
