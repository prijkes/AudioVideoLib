namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class to store an Id3v2 tag.
/// </summary>
public sealed partial class ApeTagReader
{
    /// <summary>
    /// Occurs when parsing an item.
    /// </summary>
    public event EventHandler<ApeItemParseEventArgs>? ItemParse;

    /// <summary>
    /// Occurs when an item has been parsed.
    /// </summary>
    public event EventHandler<ApeItemParsedEventArgs>? ItemParsed;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Raises the <see cref="ItemParse"/> event.
    /// </summary>
    /// <param name="e">The <see cref="AudioVideoLib.Tags.ApeItemParseEventArgs"/> instance containing the event data.</param>
    private void OnItemParse(ApeItemParseEventArgs e)
    {
        var eventHandlers = ItemParse;
        if (eventHandlers == null)
        {
            return;
        }

        foreach (EventHandler<ApeItemParseEventArgs> eventHandler in eventHandlers.GetInvocationList())
        {
            eventHandler(this, e);
            if (e.Cancel)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Raises the <see cref="ItemParsed"/> event.
    /// </summary>
    /// <param name="e">The <see cref="AudioVideoLib.Tags.ApeItemParsedEventArgs"/> instance containing the event data.</param>
    private void OnItemParsed(ApeItemParsedEventArgs e)
    {
        ItemParsed?.Invoke(this, e);
    }
}
