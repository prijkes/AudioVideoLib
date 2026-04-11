/*
 * Date: 2012-12-01
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
using System.Text;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeBinaryItem"/> item.
    /// </summary>
    public class ApeBinaryItem : ApeItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApeBinaryItem" /> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion" /> of the <see cref="ApeTag" />.</param>
        /// <param name="key">The key of the item.</param>
        /// <remarks>
        /// All characters in the key should be in the range of 0x20 to 0x7E, and may not be one of the following: ID3, TAG, OggS or MP+
        /// <para />
        /// If encoding the key in the <see cref="Encoding.UTF8" /> encoding exceeds 255 bytes,
        /// the key will be cut to the max character count which fits within 255 bytes.
        /// </remarks>
        public ApeBinaryItem(ApeVersion version, string key) : base(version, key)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeBinaryItem" /> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion" /> of the <see cref="ApeTag" />.</param>
        /// <param name="key">The key.</param>
        public ApeBinaryItem(ApeVersion version, ApeItemKey key) : base(version, key)
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the type of the item.
        /// </summary>
        /// <value>
        /// The type of the item.
        /// </value>
        /// <remarks>
        /// See <see cref="ApeItemType" /> for possible item types.
        /// </remarks>
        public override ApeItemType ItemType
        {
            get
            {
                return ApeItemType.ContainsBinary;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Equals the specified <see cref="ApeBinaryItem"/> instance with this one.
        /// </summary>
        /// <param name="item">The <see cref="ApeBinaryItem"/> instance.</param>
        /// <returns>True if both instances match; otherwise, false.</returns>
        public bool Equals(ApeBinaryItem item)
        {
            return base.Equals(item);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="data">The data.</param>
        public void SetData(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            Data = data;
        }
    }
}
