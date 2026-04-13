namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AudioTagParseEventArgs" /> class.
/// </remarks>
/// <param name="audioTagReader">The audio tag.</param>
/// <param name="tagOrigin">The tag origin.</param>
public sealed class AudioTagParseEventArgs(IAudioTagReader audioTagReader, TagOrigin tagOrigin) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the audio tag reader.
    /// </summary>
    /// <value>
    /// The audio tag reader.
    /// </value>
    public IAudioTagReader AudioTagReader { get; private set; } = audioTagReader;

    /// <summary>
    /// Gets the tag origin.
    /// </summary>
    /// <value>
    /// The tag origin.
    /// </value>
    public TagOrigin TagOrigin { get; private set; } = tagOrigin;
}
