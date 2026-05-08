namespace AudioVideoLib.Collections;

using System;

/// <summary>
/// Extension helpers for <see cref="NotifyingList{T}"/>.
/// </summary>
public static class NotifyingListExtensions
{
    /// <summary>
    /// Standard ItemReplace handler for lists that carry an ItemAdd validator:
    /// cancels the in-place replace, removes the old item at <paramref name="e"/>'s
    /// <see cref="ListItemReplaceEventArgs{T}.Index"/>, and re-Adds
    /// <paramref name="e"/>'s <see cref="ListItemReplaceEventArgs{T}.NewItem"/> so
    /// the Add validator runs on it. The new item ends up at the tail of the list,
    /// not at the original index.
    /// </summary>
    public static void HandleReplaceAsRemoveAndAdd<T>(this NotifyingList<T> list, ListItemReplaceEventArgs<T> e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(e.NewItem);
        list.RemoveAt(e.Index);
        e.Cancel = true;
        list.Add(e.NewItem);
    }
}
