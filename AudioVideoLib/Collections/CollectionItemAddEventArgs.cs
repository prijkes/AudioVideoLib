/*
 * Date: 2012-12-22
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
    public sealed class CollectionItemAddEventArgs<T> : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItemAddEventArgs&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public CollectionItemAddEventArgs(T item)
        {
            Item = item;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the item to be added.
        /// </summary>
        /// <value>
        /// The item to be added.
        /// </value>
        public T Item { get; set; }
    }
}
