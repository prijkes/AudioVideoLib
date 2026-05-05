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
///
/// <para />
/// <b>Source-stream lifetime contract.</b> Walkers that splice unchanged byte ranges from the
/// input stream (currently <c>Mp4Stream</c>, <c>AsfStream</c>, <c>MatroskaStream</c>; after the
/// format-pack retrofit also <c>FlacStream</c>, <c>MpaStream</c>, and the four new walkers
/// <c>MpcStream</c>, <c>WavPackStream</c>, <c>TtaStream</c>, <c>MacStream</c>) hold an
/// <see cref="ISourceReader"/> populated at <see cref="ReadStream"/> time. The caller must keep
/// the source <see cref="Stream"/> alive between <see cref="ReadStream"/> and
/// <see cref="WriteTo"/>. Calling <see cref="WriteTo"/> on a walker whose source has been
/// disposed (via <see cref="IDisposable.Dispose"/>) — or that was never read — produces an
/// <see cref="InvalidOperationException"/> with the message
/// <c>"Source stream was detached or never read. WriteTo requires a live source."</c>.
/// Walkers that do not need the source (no-op <c>Dispose()</c>) are exempt from this rule.
/// </remarks>
public interface IMediaContainer : IDisposable
{
    /// <summary>
    /// Gets the start offset of the <see cref="IMediaContainer"/>, where it starts in the stream.
    /// </summary>
    long StartOffset { get; }

    /// <summary>
    /// Gets the end offset of the <see cref="IMediaContainer"/>, where it ends in the stream.
    /// </summary>
    long EndOffset { get; }

    /// <summary>
    /// Gets the total length of audio in milliseconds.
    /// </summary>
    long TotalDuration { get; }

    /// <summary>
    /// Gets the total size of audio data in bytes.
    /// </summary>
    long TotalMediaSize { get; }

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between 2 frames when searching for frames.
    /// </summary>
    int MaxFrameSpacingLength { get; set; }

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
    /// <exception cref="InvalidOperationException">
    /// Thrown by walkers that hold an <see cref="ISourceReader"/> when their source has been
    /// disposed or was never populated. See the source-stream lifetime contract above.
    /// </exception>
    void WriteTo(Stream destination);
}
