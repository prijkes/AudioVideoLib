namespace AudioVideoLib.Collections;

using System.ComponentModel;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">Type of the item the list will contain.</typeparam>
public sealed class ListItemAddEventArgs<T> : CancelEventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemAddEventArgs{T}"/> class.
    /// </summary>
    /// <param name="item">The item to be added.</param>
    public ListItemAddEventArgs(T item)
    {
        Item = item;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemAddEventArgs{T}"/> class.
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
    public int Index { get; set; } = -1;

    /// <summary>
    /// Gets or sets the item to be added.
    /// </summary>
    /// <value>
    /// The item to be added to the list.
    /// </value>
    public T Item { get; set; }
}
