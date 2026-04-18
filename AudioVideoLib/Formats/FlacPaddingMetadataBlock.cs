namespace AudioVideoLib.Formats;

using System;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public sealed class FlacPaddingMetadataBlock : FlacMetadataBlock
{
    /// <inheritdoc/>
    public override FlacMetadataBlockType BlockType => FlacMetadataBlockType.Padding;

    /// <inheritdoc/>
    public override byte[] Data
    {
        get => base.Data;

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);
            base.Data = value;
        }
    }
}
