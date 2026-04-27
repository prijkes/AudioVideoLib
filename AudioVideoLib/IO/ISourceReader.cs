namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// Random-access read facade over the bytes a media-container walker captured at parse time.
/// </summary>
/// <remarks>
/// Splice rewriters (MP4, ASF, Matroska, DSF, DFF) need to copy unchanged byte ranges from the
/// source at write time. Holding the entire input in memory does not scale to multi-GB files;
/// <see cref="ISourceReader"/> abstracts the random-access read so the same walker can run
/// against either an in-memory buffer (<see cref="MemorySourceReader"/>) or a seekable file
/// stream (<see cref="StreamSourceReader"/>).
/// <para />
/// Implementations are not required to be thread-safe — a walker should hold a single reader
/// and use it sequentially. Callers must keep the underlying source (file stream, memory
/// buffer) alive until the walker is done with it; <see cref="IDisposable"/> is implemented so
/// implementations can release their own resources, but the *caller's* source is not owned.
/// </remarks>
public interface ISourceReader : IDisposable
{
    /// <summary>
    /// Gets the total length of the source, in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Reads bytes starting at <paramref name="offset"/> into <paramref name="destination"/>.
    /// Reads exactly <paramref name="destination"/>.<see cref="Span{T}.Length"/> bytes; throws
    /// <see cref="EndOfStreamException"/> if fewer are available.
    /// </summary>
    /// <param name="offset">The absolute byte offset (from the start of the source).</param>
    /// <param name="destination">The buffer to read into.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="offset"/> is negative or beyond <see cref="Length"/>.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// If the source has fewer than <paramref name="destination"/>.<see cref="Span{T}.Length"/>
    /// bytes available from <paramref name="offset"/>.
    /// </exception>
    void Read(long offset, Span<byte> destination);

    /// <summary>
    /// Copies a byte range from the source directly to <paramref name="destination"/>.
    /// </summary>
    /// <param name="offset">The absolute byte offset (from the start of the source).</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <param name="destination">The destination stream.</param>
    /// <remarks>
    /// Default implementation reads in 64 KB chunks via <see cref="Read"/> — concrete sources
    /// that have a faster bulk-copy path (memory-to-memory <see cref="Buffer.BlockCopy"/>,
    /// for instance) are encouraged to override.
    /// </remarks>
    void CopyTo(long offset, long count, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (count <= 0)
        {
            return;
        }

        Span<byte> chunk = stackalloc byte[16384];
        var remaining = count;
        var pos = offset;
        while (remaining > 0)
        {
            var want = (int)Math.Min(remaining, chunk.Length);
            Read(pos, chunk[..want]);
            destination.Write(chunk[..want]);
            pos += want;
            remaining -= want;
        }
    }
}
