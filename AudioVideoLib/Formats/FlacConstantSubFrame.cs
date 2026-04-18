namespace AudioVideoLib.Formats;

using AudioVideoLib.IO;

/// <summary>
/// Represents a constant-signal subframe.
/// </summary>
public sealed class FlacConstantSubFrame(FlacFrame flacFrame) : FlacSubFrame(flacFrame)
{
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
