/*
 * Date: 2012-12-30
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System;
using System.ComponentModel;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing event data passed as argument to subscribed event handlers.
    /// </summary>
    public sealed class Id3v2FrameParseEventArgs : CancelEventArgs
    {
        private Id3v2Frame _frame;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2FrameParseEventArgs" /> class.
        /// </summary>
        public Id3v2FrameParseEventArgs(Id3v2Frame frame)
        {
            Frame = frame;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the frame.
        /// </summary>
        /// <value>
        /// The frame.
        /// </value>
        public Id3v2Frame Frame
        {
            get
            {
                return _frame;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _frame = value;
            }
        }
    }
}
