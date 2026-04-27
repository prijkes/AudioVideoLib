namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// <see cref="ISourceReader"/> implementation backed by an in-memory byte array.
/// </summary>
/// <remarks>
/// The byte array is borrowed, not owned — disposal does not release it. Use this when the
/// source bytes are already in memory (e.g. captured by a previous reader, embedded in another
/// payload, or test fixtures) and there's nothing to release on disposal.
/// </remarks>
public sealed class MemorySourceReader : ISourceReader
{
    private readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new <see cref="MemorySourceReader"/> backed by the supplied byte array.
    /// </summary>
    /// <param name="bytes">The source bytes. Must not be <c>null</c>.</param>
    public MemorySourceReader(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        _bytes = bytes;
    }

    /// <inheritdoc/>
    public long Length => _bytes.Length;

    /// <inheritdoc/>
    public void Read(long offset, Span<byte> destination)
    {
        if (offset < 0 || offset > _bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (offset + destination.Length > _bytes.Length)
        {
            throw new EndOfStreamException();
        }

        _bytes.AsSpan((int)offset, destination.Length).CopyTo(destination);
    }

    /// <inheritdoc/>
    public void CopyTo(long offset, long count, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (count <= 0)
        {
            return;
        }

        if (offset < 0 || offset + count > _bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        destination.Write(_bytes, (int)offset, (int)count);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // The byte array is caller-owned; nothing to release.
    }
}
