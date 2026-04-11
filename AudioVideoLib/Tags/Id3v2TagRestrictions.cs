/*
 * Date: 2013-01-12
 * Sources used: 
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// For some applications it might be desired to restrict a tag in more ways than imposed by the Id3v2 specification.
    /// Note that the presence of these restrictions does not affect how the tag is decoded, merely how it was restricted before encoding.
    /// </summary>
    /// <remarks>
    /// Only used for <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/>.
    /// </remarks>
    /// The extended header contains information that can provide further insight in the structure of the tag, 
    /// but is not vital to the correct parsing of the tag information; hence the extended header is optional.
    ///  Restrictions: %ppqrrstt
    public sealed class Id3v2TagRestrictions
    {
        /// <summary>
        /// Tag size restriction flags.
        /// </summary>
        /// pp - Tag size restrictions
        public const int TagSizeRestrictionFlags = 0xC0;

        /// <summary>
        /// Text encoding restriction flags.
        /// </summary>
        /// q - Text encoding restrictions
        public const int TextEncodingRestrictionFlags = 0x20;

        /// <summary>
        /// Text fields size restriction flags.
        /// </summary>
        /// r - Text fields size restrictions
        public const int TextFieldsSizeRestrictionFlags = 0x18;

        /// <summary>
        /// Image encoding restriction flags.
        /// </summary>
        /// s - Image encoding restrictions
        public const int ImageEncodingRestrictionFlags = 0x04;

        /// <summary>
        /// Image size restriction flags.
        /// </summary>
        /// t - Image size restrictions
        public const int ImageSizeRestrictionFlags = 0x03;

        ////------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Gets or sets the tag size restriction.
        /// </summary>
        public Id3v2TagSizeRestriction TagSizeRestriction { get; set; }

        /// <summary>
        /// Gets or sets the text encoding restriction.
        /// </summary>
        /// <value>
        /// The text encoding restriction.
        /// </value>
        public Id3v2TextEncodingRestriction TextEncodingRestriction { get; set; }

        /// <summary>
        /// Gets or sets the text fields size restriction.
        /// </summary>
        /// <value>
        /// The text fields size restriction.
        /// </value>
        public Id3v2TextFieldsSizeRestriction TextFieldsSizeRestriction { get; set; }

        /// <summary>
        /// Gets or sets the image encoding restriction.
        /// </summary>
        /// <value>
        /// The image encoding restriction.
        /// </value>
        public Id3v2ImageEncodingRestriction ImageEncodingRestriction { get; set; }

        /// <summary>
        /// Gets or sets the image size restriction.
        /// </summary>
        /// <value>
        /// The image size restriction.
        /// </value>
        public Id3v2ImageSizeRestriction ImageSizeRestriction { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Places the tag restrictions into a byte array.
        /// </summary>
        /// <returns>A byte array that represents the tag restrictions.</returns>
        public byte[] ToByte()
        {
            return new[]
                {
                    (byte)
                    ((((byte)TagSizeRestriction & TagSizeRestrictionFlags) << 6) |
                     (((byte)TextEncodingRestriction & TextEncodingRestrictionFlags) << 5) |
                     (((byte)TextFieldsSizeRestriction & TextFieldsSizeRestrictionFlags) << 3) |
                     (((byte)ImageEncodingRestriction & ImageEncodingRestrictionFlags) << 2) |
                     ((byte)ImageSizeRestriction & ImageSizeRestrictionFlags))
                };
        }
    }
}
