/*
 * Date: 2013-10-16
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 *  http://www.mpx.cz/mp3manager/tags.htm
 */

using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 tag.
    /// </summary>
    public sealed partial class Lyrics3v2TagReader
    {
        /// <summary>
        /// Occurs when parsing a field.
        /// </summary>
        public event EventHandler<Lyrics3v2FieldParseEventArgs> FieldParse;

        /// <summary>
        /// Occurs when a field has been parsed.
        /// </summary>
        public event EventHandler<Lyrics3v2FieldParsedEventArgs> FieldParsed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Raises the <see cref="FieldParse"/> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioVideoLib.Tags.Lyrics3v2FieldParseEventArgs"/> instance containing the event data.</param>
        private void OnFieldParse(Lyrics3v2FieldParseEventArgs e)
        {
            EventHandler<Lyrics3v2FieldParseEventArgs> eventHandlers = FieldParse;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<Lyrics3v2FieldParseEventArgs> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Raises the <see cref="FieldParsed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioVideoLib.Tags.Lyrics3v2FieldParsedEventArgs"/> instance containing the event data.</param>
        private void OnFieldParsed(Lyrics3v2FieldParsedEventArgs e)
        {
            EventHandler<Lyrics3v2FieldParsedEventArgs> eventHandlers = FieldParsed;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }
    }
}
