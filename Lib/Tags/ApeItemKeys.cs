/*
 * Date: 2012-12-08
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */
using System.Collections.Generic;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeTag"/> item.
    /// </summary>
    public partial class ApeItem
    {
        // Seems that people like to change names?
        // Original list: http://wiki.hydrogenaudio.org/index.php?title=APE_key
        // 'Extended' list: http://taglib.github.com/api/classTagLib_1_1APE_1_1Tag.html#af77a10659fbb0018228420ad6de501e1
        // 'Extended' list: http://wiki.slimdevices.com/index.php/SlimServerSupportedTags
        // 'Extended' list: http://wiki.musicbrainz.org/PicardTagMapping
        // Discussion: http://www.hydrogenaudio.org/forums/index.php?showtopic=92623
        private static readonly Dictionary<ApeItemKey, string[]> ItemKeys = new Dictionary<ApeItemKey, string[]>
            {
                { ApeItemKey.Abstract, new[] { "Abstract" } },
                { ApeItemKey.AlbumArtist, new[] { "Album artist", "ALBUMARTIST" } },
                { ApeItemKey.AlbumName, new[] { "Album" } },
                { ApeItemKey.Artist, new[] { "Artist" } },
                { ApeItemKey.BarCodeIdentifier, new[] { "EAN/UPC" } },
                { ApeItemKey.CatalogNumber, new[] { "Catalog" } },
                { ApeItemKey.Comments, new[] { "Comment" } },
                { ApeItemKey.Composer, new[] { "Composer" } },
                { ApeItemKey.Conductor, new[] { "Conductor" } },
                { ApeItemKey.CopyrightHolder, new[] { "Copyright" } },
                { ApeItemKey.DebutAlbumName, new[] { "Debut album" } },
                { ApeItemKey.DiscNumber, new[] { "Disc", "DISCNUMBER" } },
                { ApeItemKey.Discography, new[] { "Bibliography" } },
                { ApeItemKey.Dummy, new[] { "Dummy" } },
                { ApeItemKey.FileLocation, new[] { "File" } },
                { ApeItemKey.Genre, new[] { "Genre" } },
                { ApeItemKey.Index, new[] { "Index" } },
                { ApeItemKey.IntroPlay, new[] { "IntroPlay" } },
                { ApeItemKey.InternationalStandardBookNumber, new[] { "ISBN" } },
                { ApeItemKey.InternationalStandardRecordingNumber, new[] { "ISRC" } },
                { ApeItemKey.LabelCode, new[] { "LC" } },
                { ApeItemKey.Language, new[] { "Language" } },
                { ApeItemKey.Media, new[] { "Media" } },
                { ApeItemKey.PublicationRightHolder, new[] { "Publicationright" } },
                { ApeItemKey.Publisher, new[] { "Publisher" } },
                { ApeItemKey.RecordDate, new[] { "Record Date" } },
                { ApeItemKey.RecordLocation, new[] { "Record Location" } },
                { ApeItemKey.ReplayGainAlbumGain, new[] { "REPLAYGAIN_ALBUM_GAIN" } },
                { ApeItemKey.ReplayGainAlbumPeak, new[] { "REPLAYGAIN_ALBUM_PEAK" } },
                { ApeItemKey.ReplayGainTrackGain, new[] { "REPLAYGAIN_TRACK_GAIN" } },
                { ApeItemKey.ReplayGainTrackPeak, new[] { "REPLAYGAIN_TRACK_PEAK" } },
                { ApeItemKey.Related, new[] { "Related" } },
                { ApeItemKey.ReleaseDate, new[] { "Year", "DATE" } },
                { ApeItemKey.Subtitle, new[] { "Subtitle" } },
                { ApeItemKey.Title, new[] { "Title" } },
                { ApeItemKey.TrackNumber, new[] { "Track", "TRACKNUMBER", "TRACKNUM" } }
            };
    }
}
