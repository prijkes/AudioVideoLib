namespace AudioVideoLib.Tags;

using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Defines the offset for an <see cref="IAudioTag"/> in a <see cref="Stream"/>
/// </summary>
public interface IAudioTagOffset : IStreamOffset
{
    /// <summary>
    /// Gets the audio tag.
    /// </summary>
    /// <value>
    /// The audio tag.
    /// </value>
    IAudioTag AudioTag { get; }

    /// <summary>
    /// Gets the tag origin.
    /// </summary>
    /// <value>
    /// The tag origin.
    /// </value>
    TagOrigin TagOrigin { get; }
}
