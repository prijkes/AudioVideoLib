/*
 * Date: 2013-02-16
 * Sources used:
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Picture types.
    /// </summary>
    public enum FlacPictureType
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
        /// Media (e.g. lable side of CD).
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
        /// A bright coloured fish.
        /// </summary>
        BrightColouredFish = 0x11,

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
