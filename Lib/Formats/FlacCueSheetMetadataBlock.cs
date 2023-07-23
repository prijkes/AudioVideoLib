/*
 * Date: 2013-02-16
 * Sources used: 
 *  http://xiph.org/flac/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.Collections.Generic;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// 
    /// </summary>
    public class FlacCueSheetMetadataBlock : FlacMetadataBlock
    {
        private readonly List<FlacCueSheetTrack> _tracks = new List<FlacCueSheetTrack>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override FlacMetadataBlockType BlockType
        {
            get
            {
                return FlacMetadataBlockType.CueSheet;
            }
        }

        /// <inheritdoc/>
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteString(MediaCatalogNumber);
                    stream.WriteBigEndianInt64(LeadInSampleCount);
                    stream.WriteByte(Convert.ToByte(IsCompactDisc));
                    stream.WritePadding(0x00, 256);
                    stream.WriteByte((byte)TrackCount);
                    foreach (FlacCueSheetTrack track in _tracks)
                    {
                        stream.WriteBigEndianInt64(track.TrackOffset);
                        stream.WriteByte((byte)track.TrackNumber);
                        stream.WriteString(track.TrackIsrc);
                        stream.WriteByte((byte)(((byte)track.TrackType) & (((byte)track.PreEmphasis) << 1)));
                        stream.WritePadding(0x00, 13);
                        stream.WriteByte((byte)track.TrackIndexCount);
                        foreach (FlacCueSheetTrackIndexPoint trackIndexPoint in track.TrackIndexPoints)
                        {
                            stream.WriteBigEndianInt64(trackIndexPoint.Offset);
                            stream.WriteByte((byte)trackIndexPoint.IndexPointNumber);
                            stream.WritePadding(0x00, 3);
                        }
                    }
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    MediaCatalogNumber = stream.ReadString(128);
                    LeadInSampleCount = stream.ReadBigEndianInt64();
                    IsCompactDisc = (stream.ReadByte() == 1);
                    Reserved = new byte[258];
                    stream.Read(Reserved, 258);
                    TrackCount = stream.ReadByte();
                    for (int i = 0; (i < TrackCount) && (stream.Position < stream.Length); i++)
                    {
                        long trackOffset = stream.ReadBigEndianInt64();
                        int trackNumber = stream.ReadByte();
                        string trackIsrc = stream.ReadString(12);
                        int flags = stream.ReadByte();
                        FlacCueSheetTrackType trackType = (FlacCueSheetTrackType)(flags & 0x01);
                        FlacCueSheetPreEmphasis preEmphasis = (FlacCueSheetPreEmphasis)(flags & 0x02);
                        byte[] reserved = new byte[13];
                        stream.Read(reserved, 13);
                        int trackIndexCount = stream.ReadByte();
                        List<FlacCueSheetTrackIndexPoint> indexPoints = new List<FlacCueSheetTrackIndexPoint>();
                        for (int y = 0; (y < trackIndexCount) && (stream.Position < stream.Length); y++)
                        {
                            long offset = stream.ReadBigEndianInt64();
                            int indexPointNumber = stream.ReadByte();
                            reserved = new byte[3];
                            stream.Read(reserved, 3);
                            indexPoints.Add(new FlacCueSheetTrackIndexPoint(offset, indexPointNumber, reserved));
                        }
                        _tracks.Add(
                            new FlacCueSheetTrack(trackOffset, trackNumber, trackIsrc, trackType, preEmphasis, reserved, trackIndexCount, indexPoints));
                    }
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the media catalog number.
        /// </summary>
        /// <value>
        /// The media catalog number.
        /// </value>
        /// <remarks>
        /// The media catalog number is in ASCII printable characters 0x20-0x7e.
        /// In general, the media catalog number may be 0 to 128 bytes long; any unused characters should be right-padded with NUL characters.
        /// For CD-DA, this is a thirteen digit number, followed by 115 NUL bytes.
        /// </remarks>
        public string MediaCatalogNumber { get; private set; }

        /// <summary>
        /// Gets the number of lead-in samples.
        /// </summary>
        /// <value>
        /// The number of lead-in samples.
        /// </value>
        /// <remarks>
        /// This field has meaning only for CD-DA cuesheets; for other uses it should be 0.
        /// For CD-DA, the lead-in is the TRACK 00 area where the table of contents is stored; 
        /// more precisely, it is the number of samples from the first sample of the media to the first sample of the first index point of the first track.
        /// According to the Red Book, the lead-in must be silence and CD grabbing software does not usually store it; additionally, the lead-in must be at least two seconds but may be longer.
        /// For these reasons the lead-in length is stored here so that the absolute position of the first track can be computed.
        /// Note that the lead-in stored here is the number of samples up to the first index point of the first track, not necessarily to INDEX 01 of the first track; even the first track may have INDEX 00 data.
        /// </remarks>
        public long LeadInSampleCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance corresponds to a compact disc.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance corresponds to a compact disc; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompactDisc { get; private set; }

        /// <summary>
        /// Gets or sets the reserved.
        /// </summary>
        /// <value>
        /// The reserved.
        /// </value>
        /// <remarks>
        /// All bits must be set to zero.
        /// </remarks>
        public byte[] Reserved { get; private set; }

        /// <summary>
        /// Gets the number of tracks.
        /// </summary>
        /// <value>
        /// The number of tracks.
        /// </value>
        /// <remarks>
        /// Must be at least 1 (because of the requisite lead-out track).
        /// For CD-DA, this number must be no more than 100 (99 regular tracks and one lead-out track).
        /// </remarks>
        public int TrackCount { get; private set; }

        /// <summary>
        /// Gets the tracks.
        /// </summary>
        /// <value>
        /// The tracks.
        /// </value>
        public IEnumerable<FlacCueSheetTrack> Tracks
        {
            get
            {
                return _tracks.AsReadOnly();
            }
        }
    }
}
