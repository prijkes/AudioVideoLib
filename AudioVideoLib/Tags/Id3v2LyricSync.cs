/*
 * Date: 2011-08-22
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Structure for storing a sync in synchronized lyrics.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2LyricSync"/> class.
/// </remarks>
/// <param name="syllable">The syllable.</param>
/// <param name="timeStamp">The time stamp.</param>
public class Id3v2LyricSync(string syllable, int timeStamp)
{

    /// <summary>
    /// Gets the syllable.
    /// </summary>
    /// <value>
    /// The syllable.
    /// </value>
    public string Syllable { get; private set; } = syllable ?? throw new ArgumentNullException("syllable");

    /// <summary>
    /// Gets the time stamp.
    /// </summary>
    /// <value>
    /// The time stamp.
    /// </value>
    /// <remarks>
    /// The 'time stamp' is set to zero or the whole sync is omitted if located directly at the beginning of the sound.
    /// All time stamps should be sorted in chronological order.
    /// </remarks>
    public int TimeStamp { get; private set; } = timeStamp;
}
