namespace AudioVideoLib.Collections;

using System.ComponentModel;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <typeparam name="T">Type of the item the list will contain.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ListItemReplaceEventArgs{T}"/> class.
/// </remarks>
/// <param name="index">The index the <paramref name="newItem" /> replaces the <paramref name="oldItem" />.</param>
/// <param name="oldItem">The old item.</param>
/// <param name="newItem">The new item to replace the <paramref name="oldItem" />.</param>
public sealed class ListItemReplaceEventArgs<T>(int index, T oldItem, T newItem) : CancelEventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the index in the list the <see cref="NewItem"/> replaces the <see cref="OldItem"/>.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// Gets the item which is to be replaced by the <see cref="NewItem"/>.
    /// </summary>
    public T OldItem { get; } = oldItem;

    /// <summary>
    /// Gets or sets the item to replace the <see cref="OldItem"/>.
    /// </summary>
    public T NewItem { get; set; } = newItem;
}
