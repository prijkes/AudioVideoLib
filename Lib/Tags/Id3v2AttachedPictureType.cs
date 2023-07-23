/*
 * Date: 2011-11-05
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
    /// Picture types an <see cref="Id3v2AttachedPictureFrame"/> can have.
    /// </summary>
    public enum Id3v2AttachedPictureType
    {
        /// <summary>
        /// Other type.
        /// </summary>
        Other = 0x00,

        /// <summary>
        /// 32x32 pixels 'file icon' (PNG only).
        /// </summary>
        FileIcon = 0x01,

        /// <summary>
        /// Other file icon.
        /// </summary>
        OtherFileIcon = 0x02,

        /// <summary>
        /// Cover (front).
        /// </summary>
        CoverFront = 0x03,

        /// <summary>
        /// Cover (back).
        /// </summary>
        CoverBack = 0x04,

        /// <summary>
        /// Leaflet page.
        /// </summary>
        LeafletPage = 0x05,

        /// <summary>
        /// Media (e.g. label side of CD).
        /// </summary>
        Media = 0x06,

        /// <summary>
        /// Lead artist/lead performer/soloist.
        /// </summary>
        LeadArtist = 0x07,

        /// <summary>
        /// Artist / performer.
        /// </summary>
        ArtistPerformer = 0x08,

        /// <summary>
        /// Conductor picture type.
        /// </summary>
        Conductor = 0x09,

        /// <summary>
        /// Band / Orchestra.
        /// </summary>
        Band = 0x0A,

        /// <summary>
        /// Composer picture type.
        /// </summary>
        Composer = 0x0B,

        /// <summary>
        /// Lyricist / text writer.
        /// </summary>
        TextWriter = 0x0C,

        /// <summary>
        /// Recording Location.
        /// </summary>
        RecordingLocation = 0x0D,

        /// <summary>
        /// During recording.
        /// </summary>
        DuringRecording = 0x0E,

        /// <summary>
        /// During performance.
        /// </summary>
        DuringPerformance = 0x0F,

        /// <summary>
        /// Movie/video screen capture.
        /// </summary>
        VideoScreenCapture = 0x10,

        /// <summary>
        /// A bright colored fish.
        /// </summary>
        BrightColoredFish = 0x11,

        /// <summary>
        /// Illustration picture type.
        /// </summary>
        Illustration = 0x12,

        /// <summary>
        /// Band / artist logotype.
        /// </summary>
        BandLogoType = 0x13,

        /// <summary>
        /// Publisher / Studio logotype.
        /// </summary>
        PublisherLogoType = 0x14
    }
}
