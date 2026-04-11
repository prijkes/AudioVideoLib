/*
 * Date: 2013-09-28
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
namespace AudioVideoLib.Collections;

using System.ComponentModel;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">The type of items to contain.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="CollectionItemRemoveEventArgs&lt;T&gt;" /> class.
/// </remarks>
/// <param name="item">The item to be removed.</param>
public sealed class CollectionItemRemoveEventArgs<T>(T item) : CancelEventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the item to be removed.
    /// </summary>
    /// <value>
    /// The item to be removed.
    /// </value>
    public T Item { get; set; } = item;
}
