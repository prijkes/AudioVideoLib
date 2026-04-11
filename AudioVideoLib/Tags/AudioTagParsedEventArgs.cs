/*
 * Date: 2012-12-30
 * Sources used:
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
 */
namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AudioTagParsedEventArgs" /> class.
/// </remarks>
/// <param name="audioTagOffset">The audio tag.</param>
public sealed class AudioTagParsedEventArgs(IAudioTagOffset audioTagOffset) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the audio tag offset.
    /// </summary>
    /// <value>
    /// The audio tag offset.
    /// </value>
    public IAudioTagOffset AudioTagOffset { get; private set; } = audioTagOffset;
}
