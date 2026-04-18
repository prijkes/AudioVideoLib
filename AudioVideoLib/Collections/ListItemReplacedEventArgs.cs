namespace AudioVideoLib.Collections;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">Type of the item the list will contain.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ListItemReplacedEventArgs{T}"/> class.
/// </remarks>
/// <param name="index">The index the <paramref name="newItem"/> has replaced the <paramref name="oldItem"/>.</param>
/// <param name="oldItem">The old item.</param>
/// <param name="newItem">The new item which has replaced the <paramref name="oldItem"/>.</param>
public sealed class ListItemReplacedEventArgs<T>(int index, T oldItem, T newItem) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the index in the list the <see cref="NewItem"/> has replaced the <see cref="OldItem"/>.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// Gets the item which was replaced by the <see cref="NewItem"/>.
    /// </summary>
    public T OldItem { get; } = oldItem;

    /// <summary>
    /// Gets the item which replaced the <see cref="OldItem"/>.
    /// </summary>
    public T NewItem { get; } = newItem;
}
