/*
 * Date: 2012-11-04
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a MusicMatch tag.
    /// </summary>
    /// This section of the tag was intended to give offsets into the file for each of the five major required sections of the tag.
    /// The offsets, however, are off by 1; for searching a file where the first position is offset 0, the offset given here must be reduced by 1.
    /// In practice, however, these offsets can often be invalid, 
    /// since the data that comes before may be increased or reduces (such as when an ID3v2 tag is appended to the file).
    /// Therefore these offsets are best used to calculate the size of the sections by finding the difference of two consecutive offsets.
    /// Obviously, the size of the audio meta-data section must be calculated in a different manner.
    public sealed partial class MusicMatchTagReader
    {
        private class MusicMatchDataOffsets
        {
            /// <summary>
            /// Gets the image extension offset.
            /// </summary>
            public int ImageExtensionOffset { get; set; }

            /// <summary>
            /// Gets the image binary offset.
            /// </summary>
            public int ImageBinaryOffset { get; set; }

            /// <summary>
            /// Gets the unused offset.
            /// </summary>
            public int UnusedOffset { get; set; }

            /// <summary>
            /// Gets the version info offset.
            /// </summary>
            public int VersionInfoOffset { get; set; }

            /// <summary>
            /// Gets the audio meta data offset.
            /// </summary>
            public int AudioMetaDataOffset { get; set; }

            public long TotalDataOffsetsSize
            {
                get
                {
                    return AudioMetaDataOffset - ImageExtensionOffset;
                }
            }
        }
    }
}
