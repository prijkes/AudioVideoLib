/*
 * Date: 2014-02-03
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public sealed partial class Id3v2TagReader
    {
        /// <summary>
        /// Occurs when parsing a frame.
        /// </summary>
        public event EventHandler<Id3v2FrameParseEventArgs> FrameParse;

        /// <summary>
        /// Occurs when a frame has been parsed.
        /// </summary>
        public event EventHandler<Id3v2FrameParsedEventArgs> FrameParsed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Raises the <see cref="FrameParse"/> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioVideoLib.Tags.Id3v2FrameParseEventArgs"/> instance containing the event data.</param>
        private void OnFrameParse(Id3v2FrameParseEventArgs e)
        {
            EventHandler<Id3v2FrameParseEventArgs> eventHandlers = FrameParse;
            if (eventHandlers == null)
                return;

            foreach (EventHandler<Id3v2FrameParseEventArgs> eventHandler in eventHandlers.GetInvocationList())
            {
                eventHandler(this, e);
                if (e.Cancel)
                    break;
            }
        }

        /// <summary>
        /// Raises the <see cref="FrameParsed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioVideoLib.Tags.Id3v2FrameParsedEventArgs"/> instance containing the event data.</param>
        private void OnFrameParsed(Id3v2FrameParsedEventArgs e)
        {
            EventHandler<Id3v2FrameParsedEventArgs> eventHandlers = FrameParsed;
            if (eventHandlers != null)
                eventHandlers(this, e);
        }
    }
}
