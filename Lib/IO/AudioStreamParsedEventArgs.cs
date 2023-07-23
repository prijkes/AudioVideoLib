/*
 * Date: 2013-01-27
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System;

using AudioVideoLib.Formats;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    public sealed class AudioStreamParsedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreamParsedEventArgs"/> class.
        /// </summary>
        /// <param name="audioStream">The audio stream.</param>
        public AudioStreamParsedEventArgs(IAudioStream audioStream)
        {
            AudioStream = audioStream;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the audio stream.
        /// </summary>
        /// <value>
        /// The audio stream.
        /// </value>
        public IAudioStream AudioStream { get; private set; }
    }
}
