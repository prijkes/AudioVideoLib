/*
 * Date: 2012-12-21
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System.ComponentModel;

namespace AudioVideoLib.Collections
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    /// <typeparam name="T">Type of the item the list will contain.</typeparam>
    public sealed class ListItemReplaceEventArgs<T> : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemReplaceEventArgs{T}"/> class.
        /// </summary>
        /// <param name="index">The index the <paramref name="oldItem" /> will be replaced at.</param>
        /// <param name="oldItem">The old item.</param>
        public ListItemReplaceEventArgs(int index, T oldItem)
        {
            Index = index;
            OldItem = oldItem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemReplaceEventArgs{T}"/> class.
        /// </summary>
        /// <param name="index">The index the <paramref name="newItem" /> replaces the <paramref name="oldItem" />.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="newItem">The new item to replace the <paramref name="oldItem" />.</param>
        public ListItemReplaceEventArgs(int index, T oldItem, T newItem)
        {
            Index = index;
            OldItem = oldItem;
            NewItem = newItem;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the index in the list the <see cref="NewItem"/> replaces the <see cref="OldItem"/>.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Gets or sets the item which is to be replaced by the <see cref="NewItem"/>.
        /// </summary>
        public T OldItem { get; private set; }

        /// <summary>
        /// Gets or sets the item to replace the <see cref="OldItem"/>.
        /// </summary>
        public T NewItem { get; set; }
    }
}
