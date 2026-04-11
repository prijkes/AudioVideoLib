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
    /// <see cref="Id3v2Version.Id3v240"/> <see cref="Id3v2Tag"/> size restrictions.
    /// </summary>  
    public enum Id3v2TagSizeRestriction
    {
        /// <summary>
        /// No more than 128 frames and 1 MB total tag size.
        /// </summary>
        Max128FramesAnd1024KbTotalSize = 0x00,

        /// <summary>
        /// No more than 64 frames and 128 KB total tag size.
        /// </summary>
        Max64FramesAnd128KbTotalSize = 0x01,

        /// <summary>
        /// No more than 32 frames and 40 KB total tag size.
        /// </summary>
        Max32FramesAnd40KbTotalSize = 0x02,

        /// <summary>
        /// No more than 32 frames and 4 KB total tag size.
        /// </summary>
        Max32FramesAnd4KbTotalSize = 0x03
    }
}
