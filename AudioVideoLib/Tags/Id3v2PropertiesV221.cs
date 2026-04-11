/*
 * Date: 2012-12-03
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
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        /// <summary>
        /// Gets or sets the audio seek point index.
        /// </summary>
        /// <value>
        /// The audio seek point index.
        /// </value>
        /// <remarks>
        /// This frame is specific to <see cref="Id3v2Version.Id3v221"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2CompressedDataMetaFrame> CompressedDataMeta
        {
            get
            {
                return GetFrameCollection<Id3v2CompressedDataMetaFrame>();
            }

            set
            {
                RemoveFrames<Id3v2CompressedDataMetaFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }
    }
}
