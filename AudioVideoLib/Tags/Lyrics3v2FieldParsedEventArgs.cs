/*
 * Date: 2013-01-06
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
/// Initializes a new instance of the <see cref="Lyrics3v2FieldParsedEventArgs" /> class.
/// </remarks>
/// <param name="field">The field.</param>
public sealed class Lyrics3v2FieldParsedEventArgs(Lyrics3v2Field field) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the field.
    /// </summary>
    /// <value>
    /// The field.
    /// </value>
    public Lyrics3v2Field Field { get; set; } = field;
}
