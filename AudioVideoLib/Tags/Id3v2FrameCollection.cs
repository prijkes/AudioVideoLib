/*
 * Date: 2013-09-28
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using System.Linq;

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Collection for frames.
    /// </summary>
    /// <typeparam name="T">The type of frames in the collection.</typeparam>
    public sealed class Id3v2FrameCollection<T> : EventCollection<T> where T : Id3v2Frame
    {
        /// <summary>
        /// Sorts this instance.
        /// </summary>
        public void Sort()
        {
            if (!this.Any())
                return;

        }
    }
}
