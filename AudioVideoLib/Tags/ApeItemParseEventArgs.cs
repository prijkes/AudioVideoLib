namespace AudioVideoLib.Tags;

using System;
using System.ComponentModel;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
public sealed class ApeItemParseEventArgs : CancelEventArgs
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="ApeItemParseEventArgs" /> class.
    /// </summary>
    public ApeItemParseEventArgs(ApeItem item)
    {
        Item = item;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the item.
    /// </summary>
    /// <value>
    /// The item.
    /// </value>
    public ApeItem Item
    {
        get;

        set
        {
            field = value ?? throw new ArgumentNullException("value");
        }
    } = null!;
}
