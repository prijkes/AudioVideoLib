/*
 * Date: 2013-01-05
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApeItemParsedEventArgs" /> class.
/// </remarks>
/// <param name="item">The item.</param>
public sealed class ApeItemParsedEventArgs(ApeItem item) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the item parsed.
    /// </summary>
    /// <value>
    /// The item parsed.
    /// </value>
    public ApeItem Item { get; set; } = item;
}
