namespace AudioVideoLib.Tags;

/// <summary>
/// <see cref="Id3v2RecommendedBufferSizeFrame"/> flags.
/// </summary>
//// %0000000x
public struct Id3v2RecommendedBufferSizeFlags
{
    /// <summary>
    /// If the 'embedded info flag' is true (1) then this indicates 
    /// that an ID3 tag with the maximum size described in 'Buffer size' may occur in the audio stream.
    /// </summary>
    //// Id3v2.2/3/4.0 - x
    public const int EmbeddedInfo = 0x01;
}
