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
    public sealed class ListItemAddedEventArgs<T> : EventArgs
    {
        private int _index = -1;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemAddedEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="item">The item which was added.</param>
        public ListItemAddedEventArgs(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemAddedEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="index">The index the <paramref name="item"/> was added at.</param>
        /// <param name="item">The item which was added.</param>
        public ListItemAddedEventArgs(int index, T item)
        {
            Index = index;
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the index in the list the <see cref="Item"/> was added at.
        /// </summary>
        public int Index
        {
            get { return _index; }
            private set { _index = value; }
        }

        /// <summary>
        /// Gets the item which was added.
        /// </summary>
        public T Item { get; private set; }
    }
}
