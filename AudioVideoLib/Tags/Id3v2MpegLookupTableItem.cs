namespace AudioVideoLib.Tags;

/// <summary>
/// An MPEG lookup table item in the <see cref="Id3v2MpegLocationLookupTableFrame"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2MpegLookupTableItem"/> class.
/// </remarks>
/// <param name="deviationInBytes">The deviation in bytes.</param>
/// <param name="devationInMilliseconds">The deviation in milliseconds.</param>
public class Id3v2MpegLookupTableItem(byte deviationInBytes, byte devationInMilliseconds)
{

    /// <summary>
    /// Gets the deviation in bytes.
    /// </summary>
    /// <remarks>
    /// Each reference consists of two parts; a certain number of bits, as defined in bits for bytes deviation,
    /// that describes the difference between what is said in bytes between reference and the reality and a certain number of bits,
    /// as defined in bits for milliseconds deviation,
    /// that describes the difference between what is said in milliseconds between reference and the reality.
    /// The number of bits in every reference, i.e. bits for bytes deviation + bits for milliseconds deviation,
    /// must be a multiple of four.
    /// </remarks>
    public byte DeviationInBytes { get; private set; } = deviationInBytes;

    /// <summary>
    /// Gets the deviation in milliseconds.
    /// </summary>
    /// <remarks>
    /// Each reference consists of two parts; a certain number of bits, as defined in bits for bytes deviation, 
    /// that describes the difference between what is said in bytes between reference and the reality and a certain number of bits, 
    /// as defined in bits for milliseconds deviation, 
    /// that describes the difference between what is said in milliseconds between reference and the reality.
    /// The number of bits in every reference, i.e. bits for bytes deviation + bits for milliseconds deviation, 
    /// must be a multiple of four.
    /// </remarks>
    public byte DeviationInMilliseconds { get; private set; } = devationInMilliseconds;
}
