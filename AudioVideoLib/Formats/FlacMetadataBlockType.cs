/*
 * Date: 2013-02-02
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Enum for the metadata block types.
    /// </summary>
    public enum FlacMetadataBlockType
    {
        /// <summary>
        /// The stream info
        /// </summary>
        StreamInfo = 0,

        /// <summary>
        /// The padding
        /// </summary>
        Padding = 1,

        /// <summary>
        /// The application
        /// </summary>
        Application = 2,

        /// <summary>
        /// The seek table
        /// </summary>
        SeekTable = 3,

        /// <summary>
        /// The vorbis comment
        /// </summary>
        VorbisComment = 4,

        /// <summary>
        /// The cue sheet
        /// </summary>
        CueSheet = 5,

        /// <summary>
        /// The picture
        /// </summary>
        Picture = 6,

        /// <summary>
        /// First number of reserved values.
        /// </summary>
        /// <remarks>
        /// All values between <see cref="ReservedFirst"/> and <see cref="ReservedLast"/> are reserved values and should not be used.
        /// </remarks>
        ReservedFirst = 7,

        /// <summary>
        /// Last number of reserved values.
        /// </summary>
        /// <remarks>
        /// All values between <see cref="ReservedFirst"/> and <see cref="ReservedLast"/> are reserved values and should not be used.
        /// </remarks>
        ReservedLast = 126,

        /// <summary>
        /// Invalid, to avoid confusion with a frame sync code.
        /// </summary>
        Invalid = 127
    }
}
