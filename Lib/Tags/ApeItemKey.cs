/*
 * Date: 2012-11-25
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// A list of pre-defined known <see cref="ApeItem"/> keys in an <see cref="ApeTag"/>.
    /// </summary>
    public enum ApeItemKey
    {
        /// <summary>
        /// Key of the <see cref="ApeTag.AbstractLink"/> item.
        /// </summary>
        Abstract,

        /// <summary>
        /// Key of the <see cref="ApeTag.AlbumArtist"/> item.
        /// </summary>
        AlbumArtist,

        /// <summary>
        /// Key of the <see cref="ApeTag.AlbumName"/> item.
        /// </summary>
        AlbumName,

        /// <summary>
        /// Key of the <see cref="ApeTag.Artist"/> item.
        /// </summary>
        Artist,

        /// <summary>
        /// Key of the <see cref="ApeTag.BarCodeIdentifier"/> item.
        /// </summary>
        BarCodeIdentifier,

        /// <summary>
        /// Key of the <see cref="ApeTag.CatalogNumber"/> item.
        /// </summary>
        CatalogNumber,

        /// <summary>
        /// Key of the <see cref="ApeTag.Conductor"/> item.
        /// </summary>
        Conductor,

        /// <summary>
        /// Key of the <see cref="ApeTag.Comments"/> item.
        /// </summary>
        Comments,

        /// <summary>
        /// Key of the <see cref="ApeTag.Composer"/> item.
        /// </summary>
        Composer,

        /// <summary>
        /// Key of the <see cref="ApeTag.CopyrightHolder"/> item.
        /// </summary>
        CopyrightHolder,

        /// <summary>
        /// Key of the <see cref="ApeTag.DebutAlbumName"/> item.
        /// </summary>
        DebutAlbumName,

        /// <summary>
        /// Key of the <see cref="ApeTag.DiscNumber"/> item.
        /// </summary>
        DiscNumber,

        /// <summary>
        /// Key of the <see cref="ApeTag.DiscographyLink"/> item.
        /// </summary>
        Discography,

        /// <summary>
        /// Key of the <see cref="ApeTag.Dummy"/> item.
        /// </summary>
        Dummy,

        /// <summary>
        /// Key of the <see cref="ApeTag.FileLocation"/> item.
        /// </summary>
        FileLocation,

        /// <summary>
        /// Key of the <see cref="ApeTag.Genre"/> item.
        /// </summary>
        Genre,

        /// <summary>
        /// Key of the <see cref="ApeTag.IndexTimes"/> item.
        /// </summary>
        Index,

        /// <summary>
        /// Key of the <see cref="ApeTag.InternationalStandardBookNumber"/> item.
        /// </summary>
        InternationalStandardBookNumber,

        /// <summary>
        /// Key of the <see cref="ApeTag.InternationalStandardRecordingNumber"/> item.
        /// </summary>
        InternationalStandardRecordingNumber,

        /// <summary>
        /// Key of the <see cref="ApeTag.IntroPlay"/> item.
        /// </summary>
        IntroPlay,

        /// <summary>
        /// Key of the <see cref="ApeTag.LabelCode"/> item.
        /// </summary>
        LabelCode,

        /// <summary>
        /// Key of the <see cref="ApeTag.Language"/> item.
        /// </summary>
        Language,

        /// <summary>
        /// Key of the <see cref="ApeTag.Media"/> item.
        /// </summary>
        Media,

        /// <summary>
        /// Key of the <see cref="ApeTag.PublicationRightHolder"/> item.
        /// </summary>
        PublicationRightHolder,

        /// <summary>
        /// Key of the <see cref="ApeTag.Publisher"/> item.
        /// </summary>
        Publisher,

        /// <summary>
        /// Key of the <see cref="ApeTag.RecordDate"/> item.
        /// </summary>
        RecordDate,

        /// <summary>
        /// Key of the <see cref="ApeTag.RecordLocation"/> item.
        /// </summary>
        RecordLocation,

        /// <summary>
        /// Key of the <see cref="ApeTag.Related"/> item.
        /// </summary>
        Related,

        /// <summary>
        /// Key of the <see cref="ApeTag.ReleaseDate"/> item.
        /// </summary>
        ReleaseDate,

        /// <summary>
        /// Key of the <see cref="ApeTag.ReplayGainAlbumGain"/> item.
        /// </summary>
        ReplayGainAlbumGain,

        /// <summary>
        /// Key of the <see cref="ApeTag.ReplayGainAlbumPeak"/> item.
        /// </summary>
        ReplayGainAlbumPeak,

        /// <summary>
        /// Key of the <see cref="ApeTag.ReplayGainTrackGain"/> item.
        /// </summary>
        ReplayGainTrackGain,

        /// <summary>
        /// Key of the <see cref="ApeTag.ReplayGainTrackPeak"/> item.
        /// </summary>
        ReplayGainTrackPeak,

        /// <summary>
        /// Key of the <see cref="ApeTag.Subtitle"/> item.
        /// </summary>
        Subtitle,

        /// <summary>
        /// Key of the <see cref="ApeTag.Title"/> item.
        /// </summary>
        Title,

        /// <summary>
        /// Key of the <see cref="ApeTag.TrackNumber"/> item.
        /// </summary>
        TrackNumber
    }
}
