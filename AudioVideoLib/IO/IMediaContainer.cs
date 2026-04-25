namespace AudioVideoLib.IO;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// Interface for an audio format to implement streaming.
/// </summary>
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
    /// <value>
    /// The max length of spacing.
    /// </value>
    /// <remarks>
    /// When searching for frames, spacing might exist between 2 frames.
    /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
    /// </remarks>
    int MaxFrameSpacingLength { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the audio stream from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// true if the audio stream was successfully read; otherwise, false.
    /// </returns>
    bool ReadStream(Stream stream);

    /// <summary>
    /// Places the <see cref="IMediaContainer"/> into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the <see cref="IMediaContainer"/>.
    /// </returns>
    /// <remarks>
    /// Splice rewriters (MP4, ASF, Matroska, DSF, DFF) materialise the
    /// entire output file as a contiguous array. For large containers
    /// (hundreds of MB MKV / WAV files) prefer <see cref="WriteTo(Stream)"/>
    /// to avoid the heap allocation.
    /// </remarks>
    byte[] ToByteArray();

    /// <summary>
    /// Gets the size, in bytes, of the serialised form of this container.
    /// </summary>
    /// <remarks>
    /// Default implementation calls <see cref="ToByteArray"/> and reads
    /// its <c>Length</c>; concrete types are encouraged to override with a
    /// cheaper computation when one is available.
    /// </remarks>
    int GetSerializedSize() => ToByteArray().Length;

    /// <summary>
    /// Writes the serialised form of this container to the supplied <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write the container bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Default implementation calls <see cref="ToByteArray"/> and forwards
    /// the result. Concrete walkers can override to stream output directly
    /// — useful for the splice rewriters where the result is the size of
    /// the entire input file.
    /// </remarks>
    void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = ToByteArray();
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes the serialised form of this container to the supplied <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The buffer writer to write the container bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="writer"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Use this overload to integrate with <see cref="ArrayPool{T}"/>-backed
    /// pipelines (e.g. <c>System.IO.Pipelines.PipeWriter</c>). Default
    /// implementation calls <see cref="ToByteArray"/>; concrete walkers may
    /// override to write into pooled segments without an intermediate copy.
    /// </remarks>
    void WriteTo(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var bytes = ToByteArray();
        writer.Write(bytes);
    }

    /// <summary>
    /// Attempts to write the serialised form of this container into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">A pre-sized buffer to write into.</param>
    /// <param name="written">
    /// On success, the number of bytes written. On failure, <c>0</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> when the buffer was large enough; <c>false</c> when
    /// <paramref name="destination"/> is shorter than
    /// <see cref="GetSerializedSize"/>, in which case nothing is written.
    /// </returns>
    bool TryWriteTo(Span<byte> destination, out int written)
    {
        var bytes = ToByteArray();
        if (bytes.Length > destination.Length)
        {
            written = 0;
            return false;
        }

        bytes.CopyTo(destination);
        written = bytes.Length;
        return true;
    }
}
