/*
 * Date: 2013-09-28
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
namespace AudioVideoLib.Collections;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">The type of items to contain.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="CollectionItemRemovedEventArgs&lt;T&gt;" /> class.
/// </remarks>
/// <param name="item">The item which was removed.</param>
public sealed class CollectionItemRemovedEventArgs<T>(T item) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the item which was removed.
    /// </summary>
    /// <value>
    /// The item which was removed.
    /// </value>
    public T Item { get; private set; } = item;
}
