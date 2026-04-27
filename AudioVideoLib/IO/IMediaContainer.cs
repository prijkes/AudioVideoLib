namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// Interface for an audio format to implement streaming.
/// </summary>
/// <remarks>
/// The canonical serialisation primitive is <see cref="WriteTo(Stream)"/>. Buffer-shaped
/// helpers (<c>ToByteArray</c>, <c>GetSerializedSize</c>, <c>TryWriteTo</c>,
/// <c>WriteTo(IBufferWriter&lt;byte&gt;)</c>) are provided as extension methods on
/// <see cref="IMediaContainerExtensions"/>; concrete types may override them with their own
/// instance method for a faster direct path.
/// </remarks>
public interface IMediaContainer
{
    /// <summary>
    /// Gets the start offset of the <see cref="IMediaContainer"/>, where it starts in the stream.
    /// </summary>
    /// <value>
    /// The start offset of the <see cref="IMediaContainer"/>, counting from the start of the stream.
    /// </value>
    long StartOffset { get; }

    /// <summary>
    /// Gets the end offset of the <see cref="IMediaContainer"/>, where it ends in the stream.
    /// </summary>
    /// <value>
    /// The end offset of the <see cref="IMediaContainer"/>, counting from the start of the stream.
    /// </value>
    long EndOffset { get; }

    /// <summary>
    /// Gets the total length of audio in milliseconds.
    /// </summary>
    /// <value>
    /// The total length of audio, in milliseconds.
    /// </value>
    long TotalDuration { get; }

    /// <summary>
    /// Gets the total size of audio data in bytes.
    /// </summary>
    /// <value>
    /// The total size of the audio data in the stream, in bytes.
    /// </value>
    long TotalMediaSize { get; }

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between 2 frames when searching for frames.
    /// </summary>
    int MaxFrameSpacingLength { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the audio stream from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>true if the audio stream was successfully read; otherwise, false.</returns>
    bool ReadStream(Stream stream);

    /// <summary>
    /// Writes the serialised form of this container to the supplied <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write the container bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    void WriteTo(Stream destination);
}
