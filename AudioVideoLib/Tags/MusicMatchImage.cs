/*
 * Date: 2012-11-04
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */
namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class to store a MusicMatch tag.
/// </summary>
/// MusicMatch tags can contain at most one image.
public sealed class MusicMatchImage
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    /// <value>
    /// The file extension.
    /// </value>
    /// <remarks>
    /// The extension of the image when saved as a file (for example, "jpg" or "bmp").
    /// This section is 4 bytes in length, and the data is padded with spaces (0x20) if the extension doesn't use all 4 bytes (in practice, 3-byte extensions are the most prevalent).
    /// Likewise, a <see cref="MusicMatchTag"/> without images have all spaces for this section (4 * 0x20).
    /// </remarks>
    /// This first required section is the extension of the image when saved as a file (for example, "jpg" or "bmp").
    /// This section is 4 bytes in length, and the data is padded with spaces (0x20) if the extension doesn't use all 4 bytes (in practice, 3-byte extensions are the most prevalent).
    /// Likewise, a <see cref="MusicMatchTag"/> without images have all spaces for this section (4 * 0x20).
    public string FileExtension
    {
        get;

        set
        {
            var fileExtension = value;
            if (!string.IsNullOrEmpty(fileExtension))
            {
                if (fileExtension.Length > 4)
                {
                    fileExtension = fileExtension[..4];
                }
                else if (fileExtension.Length < 4)
                {
                    fileExtension = fileExtension.PadRight(4 - fileExtension.Length);
                }
            }
            field = fileExtension;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the binary image.
    /// </summary>
    /// <value>
    /// The binary image.
    /// </value>
    /// <remarks>
    /// The actual image data.
    /// If no image is present, the image binary section consists of exactly four null bytes (4 * 0x00).
    /// </remarks>
    /// When an image is present in the tag, the image binary section consists of two fields.
    /// The first field is the size of the image data, in bytes.
    /// The second is the actual image data.
    /// <para />
    /// If no image is present, the image binary section consists of exactly four null bytes (0x00 0x00 0x00 0x00).
    public byte[] BinaryImage { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a <see cref="MusicMatchImage"/> from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>A <see cref="MusicMatchImage"/>.</returns>
    public static MusicMatchImage ReadFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);

        // Read image
        var image = new MusicMatchImage { FileExtension = sb.ReadString(4) };
        var imageSize = sb.ReadInt32();
        if (imageSize > 0)
        {
            image.BinaryImage = new byte[imageSize];
            sb.Read(image.BinaryImage, imageSize);
        }
        return image;
    }
}
