namespace AudioVideoLib.Formats;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// XING VBR Header used to store VBR information.
/// </summary>
public sealed class XingHeader : VbrHeader
{
    /// <summary>
    /// Header indicator for the XING VBR header.
    /// </summary>
    private const string VbrHeaderIndicator = "Xing";

    /// <summary>
    /// Header indicator for the CBR header.
    /// </summary>
    /// <remarks>
    /// In the Info Tag, the "Xing" identification string (mostly at 0x24) of the header is replaced by "Info" in case of a CBR file.
    /// This was done to avoid CBR files to be recognized as traditional Xing VBR files by some decoders.
    /// Although the two identification strings "Xing" and "Info" are both valid, it is suggested that you keep the identification string "Xing" in case of VBR bit stream in order to keep compatibility.
    /// </remarks>
    private const string CbrHeaderIndicator = "info";

    /// <summary>
    /// Initializes a new instance of the <see cref="XingHeader"/> class.
    /// </summary>
    /// <param name="firstFrame">The first frame.</param>
    /// <param name="firstFrameBuffer">The first frame buffer.</param>
    /// <param name="offset">The offset.</param>
    public XingHeader(MpaFrame firstFrame, StreamBuffer firstFrameBuffer, long offset) : base(firstFrame, firstFrameBuffer, offset, VbrHeaderType.Xing)
    {
        /*
        XING VBR-Header
        size    description
        4       'Xing' or 'Info'
        4       flags (indicates which fields are used)
        4       frames (optional)
        4       bytes (optional)
        100     toc (optional)
        4       a VBR quality indicator: 0=best 100=worst (optional)
        --------- +
        120 bytes
        * 
        * NOTE: the frames (frameCount) in the XING header does not include its own frame.
        * So the total frames is actually XING framecount + 1
        */

        // name of the tag as found in the file
        Name = firstFrameBuffer.ReadString(4);

        // The flags indicate which fields are used in the XING header
        Flags = firstFrameBuffer.ReadBigEndianInt32();

        // Extract total frames in the file (XING header excludes it's own frame)
        if ((Flags & XingHeaderFlags.FrameCountFlag) != 0)
        {
            FrameCount = firstFrameBuffer.ReadBigEndianInt32();
        }

        // Extract size of the file, in bytes
        if ((Flags & XingHeaderFlags.FileSizeFlag) != 0)
        {
            FileSize = firstFrameBuffer.ReadBigEndianInt32();
        }

        // Extract TOC (Table of Contents) for more accurate seeking
        if ((Flags & XingHeaderFlags.TocFlag) != 0)
        {
            Toc = new int[100];
            for (var i = 0; i < 100; i++)
            {
                Toc[i] = firstFrameBuffer.ReadByte();
            }
        }

        if ((Flags & XingHeaderFlags.VbrScaleFlag) != 0)
        {
            Quality = firstFrameBuffer.ReadBigEndianInt32();
        }

        // The LAME tag is always 120 bytes after the XING header - regardless of which fields are used
        LameTag = LameTag.FindTag(firstFrameBuffer, offset + 120);
    }

    /// <summary>
    /// Finds the <see cref="VbriHeader"/> header within the <paramref name="firstFrame"/>.
    /// </summary>
    /// <param name="firstFrame">The first frame.</param>
    /// <returns>The VBRI header if found; otherwise, null.</returns>
    /// <remarks>
    /// The XING header is located after the side information in Layer III in the first MPEG audio header in the file.
    /// It will compare the first 4 bytes against the <see cref="VbrHeaderIndicator"/> 
    /// to see if the header contains a <see cref="XingHeader"/> or not.
    /// </remarks>
    public static new XingHeader? FindHeader(MpaFrame firstFrame)
    {
        ArgumentNullException.ThrowIfNull(firstFrame);

        long offset = MpaFrame.FrameHeaderSize + firstFrame.SideInfoSize;
        var buffer = new StreamBuffer();
        var data = firstFrame.ToByteArray();
        buffer.Write(data);

        buffer.Seek(offset, SeekOrigin.Begin);
        var tagName = buffer.PeekString(4);
        return (string.Compare(tagName, VbrHeaderIndicator, StringComparison.OrdinalIgnoreCase) == 0)
            || (string.Compare(tagName, CbrHeaderIndicator, StringComparison.OrdinalIgnoreCase) == 0)
            ? new XingHeader(firstFrame, buffer, offset)
            : null;
    }

    /// <inheritdoc/>
    public override long SeekPositionByPercent(float percentage)
    {
        if (Toc == null)
        {
            return 0;
        }

        percentage = Math.Clamp(percentage, 0f, 100f);

        // Interpolate in TOC to get file seek point in bytes.
        // Toc has 100 entries (0..99); the implicit fb=256 beyond the last entry lets
        // percentage=100 map to the end of the file.
        var percent = (int)percentage;
        if (percent >= 100)
        {
            percent = 99;
        }

        var fa = (double)Toc[percent];
        var fb = percent < 99 ? (double)Toc[percent + 1] : 256.0;
        var fx = fa + ((fb - fa) * (percentage - percent));
        return (long)(fx / 256.0 * FileSize);
    }

    /// <inheritdoc/>
    public override byte[] ToByteArray()
    {
        var buf = new StreamBuffer();
        buf.WriteString(Name);
        buf.WriteBigEndianInt32(Flags);

        if ((Flags & XingHeaderFlags.FrameCountFlag) != 0)
        {
            buf.WriteBigEndianInt32(FrameCount);
        }

        if ((Flags & XingHeaderFlags.FileSizeFlag) != 0)
        {
            buf.WriteBigEndianInt32(FileSize);
        }

        // Extract TOC (Table of Contents) for more accurate seeking
        if ((Flags & XingHeaderFlags.TocFlag) != 0)
        {
            for (var i = 0; i < 100; i++)
            {
                buf.WriteByte((byte)Toc[i]);
            }
        }

        if ((Flags & XingHeaderFlags.VbrScaleFlag) != 0)
        {
            buf.WriteBigEndianInt32(Quality);
        }

        return buf.ToByteArray();
    }
}
