/*
 * Date: 2012-11-09
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 *  http://www.mpx.cz/mp3manager/tags.htm
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 tag.
    /// </summary>
    public partial class Lyrics3v2Tag
    {
        /// <summary>
        /// Gets or sets the additional information.
        /// </summary>
        /// <value>
        /// The additional information.
        /// </value>
        public Lyrics3v2TextField AdditionalInformation
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.AdditionalInformation); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the name of the extended album.
        /// </summary>
        /// <value>
        /// The name of the extended album.
        /// </value>
        public Lyrics3v2TextField ExtendedAlbumName
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.ExtendedAlbumName); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the name of the extended artist.
        /// </summary>
        /// <value>
        /// The name of the extended artist.
        /// </value>
        public Lyrics3v2TextField ExtendedArtistName
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.ExtendedArtistName); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the extended track title.
        /// </summary>
        /// <value>
        /// The extended track title.
        /// </value>
        public Lyrics3v2TextField ExtendedTrackTitle
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        /// <value>
        /// The genre.
        /// </value>
        public Lyrics3v2TextField Genre
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.Genre); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the image files.
        /// </summary>
        /// <value>
        /// The image files.
        /// </value>
        public Lyrics3v2ImageFileField ImageFile
        {
            get { return GetField<Lyrics3v2ImageFileField>(); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        public Lyrics3v2LyricsField Lyrics
        {
            get { return GetField<Lyrics3v2LyricsField>(); }
            set { SetField(value); }
        }

        /// <summary>
        /// Gets or sets the lyrics/music author name.
        /// </summary>
        /// <value>
        /// The lyrics/music author name.
        /// </value>
        public Lyrics3v2TextField LyricsAuthorName
        {
            get { return GetField(Lyrics3v2TextFieldIdentifier.LyricsAuthorName); }
            set { SetField(value); }
        }
    }
}
