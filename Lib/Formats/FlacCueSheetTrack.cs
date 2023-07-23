/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System.Collections.Generic;
using System.Linq;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Class to contain a FLAC cue sheet track.
    /// </summary>
    public class FlacCueSheetTrack
    {
        private readonly List<FlacCueSheetTrackIndexPoint> _trackIndexPoints = new List<FlacCueSheetTrackIndexPoint>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="FlacCueSheetTrack" /> class.
        /// </summary>
        /// <param name="trackOffset">The track offset.</param>
        /// <param name="trackNumber">The track number.</param>
        /// <param name="trackIsrc">The international recording code of the track.</param>
        /// <param name="trackType">Type of the track.</param>
        /// <param name="preEmphasis">The pre emphasis.</param>
        /// <param name="reserved">The reserved.</param>
        /// <param name="trackIndexCount">The track index count.</param>
        /// <param name="indexPoints">The index points.</param>
        public FlacCueSheetTrack(long trackOffset, int trackNumber, string trackIsrc, FlacCueSheetTrackType trackType, FlacCueSheetPreEmphasis preEmphasis, byte[] reserved, int trackIndexCount, IEnumerable<FlacCueSheetTrackIndexPoint> indexPoints)
        {
            TrackOffset = trackOffset;
            TrackNumber = trackNumber;
            TrackIsrc = trackIsrc;
            TrackType = trackType;
            PreEmphasis = preEmphasis;
            Reserved = reserved;
            TrackIndexCount = trackIndexCount;
            _trackIndexPoints = indexPoints.ToList();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the track offset in samples, relative to the beginning of the FLAC audio stream.
        /// </summary>
        /// <value>
        /// The track offset in samples, relative to the beginning of the FLAC audio stream.
        /// </value>
        /// <remarks>
        /// Track offset is the offset to the first index point of the track.
        /// (Note how this differs from CD-DA, where the track's offset in the TOC is that of the track's INDEX 01 even if there is an INDEX 00.)
        /// For CD-DA, the offset must be evenly divisible by 588 samples (588 samples = 44100 samples/sec * 1/75th of a sec).
        /// </remarks>
        public long TrackOffset { get; private set; }

        /// <summary>
        /// Gets the track number.
        /// </summary>
        /// <value>
        /// The track number.
        /// </value>
        /// <remarks>
        /// A track number of 0 is not allowed to avoid conflicting with the CD-DA spec, which reserves this for the lead-in.
        /// For CD-DA the number must be 1-99, or 170 for the lead-out; for non-CD-DA, the track number must for 255 for the lead-out.
        /// It is not required but encouraged to start with track 1 and increase sequentially.
        /// Track numbers must be unique within a CUESHEET.
        /// </remarks>
        public int TrackNumber { get; private set; }

        /// <summary>
        /// Gets the International Standard Recording Code of the track.
        /// </summary>
        /// <value>
        /// The International Standard Recording Code of the track.
        /// </value>
        /// <remarks>
        /// This is a 12-digit alphanumeric code.
        /// A value of 12 ASCII NUL characters may be used to denote absence of an ISRC.
        /// </remarks>
        public string TrackIsrc { get; private set; }

        /// <summary>
        /// Gets the type of the track.
        /// </summary>
        /// <value>
        /// The type of the track.
        /// </value>
        public FlacCueSheetTrackType TrackType { get; private set; }

        /// <summary>
        /// Gets the pre emphasis.
        /// </summary>
        /// <value>
        /// The pre emphasis.
        /// </value>
        public FlacCueSheetPreEmphasis PreEmphasis { get; private set; }

        /// <summary>
        /// Gets the reserved.
        /// </summary>
        /// <value>
        /// The reserved.
        /// </value>
        /// <remarks>
        /// All bits must be set to zero.
        /// </remarks>
        public byte[] Reserved { get; private set; }

        /// <summary>
        /// Gets the number of track index points.
        /// </summary>
        /// <value>
        /// The number of track index points.
        /// </value>
        /// <remarks>
        /// There must be at least one index in every track in a CUESHEET except for the lead-out track, which must have zero.
        /// For CD-DA, this number may be no more than 100.
        /// </remarks>
        public int TrackIndexCount { get; private set; }

        /// <summary>
        /// Gets the track index points.
        /// </summary>
        /// <value>
        /// The track index points.
        /// </value>
        /// <remarks>
        /// For all tracks except the lead-out track, one or more track index points.
        /// </remarks>
        public IEnumerable<FlacCueSheetTrackIndexPoint> TrackIndexPoints
        {
            get
            {
                return _trackIndexPoints.AsReadOnly();
            }
        }
    }
}
