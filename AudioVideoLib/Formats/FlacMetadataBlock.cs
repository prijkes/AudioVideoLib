/*
 * Date: 2013-02-02
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for FLAC audio frames.
    /// </summary>
    public partial class FlacMetadataBlock
    {
        /// <summary>
        /// The minimum block size, in bytes.
        /// </summary>
        public const int MinimumSize = 4;

        private int _flags;

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
                    Flags &= ~HeaderFlags.IsLastBlock;
                else
                    Flags |= HeaderFlags.IsLastBlock;
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
        public virtual byte[] Data { get; protected set; }

        /// <summary>
        /// Gets the flags of this frame.
        /// </summary>
        /// The flags value is stored as short (2 bytes) for all versions.
        private int Flags
        {
            get
            {
                return (_flags | ((int)BlockType & HeaderFlags.BlockType));
            }

            set
            {
                _flags = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the block.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static FlacMetadataBlock ReadBlock(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return ReadBlock(stream as StreamBuffer ?? new StreamBuffer(stream));
        }

        /// <summary>
        /// Reads the block.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        public static FlacMetadataBlock ReadBlock(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return ReadBlock(new StreamBuffer(data));
        }

        /// <summary>
        /// Returns the metadata block in a byte array.
        /// </summary>
        /// <returns>The frame in a byte array.</returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                buffer.WriteBigEndianBytes(Flags, 1);
                buffer.WriteBigEndianBytes(Data.Length, 3);
                buffer.Write(Data);
                return buffer.ToByteArray();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static FlacMetadataBlock ReadBlock(StreamBuffer stream)
        {
            int flags = stream.ReadByte();

            // Length
            int length = stream.ReadBigEndianInt(3);
            if (length >= stream.Length)
                return null;

            byte[] data = new byte[length];
            stream.Read(data, length);

            FlacMetadataBlock metadataBlock;
            FlacMetadataBlockType blockType = (FlacMetadataBlockType)(flags & HeaderFlags.BlockType);
            switch (blockType)
            {
                case FlacMetadataBlockType.Padding:
                    metadataBlock = new FlacPaddingMetadataBlock();
                    break;

                case FlacMetadataBlockType.Application:
                    metadataBlock = new FlacApplicationMetadataBlock();
                    break;

                case FlacMetadataBlockType.StreamInfo:
                    metadataBlock = new FlacStreamInfoMetadataBlock();
                    break;

                case FlacMetadataBlockType.SeekTable:
                    metadataBlock = new FlacSeekTableMetadataBlock();
                    break;

                case FlacMetadataBlockType.VorbisComment:
                    metadataBlock = new FlacVorbisCommentsMetadataBlock();
                    break;

                case FlacMetadataBlockType.CueSheet:
                    metadataBlock = new FlacCueSheetMetadataBlock();
                    break;

                case FlacMetadataBlockType.Picture:
                    metadataBlock = new FlacPictureMetadataBlock();
                    break;

                default:
                    metadataBlock = new FlacMetadataBlock();
                    break;
            }
            metadataBlock.Flags = flags;
            metadataBlock.Data = data;
            return metadataBlock;
        }
    }
}
