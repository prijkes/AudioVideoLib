namespace AudioVideoLib.Tags;

using System.Linq;

using AudioVideoLib.Collections;

/// <summary>
/// Collection for frames.
/// </summary>
/// <typeparam name="T">The type of frames in the collection.</typeparam>
public sealed class Id3v2FrameCollection<T> : NotifyingList<T> where T : Id3v2Frame
{
    /// <summary>
    /// Sorts this instance.
    /// </summary>
    public void Sort()
    {
        if (!this.Any())
        {
            return;
        }
    }
}
