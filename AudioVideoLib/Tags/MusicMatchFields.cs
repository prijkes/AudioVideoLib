/*
 * Date: 2012-11-10
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a MusicMatch tag.
    /// </summary>
    public partial class MusicMatchTag
    {
        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="MusicMatchImage"/>.
        /// </summary>
        /// <value>
        /// The <see cref="MusicMatchImage"/>.
        /// </value>
        public MusicMatchImage Image { get; set; }

        /* 
         * The audio meta-data is the heart of the MusicMatch tag.
         * It contains most of the pertinent information found in other tagging formats (song title, album title, artist, etc.) 
         * and some that are unique to this format (mood, preference, situation).
         * 
         * In all versions of the MusicMatch format up to and including 3.00, this section is always 7868 bytes in length.
         * All subsequent versions allowed three possible lengths for this section: 7936, 8004, and 8132 bytes.
         * The conditions under which a particular length from these three possibilities was used is unknown.
         * In all cases, this section is padded with dashes (0x2D) to achieve this constant size.
         * 
         * Due to the great number of fields in this portion of the tag, they are divided amongst the next four sections of the document: 
         * single-line text fields, 
         * non-text fields, 
         * multi-line text fields, 
         * and internet addresses.
         * 
         * This clarification is somewhat arbitrary and somewhat inaccurate (some of the fields described as "non-text" are indeed ASCII strings).
         * However, the clarification does allow for easier description of the meta-data as a whole.
         * At any rate, the actual fields in this section of the tag appear sequentially in the order presented.
         */

        /* * * * * * * * * * * * * * * * * * * * * * * *
         * Single-line text fields
         * 
         * The first group entries in this section of the tag are variable-length ASCII text strings.
         * Each of these strings are preceded by a two-byte field describing the size of the following string (again, in LSB order).
         * Multiple entries in a text field are separated by a semicolon (0x3B).
         * An empty (and non-existent) text field is indicated by a size field of 0 (0x00 0x00).
         * 
         * * * * * * * * * * * * * * * * * * * * * * * */

        /*
         * The first three of these entries are fairly-self explanatory: song title, album title, and artist name.
         */

        /// <summary>
        /// Gets or sets the song title.
        /// </summary>
        /// <value>
        /// The song title.
        /// </value>
        public string SongTitle { get; set; }

        /// <summary>
        /// Gets or sets the album title.
        /// </summary>
        /// <value>
        /// The album title.
        /// </value>
        public string AlbumTitle { get; set; }

        /// <summary>
        /// Gets or sets the name of the artist.
        /// </summary>
        /// <value>
        /// The name of the artist.
        /// </value>
        public string ArtistName { get; set; }

        /*
         * The final five entries are a little less common: Genre, Tempo, Mood, Situation, and Preference.
         * These fields can contain any information, but do to the interface and default set-up for the Jukebox application, 
         * they typically are limited to a subset of possibilities.
         */

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        /// <value>
        /// The genre.
        /// </value>
        /// The Genre entry differs from the ID3v1 tagging format in that it allows a full-text genre description, 
        /// whereas ID3v1 maps a number to a list of genres.
        /// Again, the genre description could be anything, but the interface in Jukebox typically limited most users to the standard ID3v1 genres.
        public string Genre { get; set; }

        /// <summary>
        /// Gets or sets the tempo.
        /// </summary>
        /// <value>
        /// The tempo.
        /// </value>
        /// The Tempo entry is intended to describe the general tempo of the song.
        /// The Jukebox application provided the following defaults: None, Fast, Pretty fast, Moderate, Pretty slow, and Slow.
        public string Tempo { get; set; }

        /// <summary>
        /// Gets or sets the mood.
        /// </summary>
        /// <value>
        /// The mood.
        /// </value>
        /// The Mood entry describes what type of mood the audio establishes: 
        /// Typical values include the following: None, Wild, Upbeat, Morose, Mellow, Tranquil, and Comatose.
        public string Mood { get; set; }

        /// <summary>
        /// Gets or sets the situation.
        /// </summary>
        /// <value>
        /// The situation.
        /// </value>
        /// The Situation entry describes in which situation this music is best played.
        /// Expect the following: None, Dance, Party, Romantic, Dinner, Background, Seasonal, Rave, and Drunken Brawl.
        public string Situation { get; set; }

        /// <summary>
        /// Gets or sets the preference.
        /// </summary>
        /// <value>
        /// The preference.
        /// </value>
        /// The Preference entry allows the user to rate the song.
        /// Possible values include the following: None, Excellent, Very Good, Good, Fair, Poor, and Bad Taste.
        public string Preference { get; set; }

        /* * * * * * * * * * * * * * * * * * * * * * * *
         *  Non-text fields
         *  
         * The next group of fields is described here as "non-text".
         * They are probably better described as entries that are auto-created (i.e., not entered in by a user), 
         * although this isn't entirely accurate, either, as the track number field is determined by user input.
         * At any rate, they've been separated to clarify the presentation of the material.
         * * * * * * * * * * * * * * * * * * * * * * * */

        /// <summary>
        /// Gets or sets the duration of the song.
        /// </summary>
        /// <value>
        /// The duration of the song.
        /// </value>
        /// The "Song duration" entry consists of two fields: a size and text.
        /// The text is formatted as "minutes:seconds", and thus the size field is typically 4 (0x04 0x00).
        public string SongDuration { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        /// <value>
        /// The creation date.
        /// </value>
        /// The only field that is neither a string nor a LSB numerical value is the creation date.
        /// It is 8-byte floating-point value.
        /// It can be interpreted as a TDateTime in the Delphi programming language, 
        /// where the integral portion is the number of elapsed days since 1899-12-30, 
        /// and the mantissa portion represents the fractional portion of that day, 
        /// where .0 would be midnight, .5 would be noon, and .99999... would be just before midnight of the next day.
        /// In practice, this field is typically unused and will be filled with 8 null (0x00) bytes.
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the play counter.
        /// </summary>
        /// <value>
        /// The play counter.
        /// </value>
        /// The next field is the play counter, presumably maintained by the Jukebox application.
        /// Most of the time this field is unused, and is typically 0 (0x00 0x00 0x00 0x00).
        public int PlayCounter { get; set; }

        /// <summary>
        /// Gets or sets the original filename.
        /// </summary>
        /// <value>
        /// The original filename.
        /// </value>
        /// The next entry is a size/text combo and represents the original filename and path.
        /// As these tags were created almost universally on Windows machines, the entries are typically in the form of "C:\path\to\file.mp3".
        public string OriginalFilename { get; set; }

        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        /// <value>
        /// The serial number.
        /// </value>
        /// The next size/text entry is the album serial number fetched from the online CDDB when a track is ripped with MusicMatch.
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        /// <value>
        /// The track number.
        /// </value>
        /// The final field is the track number, usually entered automatically when ripping, encoding, and tagging the audio off from a CD using CDDB.
        public short TrackNumber { get; set; }

        /* * * * * * * * * * * * * * * * * * * * * * * *
         * Multi-line text fields
         * 
         * The next three entries are typically multi-line entries.
         * All line separators use the Windows-standard carriage return (0x0D 0x0A).
         * As with the single-line text entries, the text fields are preceded by LSB size fields
         * * * * * * * * * * * * * * * * * * * * * * * */

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        /// <value>
        /// The notes.
        /// </value>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets the artist bio.
        /// </summary>
        /// <value>
        /// The artist bio.
        /// </value>
        public string ArtistBio { get; set; }

        /// <summary>
        /// Gets or sets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        public string Lyrics { get; set; }

        /* * * * * * * * * * * * * * * * * * * * * * * *
         * Internet addresses
         * 
         * The final group of meta-data are internet addresses.
         * As with other text entries, the text fields are preceded by LSB size fields.
         * * * * * * * * * * * * * * * * * * * * * * * */

        /// <summary>
        /// Gets or sets the artist URL.
        /// </summary>
        /// <value>
        /// The artist URL.
        /// </value>
        public string ArtistUrl { get; set; }

        /// <summary>
        /// Gets or sets the buy cd URL.
        /// </summary>
        /// <value>
        /// The buy cd URL.
        /// </value>
        public string BuyCdUrl { get; set; }

        /// <summary>
        /// Gets or sets the artist email.
        /// </summary>
        /// <value>
        /// The artist email.
        /// </value>
        public string ArtistEmail { get; set; }
    }
}
