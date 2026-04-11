/*
 * Date: 2013-02-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// The stream containing FLAC Audio <see cref="FlacFrame"/>s.
/// </summary>
public sealed partial class FlacStream : IAudioStream
{
    // Max length of spacing, in bytes, between 2 metadata blocks. If there is spacing between metadata blocks, this means that a metadata block is corrupted.

    // Max length of spacing, in bytes, between 2 frames. If there is spacing between frames, this means that a frame is corrupted.

    private const string Identifier = "fLaC";

    private readonly List<FlacFrame> _frames = [];

    private readonly List<FlacMetadataBlock> _metadataBlocks = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the start offset of the <see cref="IAudioStream"/>, where it starts in the stream.
    /// </summary>
    /// <value>
    /// The start offset of the <see cref="IAudioStream"/>, counting from the start of the stream.
    /// </value>
    public long StartOffset
    {
        get
        {
            return _frames.Any() ? _frames.First().StartOffset : 0;
        }
    }

    /// <summary>
    /// Gets the end offset of the <see cref="IAudioStream"/>, where it ends in the stream.
    /// </summary>
    /// <value>
    /// The end offset of the <see cref="IAudioStream"/>, counting from the start of the stream.
    /// </value>
    public long EndOffset
    {
        get
        {
            return _frames.Any() ? _frames.Last().EndOffset : 0;
        }
    }

    /// <summary>
    /// Gets the frames in the stream.
    /// </summary>
    /// <value>
    /// A list of <see cref="Id3v2Frame"/>s in the stream.
    /// </value>
    public IEnumerable<FlacFrame> Frames
    {
        get
        {
            return _frames.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the total length of audio in milliseconds.
    /// </summary>
    /// <value>
    /// The total length of audio, in milliseconds.
    /// </value>
    public long TotalAudioLength
    {
        get
        {
            return Frames.Sum(f => f.AudioLength);
        }
    }

    /// <summary>
    /// Gets the total size of audio data in bytes.
    /// </summary>
    /// <value>
    /// The total size of the audio data in the stream, in bytes.
    /// </value>
    public long TotalAudioSize
    {
        get
        {
            return Frames.Sum(f => f.FrameLength);
        }
    }

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
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var streamLength = sb.Length;
        if ((sb.Position + Identifier.Length) > streamLength)
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
        while (((sb.Position + FlacMetadataBlock.MinimumSize) <= streamLength) && (spacing < MaxMetadataBlockSpacingLength))
        {
            var metadataBlock = FlacMetadataBlock.ReadBlock(sb);
            if (metadataBlock != null)
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
        while ((stream.Position <= streamLength) && (spacing < MaxFrameSpacingLength))
        {
            spacing++;
            var frame = FlacFrame.ReadFrame(stream, this);
            if (frame != null)
            {
                spacing = 0;
                _frames.Add(frame);
                continue;
            }
            stream.Position++;
        }
        return _frames.Any();
    }

    /// <inheritdoc />
    public byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.WriteString(Identifier);
        var streamInfoMetadataBlock = StreamInfoMetadataBlocks.FirstOrDefault();
        if (streamInfoMetadataBlock != null)
        {
            sb.Write(streamInfoMetadataBlock.ToByteArray());
        }

        foreach (var metadataBlock in MetadataBlocks.Where(m => !ReferenceEquals(m, streamInfoMetadataBlock)))
        {
            sb.Write(metadataBlock.ToByteArray());
        }

        foreach (var frame in Frames)
        {
            sb.Write(frame.ToByteArray());
        }

        return sb.ToByteArray();
    }
}
