/*
 * Date: 2013-01-05
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System;
using System.ComponentModel;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    public sealed class ApeItemParseEventArgs : CancelEventArgs
    {
        private ApeItem _item;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeItemParseEventArgs" /> class.
        /// </summary>
        public ApeItemParseEventArgs(ApeItem item)
        {
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>
        /// The item.
        /// </value>
        public ApeItem Item
        {
            get
            {
                return _item;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _item = value;
            }
        }
    }
}
