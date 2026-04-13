namespace AudioVideoLib.Formats;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public partial class FlacMetadataBlock
{
    /// <summary>
    /// The minimum block size, in bytes.
    /// </summary>
    public const int MinimumSize = 4;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="FlacMetadataBlock" /> class.
    /// </summary>
    protected FlacMetadataBlock()
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets a value indicating whether this instance is the last metadata block.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is the last metadata block; otherwise, <c>false</c>.
    /// </value>
    public bool IsLastBlock
    {
        get
        {
            return (Flags & HeaderFlags.IsLastBlock) == HeaderFlags.IsLastBlock;
        }

        set
        {
            if (value)
            {
                Flags |= HeaderFlags.IsLastBlock;
            }
            else
            {
                Flags &= ~HeaderFlags.IsLastBlock;
            }
        }
    }

    /// <summary>
    /// Gets or sets the type of the block.
    /// </summary>
    /// <value>
    /// The type of the block.
    /// </value>
    public virtual FlacMetadataBlockType BlockType
    {
        get
        {
            return (FlacMetadataBlockType)(Flags & HeaderFlags.BlockType);
        }
    }

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    /// <value>
    /// The data.
    /// </value>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public virtual byte[] Data { get; protected set; } = null!;

    /// <summary>
    /// Gets the flags of this frame.
    /// </summary>
    /// The flags value is stored as short (2 bytes) for all versions.
    private int Flags
    {
        get
        {
            return field | ((int)BlockType & HeaderFlags.BlockType);
        }

        set;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the block.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">stream</exception>
    public static FlacMetadataBlock? ReadBlock(Stream stream)
    {
        return stream == null ? throw new ArgumentNullException("stream") : ReadBlock(stream as StreamBuffer ?? new StreamBuffer(stream));
    }

    /// <summary>
    /// Reads the block.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">data</exception>
    public static FlacMetadataBlock? ReadBlock(byte[] data)
    {
        return data == null ? throw new ArgumentNullException("data") : ReadBlock(new StreamBuffer(data));
    }

    /// <summary>
    /// Returns the metadata block in a byte array.
    /// </summary>
    /// <returns>The frame in a byte array.</returns>
    public byte[] ToByteArray()
    {
        var buffer = new StreamBuffer();
        buffer.WriteBigEndianBytes(Flags, 1);
        buffer.WriteBigEndianBytes(Data.Length, 3);
        buffer.Write(Data);
        return buffer.ToByteArray();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static FlacMetadataBlock? ReadBlock(StreamBuffer stream)
    {
        var flags = stream.ReadByte();

        // Length
        var length = stream.ReadBigEndianInt(3);
        if (length >= stream.Length)
        {
            return null;
        }

        var data = new byte[length];
        stream.Read(data, length);
        var blockType = (FlacMetadataBlockType)(flags & HeaderFlags.BlockType);

        var metadataBlock = blockType switch
        {
            FlacMetadataBlockType.Padding => new FlacPaddingMetadataBlock(),
            FlacMetadataBlockType.Application => new FlacApplicationMetadataBlock(),
            FlacMetadataBlockType.StreamInfo => new FlacStreamInfoMetadataBlock(),
            FlacMetadataBlockType.SeekTable => new FlacSeekTableMetadataBlock(),
            FlacMetadataBlockType.VorbisComment => new FlacVorbisCommentsMetadataBlock(),
            FlacMetadataBlockType.CueSheet => new FlacCueSheetMetadataBlock(),
            FlacMetadataBlockType.Picture => new FlacPictureMetadataBlock(),
            _ => new FlacMetadataBlock(),
        };
        metadataBlock.Flags = flags;
        metadataBlock.Data = data;
        return metadataBlock;
    }
}
