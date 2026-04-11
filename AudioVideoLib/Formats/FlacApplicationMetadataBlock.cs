/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public class FlacApplicationMetadataBlock : FlacMetadataBlock
{
    /// <inheritdoc/>
    public override FlacMetadataBlockType BlockType
    {
        get
        {
            return FlacMetadataBlockType.Application;
        }
    }

    /// <inheritdoc/>
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            stream.WriteString(ApplicationIdentifier);
            if (ApplicationData != null)
            {
                stream.Write(ApplicationData);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            ApplicationIdentifier = stream.ReadString(4);
            ApplicationData = new byte[stream.Length - stream.Position];
            stream.Read(ApplicationData, ApplicationData.Length);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the application identifier.
    /// </summary>
    /// <value>
    /// The application identifier.
    /// </value>
    public string ApplicationIdentifier { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the application data.
    /// </summary>
    /// <value>
    /// The application data.
    /// </value>
    public byte[] ApplicationData { get; private set; } = null!;
}
