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
    public sealed class AudioTagParsedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTagParsedEventArgs" /> class.
        /// </summary>
        /// <param name="audioTagOffset">The audio tag.</param>
        public AudioTagParsedEventArgs(IAudioTagOffset audioTagOffset)
        {
            AudioTagOffset = audioTagOffset;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the audio tag offset.
        /// </summary>
        /// <value>
        /// The audio tag offset.
        /// </value>
        public IAudioTagOffset AudioTagOffset { get; private set; }
    }
}
