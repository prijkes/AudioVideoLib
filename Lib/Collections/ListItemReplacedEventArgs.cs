/*
 * Date: 2012-12-21
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
    /// <typeparam name="T">Type of the item the list will contain.</typeparam>
    public sealed class ListItemReplacedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemReplacedEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="index">The index the <paramref name="newItem"/> has replaced the <paramref name="oldItem"/>.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="newItem">The new item which has replaced the <paramref name="oldItem"/>.</param>
        public ListItemReplacedEventArgs(int index, T oldItem, T newItem)
        {
            Index = index;
            OldItem = oldItem;
            NewItem = newItem;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the index in the list the <see cref="NewItem"/> has replaced the <see cref="OldItem"/>.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the item which was replaced by the <see cref="NewItem"/>.
        /// </summary>
        public T OldItem { get; private set; }

        /// <summary>
        /// Gets the item which replaced the <see cref="OldItem"/>.
        /// </summary>
        public T NewItem { get; private set; }
    }
}
