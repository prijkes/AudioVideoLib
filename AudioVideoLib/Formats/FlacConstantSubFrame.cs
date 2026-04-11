/*
 * Date: 2013-03-23
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
namespace AudioVideoLib.Formats;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Represents a constant-signal subframe.
/// </summary>
public sealed class FlacConstantSubFrame : FlacSubFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlacConstantSubFrame"/> class.
    /// </summary>
    /// <param name="flacFrame">The FLAC frame.</param>
    public FlacConstantSubFrame(FlacFrame flacFrame) : base(flacFrame)
    {
        ArgumentNullException.ThrowIfNull(flacFrame);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the unencoded constant value of the subblock.
    /// </summary>
    /// <value>
    /// The unencoded constant value of the subblock.
    /// </value>
    public int UnencodedConstantValue { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] ToByteArray()
    {
        var sb = new StreamBuffer();
        sb.Write(base.ToByteArray());
        sb.WriteBigEndianBytes(UnencodedConstantValue, SampleSize / 8);
        return sb.ToByteArray();
    }

    ////protected override void Read(StreamBuffer sb)
    ////{
    ////    UnencodedConstantValue = sb.ReadBigEndianInt(SampleSize / 8);
    ////}
}
