/*
 * Date: 2013-02-16
 * Sources used:
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class for storing an attached picture frame.
    /// </summary>
    /// <remarks>
    /// This frame contains a picture directly related to the audio file.
    /// </remarks>
    public sealed class FlacPictureMetadataBlock : FlacMetadataBlock
    {
        /// <inheritdoc/>
        public override FlacMetadataBlockType BlockType
        {
            get
            {
                return FlacMetadataBlockType.Picture;
            }
        }

        /// <inheritdoc/>
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteBigEndianInt32((int)PictureType);
                    stream.WriteBigEndianInt32(MimeType.Length);
                    stream.WriteString(MimeType);
                    stream.WriteBigEndianInt32(Encoding.UTF8.GetByteCount(Description));
                    stream.WriteString(Description, Encoding.UTF8);
                    stream.WriteBigEndianInt32(Width);
                    stream.WriteBigEndianInt32(Height);
                    stream.WriteBigEndianInt32(ColorDepth);
                    stream.WriteBigEndianInt32(ColorCount);
                    stream.WriteBigEndianInt32(PictureData.Length);
                    stream.Write(PictureData);
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    PictureType = (FlacPictureType)stream.ReadBigEndianInt32();
                    int length = stream.ReadBigEndianInt32();
                    MimeType = stream.ReadString(length);
                    length = stream.ReadBigEndianInt32();
                    Description = stream.ReadString(length, Encoding.UTF8);
                    Width = stream.ReadBigEndianInt32();
                    Height = stream.ReadBigEndianInt32();
                    ColorDepth = stream.ReadBigEndianInt32();
                    ColorCount = stream.ReadBigEndianInt32();
                    length = stream.ReadBigEndianInt32();
                    PictureData = new byte[length];
                    stream.Read(PictureData, length);
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the type of the picture.
        /// </summary>
        /// <value>
        /// The type of the picture.
        /// </value>
        /// <remarks>
        /// There may only be one each of picture type <see cref="FlacPictureType.FileIcon"/> and <see cref="FlacPictureType.OtherFileIcon"/> in a file.
        /// </remarks>
        public FlacPictureType PictureType { get; private set; }

        /// <summary>
        /// Gets the MIME type.
        /// </summary>
        /// <value>
        /// The MIME type.
        /// </value>
        /// <remarks>
        /// The MIME type string is in printable ASCII characters 0x20-0x7e.
        /// The MIME type may also be --> to signify that the data part is a URL of the picture instead of the picture data itself.
        /// </remarks>
        public string MimeType { get; private set; }

        /// <summary>
        /// Gets the description of the picture.
        /// </summary>
        /// <value>
        /// The description of the picture.
        /// </value>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the width of the picture in pixels.
        /// </summary>
        /// <value>
        /// The width of the picture in pixels.
        /// </value>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the height of the picture in pixels.
        /// </summary>
        /// <value>
        /// The height of the picture in pixels.
        /// </value>
        public int Height { get; private set; }

        /// <summary>
        /// Gets the color depth of the picture in bits-per-pixel.
        /// </summary>
        /// <value>
        /// The color depth of the picture in bits-per-pixel.
        /// </value>
        public int ColorDepth { get; private set; }

        /// <summary>
        /// Gets the number of colors used.
        /// </summary>
        /// <value>
        /// The number of colors used.
        /// </value>
        /// <remarks>
        /// For indexed-color pictures (e.g. GIF), the number of colors used, or 0 for non-indexed pictures.
        /// </remarks>
        public int ColorCount { get; private set; }

        /// <summary>
        /// Gets the binary picture data.
        /// </summary>
        /// <value>
        /// The binary picture data.
        /// </value>
        public byte[] PictureData { get; private set; }
    }
}
