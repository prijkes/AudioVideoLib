/*
 * Date: 2013-09-28
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
    /// <typeparam name="T">The type of items to contain.</typeparam>
    public sealed class ListItemRemoveEventArgs<T> : CancelEventArgs
    {
        private int _index = -1;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemRemoveEventArgs&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="item">The item to be removed from the list.</param>
        public ListItemRemoveEventArgs(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemRemoveEventArgs&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="index">The index of the item in the list to be removed.</param>
        /// <param name="item">The item to be removed from the list.</param>
        public ListItemRemoveEventArgs(int index, T item)
        {
            Index = index;
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the index of the item in the list to be removed. The value -1 is used to indicate an unknown index.
        /// </summary>
        /// <value>
        /// The index of the item in the list to be removed.
        /// </value>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets the item to be removed from the list.
        /// </summary>
        /// <value>
        /// The item to be removed from the list.
        /// </value>
        public T Item { get; private set; }
    }
}
