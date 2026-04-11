/*
 * Date: 2011-10-22
 * Sources used: 
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */
using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// The header for an <see cref="ApeItem"/>.
    /// </summary>
    public partial class ApeItem
    {
        private string _key;

        private int _flags;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the frame version of the frame.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public ApeVersion Version { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ApeItem"/> is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="ApeItem"/> is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get
            {
                return (Flags & HeaderFlags.ReadOnly) == 0;
            }

            set
            {
                if (value)
                    Flags |= HeaderFlags.ReadOnly;
                else
                    Flags &= ~HeaderFlags.ReadOnly;
            }
        }

        /// <summary>
        /// Gets the type of the item.
        /// </summary>
        /// <value>
        /// The type of the item.
        /// </value>
        /// <remarks>
        /// See <see cref="ApeItemType" /> for possible item types.
        /// </remarks>
        public virtual ApeItemType ItemType
        {
            get
            {
                return GetItemType(Flags);
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the key of this item.
        /// </summary>
        /// <value>
        /// The key of the item.
        /// </value>
        public string Key
        {
            get
            {
                return _key;
            }

            private set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length < MinKeyLengthCharacters)
                    throw new InvalidDataException(String.Format("value needs to be at least {0} characters long.", MinKeyLengthCharacters));

                _key = GetTruncatedEncodedString(value, MaxKeyLengthBytes);
            }
        }

        /// <summary>
        /// Gets or sets the flags of this frame.
        /// </summary>
        /// The flags value is stored as short (2 bytes) for all versions.
        private int Flags
        {
            get
            {
                return (_flags & ~HeaderFlags.ItemType) | (((int)ItemType) << 1);
            }

            set
            {
                _flags = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static string GetTruncatedEncodedString(string value, int maxBytesAllowed)
        {
            return StreamBuffer.GetTruncatedEncodedString(value, Encoding.UTF8, maxBytesAllowed);
        }
    }
}
