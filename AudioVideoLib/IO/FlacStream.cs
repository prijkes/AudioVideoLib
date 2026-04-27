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

        var identifierBytes = System.Text.Encoding.ASCII.GetBytes(Identifier);
        destination.Write(identifierBytes, 0, identifierBytes.Length);

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

        foreach (var frame in Frames)
        {
            var bytes = frame.ToByteArray();
            destination.Write(bytes, 0, bytes.Length);
        }
    }
}
