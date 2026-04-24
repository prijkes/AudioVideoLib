namespace AudioVideoLib.IO;

using System;

/// <summary>
/// Class for storing event data passed as argument to subscribed event handlers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaContainerParseEventArgs"/> class.
/// </remarks>
/// <param name="audioStream">The audio stream.</param>
public sealed class MediaContainerParseEventArgs(IMediaContainer audioStream) : EventArgs
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the audio stream.
    /// </summary>
    /// <value>
    /// The audio stream.
    /// </value>
    public IMediaContainer AudioStream { get; } = audioStream;
}
