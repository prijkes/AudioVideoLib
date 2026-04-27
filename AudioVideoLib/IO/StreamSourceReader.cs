namespace AudioVideoLib.IO;

using System;
using System.IO;

/// <summary>
/// <see cref="ISourceReader"/> implementation backed by a seekable <see cref="Stream"/>.
/// </summary>
/// <remarks>
/// The stream is borrowed by default — disposal does not close it unless
/// <c>leaveOpen</c> was set to <c>false</c> at construction. The caller is responsible
/// for ensuring the stream remains valid for the lifetime of any walker that holds this
/// reader. Calls are not thread-safe; the reader uses <see cref="Stream.Position"/>
/// directly and assumes serial access.
/// </remarks>
public sealed class StreamSourceReader : ISourceReader
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly long _baseOffset;

    /// <summary>
    /// Initializes a new <see cref="StreamSourceReader"/> over a seekable stream.
    /// </summary>
    /// <param name="stream">The seekable stream. Must not be <c>null</c>.</param>
    /// <param name="leaveOpen">
    /// <c>true</c> (default) to leave the stream open when this reader is disposed;
    /// <c>false</c> to close the stream on disposal.
    /// </param>
    /// <exception cref="ArgumentException">If <paramref name="stream"/> is not seekable.</exception>
    public StreamSourceReader(Stream stream, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Source stream must be seekable.", nameof(stream));
        }

        _stream = stream;
        _leaveOpen = leaveOpen;
        _baseOffset = stream.Position;
        Length = stream.Length - _baseOffset;
    }

    /// <inheritdoc/>
    public long Length { get; }

    /// <inheritdoc/>
    public void Read(long offset, Span<byte> destination)
    {
        if (offset < 0 || offset > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        _stream.Position = _baseOffset + offset;
        _stream.ReadExactly(destination);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
