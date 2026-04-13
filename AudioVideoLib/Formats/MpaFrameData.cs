namespace AudioVideoLib.Formats;

/// <summary>
/// Public class for MPEG audio frames.
/// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
/// </summary>
public sealed partial class MpaFrame
{
    /// <summary>
    /// Gets the audio data.
    /// </summary>
    /// <returns>The audio data.</returns>
    public byte[] AudioData { get; private set; } = null!;

    /// <summary>
    /// Gets the length of the audio data, in bytes.
    /// </summary>
    /// <returns>The length of the audio data, in bytes.</returns>
    public int AudioDataLength
    {
        get { return AudioData.Length; }
    }

    ///// <summary>
    ///// Gets a value indicating whether the frame data is valid.
    ///// </summary>
    ///// <returns><c>true</c> if the frame data is valid; otherwise, <c>false</c>.</returns>
    ////public bool IsValidAudioData
    ////{
    ////    get { return true; }
    ////}
}
