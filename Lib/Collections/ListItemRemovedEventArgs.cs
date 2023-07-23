/*
 * Date: 2013-09-28
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System;

namespace AudioVideoLib.Collections
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    /// <typeparam name="T">The type of items to contain.</typeparam>
    public sealed class ListItemRemovedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemRemovedEventArgs&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="index">The index of the item in the list which was removed.</param>
        /// <param name="item">The item which was removed from the list.</param>
        public ListItemRemovedEventArgs(int index, T item)
        {
            Index = index;
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the index of the item in the list to be removed.
        /// </summary>
        /// <value>
        /// The index of the item in the list to be removed.
        /// </value>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the item which was removed.
        /// </summary>
        /// <value>
        /// The item which was removed.
        /// </value>
        public T Item { get; private set; }
    }
}
