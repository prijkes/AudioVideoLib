/*
 * Date: 2013-01-06
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    public sealed class Lyrics3v2FieldParsedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Lyrics3v2FieldParsedEventArgs" /> class.
        /// </summary>
        /// <param name="field">The field.</param>
        public Lyrics3v2FieldParsedEventArgs(Lyrics3v2Field field)
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
        public Lyrics3v2Field Field { get; set; }
    }
}
