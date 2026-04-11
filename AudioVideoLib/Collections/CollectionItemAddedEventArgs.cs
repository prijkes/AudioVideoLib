/*
 * Date: 2012-12-25
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
    public sealed class CollectionItemAddedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItemAddedEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="item">The item which was added.</param>
        public CollectionItemAddedEventArgs(T item)
        {
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the item which was added.
        /// </summary>
        /// <value>
        /// The item which was added.
        /// </value>
        public T Item { get; private set; }
    }
}
