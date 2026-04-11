/*
 * Date: 2012-12-30
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
    public sealed class Id3v2FrameParsedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2FrameParsedEventArgs" /> class.
        /// </summary>
        /// <param name="frame">The frame.</param>
        public Id3v2FrameParsedEventArgs(Id3v2Frame frame)
        {
            Frame = frame;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the frame parsed.
        /// </summary>
        /// <value>
        /// The frame parsed.
        /// </value>
        public Id3v2Frame Frame { get; set; }
    }
}
