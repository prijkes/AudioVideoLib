/*
 * Date: 2012-11-25
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 *  http://msdn.microsoft.com/en-us/library/system.uri.iswellformeduristring.aspx
 */
using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeLocatorItem"/> item.
    /// </summary>
    public sealed class ApeLocatorItem : ApeUtf8Item
    {
        private readonly EventList<string> _values = new EventList<string>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeLocatorItem"/> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
        /// <param name="key">The name of the item.</param>
        public ApeLocatorItem(ApeVersion version, string key) : base(version, key)
        {
            BindEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeLocatorItem"/> class.
        /// </summary>
        /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
        /// <param name="key">The key.</param>
        public ApeLocatorItem(ApeVersion version, ApeItemKey key) : base(version, key)
        {
            BindEvents();
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
                return ApeItemType.IsLocator;
            }
        }

        /// <summary>
        /// Gets or sets a list of one or more locator values.
        /// </summary>
        /// <value>
        /// A list of one or more locator values.
        /// </value>
        /// <remarks>
        /// Only locator values in accordance with RFC 2396 and RFC 2732 are allowed.
        /// </remarks>
        /// http://msdn.microsoft.com/en-us/library/system.uri.iswellformeduristring.aspx
        public override IList<string> Values
        {
            get
            {
                return _values;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Equals the specified <see cref="ApeLocatorItem"/> instance with this one.
        /// </summary>
        /// <param name="item">The <see cref="ApeLocatorItem"/> instance.</param>
        /// <returns>True if both instances match; otherwise, false.</returns>
        public bool Equals(ApeLocatorItem item)
        {
            return Equals(item as ApeUtf8Item);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void BindEvents()
        {
            _values.ItemAdd += ItemAdd;

            _values.ItemReplace += ItemReplace;
        }

        private static void ItemAdd(object sender, ListItemAddEventArgs<string> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (!Uri.IsWellFormedUriString(e.Item, UriKind.RelativeOrAbsolute))
                throw new InvalidDataException("One or more URI values are invalid.");
        }

        private static void ItemReplace(object sender, ListItemReplaceEventArgs<string> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            if (!Uri.IsWellFormedUriString(e.NewItem, UriKind.RelativeOrAbsolute))
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
        }
    }
}
