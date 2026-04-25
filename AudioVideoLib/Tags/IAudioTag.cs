namespace AudioVideoLib.Tags;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// Interface which tag implementations must implement.
/// </summary>
public interface IAudioTag : IEquatable<IAudioTag>
{
    /// <summary>
    /// Places the <see cref="IAudioTag"/> into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the <see cref="IAudioTag"/>.
    /// </returns>
    /// <remarks>
    /// Allocates a fresh array on every call. For tags whose serialised form
    /// is small (a few KB) this is the simplest option. For large tags or
    /// tight write loops, prefer <see cref="WriteTo(Stream)"/> or
    /// <see cref="TryWriteTo(Span{byte}, out int)"/> to avoid the
    /// per-call allocation.
    /// </remarks>
    byte[] ToByteArray();

    /// <summary>
    /// Gets the size, in bytes, of the serialised form of this tag.
    /// </summary>
    /// <remarks>
    /// The default implementation calls <see cref="ToByteArray"/> and reads
    /// its <c>Length</c>; concrete types are encouraged to override with a
    /// cheaper computation when one is available.
    /// </remarks>
    int GetSerializedSize() => ToByteArray().Length;

    /// <summary>
    /// Writes the serialised form of this tag to the supplied <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write the tag bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Default implementation calls <see cref="ToByteArray"/> and forwards
    /// the result to <see cref="Stream.Write(byte[], int, int)"/>; concrete
    /// types may override to stream directly without materialising the
    /// whole buffer.
    /// </remarks>
    void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = ToByteArray();
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes the serialised form of this tag to the supplied <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The buffer writer to write the tag bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="writer"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Use this overload to integrate with <see cref="ArrayPool{T}"/>-backed
    /// pipelines (e.g. <c>System.IO.Pipelines.PipeWriter</c>). Default
    /// implementation calls <see cref="ToByteArray"/>; concrete types may
    /// override to write into pooled segments without an intermediate copy.
    /// </remarks>
    void WriteTo(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var bytes = ToByteArray();
        writer.Write(bytes);
    }

    /// <summary>
    /// Attempts to write the serialised form of this tag into <paramref name="destination"/>.
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
    /// <remarks>
    /// Useful when callers can size up front via <see cref="GetSerializedSize"/>
    /// (or rent a buffer from <see cref="ArrayPool{T}"/>) and want to skip
    /// the per-call allocation that <see cref="ToByteArray"/> performs.
    /// </remarks>
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
