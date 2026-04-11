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
    public sealed class ListItemAddEventArgs<T> : CancelEventArgs
    {
        private int _index = -1;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemAddEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public ListItemAddEventArgs(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemAddEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="index">The index the <paramref name="item"/> will be added.</param>
        /// <param name="item">The item to be added.</param>
        public ListItemAddEventArgs(int index, T item)
        {
            Index = index;
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the index in the list the <see cref="Item" /> will be added at. The value -1 is used to add the item at the end of the list.
        /// </summary>
        /// <value>
        /// The index in the list the <see cref="Item"/> will be added at.
        /// </value>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets the item to be added.
        /// </summary>
        /// <value>
        /// The item to be added to the list.
        /// </value>
        public T Item { get; set; }
    }
}
