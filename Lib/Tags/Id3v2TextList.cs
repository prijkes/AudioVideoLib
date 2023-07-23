/*
 * Date: 2013-10-06
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Collection for <see cref="Id3v2TextFrame"/>s.
    /// </summary>
    public sealed class Id3v2TextList : EventList<string>
    {
    }
}
