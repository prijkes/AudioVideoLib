namespace AudioVideoLib.Formats;

using System;
using System.IO;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public class FlacVorbisCommentsMetadataBlock : FlacMetadataBlock
{
    /// <inheritdoc/>
    public override FlacMetadataBlockType BlockType
    {
        get
        {
            return FlacMetadataBlockType.VorbisComment;
        }
    }

    /// <inheritdoc/>
    public override byte[] Data
    {
        get
        {
            return VorbisComments.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            VorbisComments = VorbisComments.ReadStream(new StreamBuffer(value)) ?? throw new InvalidDataException("Could not parse Vorbis comments block.");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the vorbis comment.
    /// </summary>
    /// <value>
    /// The vorbis comment.
    /// </value>
    public VorbisComments VorbisComments { get; set; } = null!;
}
