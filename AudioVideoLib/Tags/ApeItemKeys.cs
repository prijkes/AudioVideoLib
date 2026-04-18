namespace AudioVideoLib.Tags;

using System.Collections.Generic;

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
    private static readonly Dictionary<ApeItemKey, string[]> ItemKeys = new()
    {
            { ApeItemKey.Abstract, ["Abstract"] },
            { ApeItemKey.AlbumArtist, ["Album artist", "ALBUMARTIST"] },
            { ApeItemKey.AlbumName, ["Album"] },
            { ApeItemKey.Artist, ["Artist"] },
            { ApeItemKey.BarCodeIdentifier, ["EAN/UPC"] },
            { ApeItemKey.CatalogNumber, ["Catalog"] },
            { ApeItemKey.Comments, ["Comment"] },
            { ApeItemKey.Composer, ["Composer"] },
            { ApeItemKey.Conductor, ["Conductor"] },
            { ApeItemKey.CopyrightHolder, ["Copyright"] },
            { ApeItemKey.DebutAlbumName, ["Debut album"] },
            { ApeItemKey.DiscNumber, ["Disc", "DISCNUMBER"] },
            { ApeItemKey.Discography, ["Bibliography"] },
            { ApeItemKey.Dummy, ["Dummy"] },
            { ApeItemKey.FileLocation, ["File"] },
            { ApeItemKey.Genre, ["Genre"] },
            { ApeItemKey.Index, ["Index"] },
            { ApeItemKey.IntroPlay, ["IntroPlay"] },
            { ApeItemKey.InternationalStandardBookNumber, ["ISBN"] },
            { ApeItemKey.InternationalStandardRecordingNumber, ["ISRC"] },
            { ApeItemKey.LabelCode, ["LC"] },
            { ApeItemKey.Language, ["Language"] },
            { ApeItemKey.Media, ["Media"] },
            { ApeItemKey.PublicationRightHolder, ["Publicationright"] },
            { ApeItemKey.Publisher, ["Publisher"] },
            { ApeItemKey.RecordDate, ["Record Date"] },
            { ApeItemKey.RecordLocation, ["Record Location"] },
            { ApeItemKey.ReplayGainAlbumGain, ["REPLAYGAIN_ALBUM_GAIN"] },
            { ApeItemKey.ReplayGainAlbumPeak, ["REPLAYGAIN_ALBUM_PEAK"] },
            { ApeItemKey.ReplayGainTrackGain, ["REPLAYGAIN_TRACK_GAIN"] },
            { ApeItemKey.ReplayGainTrackPeak, ["REPLAYGAIN_TRACK_PEAK"] },
            { ApeItemKey.Related, ["Related"] },
            { ApeItemKey.ReleaseDate, ["Year", "DATE"] },
            { ApeItemKey.Subtitle, ["Subtitle"] },
            { ApeItemKey.Title, ["Title"] },
            { ApeItemKey.TrackNumber, ["Track", "TRACKNUMBER", "TRACKNUM"] }
        };
}
