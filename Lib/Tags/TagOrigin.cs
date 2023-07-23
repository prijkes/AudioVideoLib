/*
 * Date: 2012-05-28
 * Sources used: 
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Provides the fields that represent the reference points of an <see cref="IAudioTag"/> in a stream.
    /// </summary>
    public enum TagOrigin
    {
        /// <summary>
        /// The tag is is located at the start in the stream.
        /// </summary>
        Start,

        /// <summary>
        /// The tag is located at the end in the stream.
        /// </summary>
        End
    }
}
