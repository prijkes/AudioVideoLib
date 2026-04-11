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
    public sealed class AudioTagParseEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTagParseEventArgs" /> class.
        /// </summary>
        /// <param name="audioTagReader">The audio tag.</param>
        /// <param name="tagOrigin">The tag origin.</param>
        public AudioTagParseEventArgs(IAudioTagReader audioTagReader, TagOrigin tagOrigin)
        {
            AudioTagReader = audioTagReader;
            TagOrigin = tagOrigin;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the audio tag reader.
        /// </summary>
        /// <value>
        /// The audio tag reader.
        /// </value>
        public IAudioTagReader AudioTagReader { get; private set; }

        /// <summary>
        /// Gets the tag origin.
        /// </summary>
        /// <value>
        /// The tag origin.
        /// </value>
        public TagOrigin TagOrigin { get; private set; }
    }
}
