namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;

/// <summary>
/// The stream containing FLAC Audio <see cref="FlacFrame"/>s.
/// </summary>
public sealed partial class FlacStream : IMediaContainer
{
    private const string Identifier = "fLaC";

    private readonly List<FlacFrame> _frames = [];

    private readonly List<FlacMetadataBlock> _metadataBlocks = [];

    /// <summary>
    /// Live source reader populated by <see cref="ReadStream"/>; consumed by
    /// <see cref="WriteTo"/> for byte-passthrough of audio frames. Disposed by
    /// <see cref="Dispose"/>. <c>null</c> until <see cref="ReadStream"/> succeeds.
    /// </summary>
    private ISourceReader? _source;

    /// <summary>
    /// File-absolute offset of the <c>fLaC</c> magic captured during <see cref="ReadStream"/>.
    /// Used by <see cref="WriteTo"/> to translate file-absolute frame offsets into
    /// <see cref="ISourceReader"/>-relative offsets (offset 0 in the source view == start of magic).
    /// </summary>
    private long _containerStart;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the start offset of the <see cref="IMediaContainer"/>, where it starts in the stream.
    /// </summary>
    /// <value>
    /// The start offset of the <see cref="IMediaContainer"/>, counting from the start of the stream.
    /// </value>
    public long StartOffset => _frames.Count > 0 ? _frames[0].StartOffset : 0;

    /// <summary>
    /// Gets the end offset of the <see cref="IMediaContainer"/>, where it ends in the stream.
    /// </summary>
    /// <value>
    /// The end offset of the <see cref="IMediaContainer"/>, counting from the start of the stream.
    /// </value>
    public long EndOffset => _frames.Count > 0 ? _frames[^1].EndOffset : 0;

    /// <summary>
    /// Gets the frames in the stream.
    /// </summary>
    /// <value>
    /// A list of <see cref="FlacFrame"/>s in the stream.
    /// </value>
    public IEnumerable<FlacFrame> Frames => _frames.AsReadOnly();

    /// <summary>
    /// Gets the total length of audio in milliseconds.
    /// </summary>
    /// <value>
    /// The total length of audio, in milliseconds.
    /// </value>
    public long TotalDuration => Frames.Sum(f => f.AudioLength);

    /// <summary>
    /// Gets the total size of audio data in bytes.
    /// </summary>
    /// <value>
    /// The total size of the audio data in the stream, in bytes.
    /// </value>
    public long TotalMediaSize => Frames.Sum(f => f.FrameLength);

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between 2 frames when searching for frames.
    /// </summary>
    /// <value>
    ///  The max length of spacing.
    /// </value>
    /// <remarks>
    /// When searching for frames, spacing might exist between 2 frames.
    /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
    /// </remarks>
    public int MaxFrameSpacingLength { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between 2 metadata blocks when searching for metadata blocks.
    /// </summary>
    /// <value>
    ///  The max length of spacing.
    /// </value>
    /// <remarks>
    /// When searching for metadata blocks, spacing might exist between 2 metadata blocks.
    /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
    /// </remarks>
    public int MaxMetadataBlockSpacingLength { get; set; } = 16;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the FLAC stream from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// true if one or more frames are read from the stream; otherwise, false.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">stream</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var streamLength = sb.Length;
        if (sb.Position + Identifier.Length > streamLength)
        {
            return false;
        }

        // Check 'fLaC' identifier.
        var identifier = sb.ReadString(Identifier.Length);
        if (!string.Equals(identifier, Identifier, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Capture the live source for byte-passthrough WriteTo. We rewind the underlying
        // stream to the FLAC start before constructing StreamSourceReader, so the reader's
        // offset 0 == start of 'fLaC' magic. Then restore position so the existing parse
        // path resumes immediately past the magic.
        var flacStart = sb.Position - Identifier.Length;
        _containerStart = flacStart;
        var sourceBaseline = stream.Position;
        stream.Position = flacStart;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        stream.Position = sourceBaseline;

        long spacing = 0;

        // Read all metadata blocks.
        while (sb.Position + FlacMetadataBlock.MinimumSize <= streamLength && spacing < MaxMetadataBlockSpacingLength)
        {
            var metadataBlock = FlacMetadataBlock.ReadBlock(sb);
            if (metadataBlock is not null)
            {
                spacing = 0;
                _metadataBlocks.Add(metadataBlock);
                if (metadataBlock.IsLastBlock)
                {
                    break;
                }

                continue;
            }
            spacing++;
        }
        sb.Position -= spacing;

        // Read all frames.
        while (stream.Position <= streamLength && spacing < MaxFrameSpacingLength)
        {
            spacing++;
            var frame = FlacFrame.ReadFrame(stream, this);
            if (frame is not null)
            {
                spacing = 0;
                _frames.Add(frame);
                continue;
            }
            stream.Position++;
        }
        return _frames.Count > 0;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Emits the <c>fLaC</c> magic, then each metadata block, then each audio frame, in turn —
    /// written directly to <paramref name="destination"/> without building an intermediate
    /// byte array.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        // 'fLaC' magic.
        var identifierBytes = System.Text.Encoding.ASCII.GetBytes(Identifier);
        destination.Write(identifierBytes, 0, identifierBytes.Length);

        // Metadata blocks: encoded via their existing per-block ToByteArray() path
        // (these stay encodable — tags live here). STREAMINFO is emitted first per
        // the FLAC spec, then every other block in original order.
        var streamInfoMetadataBlock = StreamInfoMetadataBlocks.FirstOrDefault();
        if (streamInfoMetadataBlock is not null)
        {
            var bytes = streamInfoMetadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        foreach (var metadataBlock in MetadataBlocks.Where(m => !ReferenceEquals(m, streamInfoMetadataBlock)))
        {
            var bytes = metadataBlock.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        // Audio frames: byte-passthrough from the captured source. No re-encode.
        // FlacFrame.StartOffset is file-absolute (set in FlacFrame.ReadFrame from sb.Position).
        // _source's offset 0 == _containerStart (the 'fLaC' magic), so we translate by
        // subtracting _containerStart. Note: FlacStream.StartOffset is the FIRST FRAME's
        // offset, not the container start, hence the dedicated _containerStart field.
        foreach (var frame in Frames)
        {
            var offsetInSource = frame.StartOffset - _containerStart;
            _source.CopyTo(offsetInSource, frame.Length, destination);
        }
    }

    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
    /// <see cref="Stream"/>; the caller still owns that. Idempotent.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }
}
