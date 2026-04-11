/*
 * Date: 2013-01-06
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

namespace AudioVideoLib.Tags;

using System;
using System.ComponentModel;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
public sealed class Lyrics3v2FieldParseEventArgs : CancelEventArgs
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2FieldParseEventArgs" /> class.
    /// </summary>
    /// <param name="field">The field.</param>
    public Lyrics3v2FieldParseEventArgs(Lyrics3v2Field field)
    {
        Field = field;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the field.
    /// </summary>
    /// <value>
    /// The field.
    /// </value>
    public Lyrics3v2Field Field
    {
        get;

        set
        {
            field = value ?? throw new ArgumentNullException("value");
        }
    } = null!;
}
