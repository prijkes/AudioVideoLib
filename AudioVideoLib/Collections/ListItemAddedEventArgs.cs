namespace AudioVideoLib.Collections;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">Type of the item the list will contain.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ListItemAddedEventArgs{T}"/> class.
/// </remarks>
/// <param name="index">The index the <paramref name="item"/> was added at.</param>
/// <param name="item">The item which was added.</param>
public sealed class ListItemAddedEventArgs<T>(int index, T item) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the index in the list the <see cref="Item"/> was added at.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// Gets the item which was added.
    /// </summary>
    public T Item { get; } = item;
}
