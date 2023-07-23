/*
 * Date: 2011-11-28
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 *  http://forum.musepack.net/showthread.php?t=46
 */
using System;
using System.IO;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTag
    {
        /// <summary>
        /// Gets or sets an abstract <see cref="ApeLocatorItem"/>.
        /// </summary>
        /// <value>
        /// An abstract <see cref="ApeLocatorItem"/>.
        /// </value>
        public ApeLocatorItem AbstractLink
        {
            get { return GetItem(ApeItemKey.Abstract) as ApeLocatorItem; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the artist of the album.
        /// </summary>
        /// <value>
        /// The album artist.
        /// </value>
        public ApeUtf8Item AlbumArtist
        {
            get { return GetItem(ApeItemKey.AlbumArtist) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the name of the album.
        /// </summary>
        /// <value>
        /// The album name.
        /// </value>
        public ApeUtf8Item AlbumName
        {
            get { return GetItem(ApeItemKey.AlbumName) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>
        /// The artist.
        /// </value>
        /// <remarks>
        /// The artist can be one or more performing artists.
        /// </remarks>
        public ApeUtf8Item Artist
        {
            get { return GetItem(ApeItemKey.Artist) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets a bar code identifier.
        /// </summary>
        /// <value>
        /// The bar code identifier.
        /// </value>
        /// <remarks>
        /// The bar code identifier should an EAN-13/UPC-A bar code identifier.
        /// </remarks>
        public ApeUtf8Item BarCodeIdentifier
        {
            get { return GetItem(ApeItemKey.BarCodeIdentifier) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the catalog number.
        /// </summary>
        /// <value>
        /// The catalog number.
        /// </value>
        /// <remarks>
        /// The EAN/UPC or the labels catalog number for this media.
        /// </remarks>
        public ApeUtf8Item CatalogNumber
        {
            get { return GetItem(ApeItemKey.CatalogNumber) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets user comments.
        /// </summary>
        /// <value>
        /// The user comments.
        /// </value>
        /// <remarks>
        /// One or more comments can be stored using this field.
        /// </remarks>
        public ApeUtf8Item Comments
        {
            get { return GetItem(ApeItemKey.Comments) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the composer.
        /// </summary>
        /// <value>
        /// The composer.
        /// </value>
        /// <remarks>
        /// Name of the original composer, or name of the original arranger.
        /// </remarks>
        public ApeUtf8Item Composer
        {
            get { return GetItem(ApeItemKey.Composer) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the conductor.
        /// </summary>
        /// <value>
        /// The conductor.
        /// </value>
        /// <remarks>
        /// Name of the conductor.
        /// </remarks>
        public ApeUtf8Item Conductor
        {
            get { return GetItem(ApeItemKey.Conductor) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the copyright holder.
        /// </summary>
        /// <value>
        /// The copyright holder.
        /// </value>
        public ApeUtf8Item CopyrightHolder
        {
            get { return GetItem(ApeItemKey.CopyrightHolder) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the name of the debut album.
        /// </summary>
        /// <value>
        /// The debut album name.
        /// </value>
        public ApeUtf8Item DebutAlbumName
        {
            get { return GetItem(ApeItemKey.DebutAlbumName) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the disc number.
        /// </summary>
        /// <value>
        /// The disc number.
        /// </value>
        public ApeUtf8Item DiscNumber
        {
            get { return GetItem(ApeItemKey.DiscNumber) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets a link to the discography.
        /// </summary>
        /// <value>
        /// The discography link.
        /// </value>
        /// <remarks>
        /// A link to a page containing the discography.
        /// </remarks>
        public ApeLocatorItem DiscographyLink
        {
            get { return GetItem(ApeItemKey.Discography) as ApeLocatorItem; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the dummy.
        /// </summary>
        /// <value>
        /// The dummy.
        /// </value>
        /// <remarks>
        /// This field is a place holder.
        /// </remarks>
        public ApeBinaryItem Dummy
        {
            get { return GetItem(ApeItemKey.Dummy) as ApeBinaryItem; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the location of the file.
        /// </summary>
        /// <value>
        /// The file location.
        /// </value>
        /// <remarks>
        /// The location of the file.
        /// </remarks>
        public ApeLocatorItem FileLocation
        {
            get { return GetItem(ApeItemKey.FileLocation) as ApeLocatorItem; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        /// <value>
        /// The genre.
        /// </value>
        /// <remarks>
        /// Genre keywords should be normally English terms.
        /// A native language supporting plugin can translate common expression to the local language and vice versa when storing the genre in the file.
        /// </remarks>
        public ApeUtf8Item Genre
        {
            get { return GetItem(ApeItemKey.Genre) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the index times.
        /// </summary>
        /// <value>
        /// The index times.
        /// </value>
        /// <remarks>
        /// Indexes of time for quick access.
        /// </remarks>
        public ApeUtf8Item IndexTimes
        {
            get { return GetItem(ApeItemKey.Index) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the intro play.
        /// </summary>
        /// <value>
        /// The intro play.
        /// </value>
        /// <remarks>
        /// Characteristic part of piece for intro playing.
        /// </remarks>
        public ApeUtf8Item IntroPlay
        {
            get { return GetItem(ApeItemKey.IntroPlay) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the international standard book number.
        /// </summary>
        /// <value>
        /// The international standard book number.
        /// </value>
        /// <remarks>
        /// ISBN10 number with check digit.
        /// <para />
        /// Use <see cref="IsValidIsbn10"/> to see if a value is a valid ISBN10 number.
        /// </remarks>
        public ApeUtf8Item InternationalStandardBookNumber
        {
            get
            {
                return GetItem(ApeItemKey.InternationalStandardBookNumber) as ApeUtf8Item;
            }

            set
            {
                if (value == null)
                {
                    RemoveItem(InternationalStandardBookNumber);
                }
                else
                {
                    if (!value.Values.All(IsValidIsbn10))
                        throw new InvalidDataException("One or more invalid ISBN10 values found.");

                    SetItem(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the international standard recording number.
        /// </summary>
        /// <value>
        /// The international standard recording number.
        /// </value>
        /// <remarks>
        /// International Standard Recording Number.
        /// </remarks>
        public ApeUtf8Item InternationalStandardRecordingNumber
        {
            get { return GetItem(ApeItemKey.InternationalStandardRecordingNumber) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the label code.
        /// </summary>
        /// <value>
        /// The label code.
        /// </value>
        /// <remarks>
        /// Label Code.
        /// </remarks>
        public ApeUtf8Item LabelCode
        {
            get { return GetItem(ApeItemKey.LabelCode) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>
        /// The language.
        /// </value>
        /// <remarks>
        /// Used Language(s) for music/spoken words
        /// </remarks>
        public ApeUtf8Item Language
        {
            get { return GetItem(ApeItemKey.Language) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the media.
        /// </summary>
        /// <value>
        /// The media.
        /// </value>
        /// <remarks>
        /// Source, Source Media Number/Total Media Number, Source Time.
        /// </remarks>
        public ApeUtf8Item Media
        {
            get { return GetItem(ApeItemKey.Media) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the publication right holder.
        /// </summary>
        /// <value>
        /// The publication right holder.
        /// </value>
        public ApeUtf8Item PublicationRightHolder
        {
            get { return GetItem(ApeItemKey.PublicationRightHolder) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the publisher.
        /// </summary>
        /// <value>
        /// The publisher.
        /// </value>
        /// <remarks>
        /// Record label or publisher.
        /// </remarks>
        public ApeUtf8Item Publisher
        {
            get { return GetItem(ApeItemKey.Publisher) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the record date.
        /// </summary>
        /// <value>
        /// The record date.
        /// </value>
        /// <remarks>
        /// Record date.
        /// </remarks>
        public ApeUtf8Item RecordDate
        {
            get { return GetItem(ApeItemKey.RecordDate) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the record location.
        /// </summary>
        /// <value>
        /// The record location.
        /// </value>
        /// <remarks>
        /// Record location(s).
        /// </remarks>
        public ApeUtf8Item RecordLocation
        {
            get { return GetItem(ApeItemKey.RecordLocation) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the related.
        /// </summary>
        /// <value>
        /// The related.
        /// </value>
        /// <remarks>
        /// Location of related information.
        /// </remarks>
        public ApeLocatorItem Related
        {
            get { return GetItem(ApeItemKey.Related) as ApeLocatorItem; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        /// <value>
        /// The release date.
        /// </value>
        /// <remarks>
        /// Release date.
        /// </remarks>
        public ApeUtf8Item ReleaseDate
        {
            get { return GetItem(ApeItemKey.ReleaseDate) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the replay gain album gain.
        /// </summary>
        /// <value>
        /// The replay gain album gain.
        /// </value>
        public ApeUtf8Item ReplayGainAlbumGain
        {
            get { return GetItem(ApeItemKey.ReplayGainAlbumGain) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the replay gain album peak.
        /// </summary>
        /// <value>
        /// The replay gain album peak.
        /// </value>
        public ApeUtf8Item ReplayGainAlbumPeak
        {
            get { return GetItem(ApeItemKey.ReplayGainAlbumPeak) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the replay gain track gain.
        /// </summary>
        /// <value>
        /// The replay gain track gain.
        /// </value>
        public ApeUtf8Item ReplayGainTrackGain
        {
            get { return GetItem(ApeItemKey.ReplayGainTrackGain) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the replay gain track peak.
        /// </summary>
        /// <value>
        /// The replay gain track peak.
        /// </value>
        public ApeUtf8Item ReplayGainTrackPeak
        {
            get { return GetItem(ApeItemKey.ReplayGainTrackPeak) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the subtitle.
        /// </summary>
        /// <value>
        /// The subtitle.
        /// </value>
        /// <remarks>
        /// Title when TITLE contains the work or additional sub title.
        /// </remarks>
        public ApeUtf8Item Subtitle
        {
            get { return GetItem(ApeItemKey.Subtitle) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        /// <remarks>
        /// Music Piece Title, Music Work.
        /// </remarks>
        public ApeUtf8Item Title
        {
            get { return GetItem(ApeItemKey.Title) as ApeUtf8Item; }
            set { SetItem(value); }
        }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        /// <value>
        /// The track number.
        /// </value>
        public ApeUtf8Item TrackNumber
        {
            get { return GetItem(ApeItemKey.TrackNumber) as ApeUtf8Item; }
            set { SetItem(value); }
        }
    }
}
