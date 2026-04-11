/*
 * Date: 2011-06-25
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.IO;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        /// <summary>
        /// Gets or sets the album title.
        /// </summary>
        /// <value>
        /// The album titles.
        /// </value>
        /// <remarks>
        /// The 'Album/Movie/Show title' frame is intended for the title of the recording (or source of sound)
        /// from which the audio in the file is taken.
        /// </remarks>
        public Id3v2TextFrame AlbumTitle
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.AlbumTitle);
            }

            set
            {
                if (value == null)
                    RemoveFrame(AlbumTitle);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>
        /// The artists.
        /// </value>
        /// <remarks>
        /// The 'Lead artist(s)/Lead performer(s)/Soloist(s)/Performing group' is used for the main artist(s).
        /// </remarks>
        public Id3v2TextFrame Artist
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.Artist);
            }

            set
            {
                if (value == null)
                    RemoveFrame(Artist);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the artist extra.
        /// </summary>
        /// <value>
        /// The artists extra.
        /// </value>
        /// <remarks>
        /// The 'Band/Orchestra/Accompaniment' frame is used for additional information about the performers in the recording.
        /// </remarks>
        public Id3v2TextFrame ArtistExtra
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ArtistExtra);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ArtistExtra);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the size of the audio.
        /// </summary>
        /// <value>
        /// The sizes of the audio.
        /// </value>
        /// <remarks>
        /// The 'Size' frame contains the size of the audio file in bytes excluding the tag, represented as a numeric string.
        /// <para/>
        /// This frame has been removed as of <see cref="Id3v2Version.Id3v240"/>.
        /// The information contained in this frame is in the general case either trivial to calculate for the player or impossible for the tagger to calculate.
        /// There is however no good use for such information.
        /// The frame is therefore completely deprecated.
        /// </remarks>
        public Id3v2TextFrame AudioSize
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.AudioSize) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                {
                    RemoveFrame(AudioSize);
                    return;
                }

                if ((value.Version < Id3v2Version.Id3v240) && (value.TextEncoding != Id3v2FrameEncodingType.Default))
                    throw new InvalidDataException("value.TextEncoding has to to be Id3v2FrameEncodingType.Default");

                int i;
                if (value.Values.Any(v => !Int32.TryParse(v, out i)))
                    throw new InvalidDataException("One or more entries in value.Informations are not valid integers.");

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the beats per minute.
        /// </summary>
        /// <value>
        /// The beats per minute.
        /// </value>
        /// <remarks>
        /// BPM is short for beats per minute, and is easily computed by dividing the number of beats in a musical piece with its length.
        /// To get a more accurate result, do the BPM calculation on the main-part only.
        /// To acquire best result measure the time between each beat and calculate individual BPM for each beat and use the median value as result.
        /// BPM is an integer and represented as a numerical string.
        /// </remarks>
        public Id3v2TextFrame BeatsPerMinute
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.BeatsPerMinute);
            }

            set
            {
                if (value == null)
                {
                    RemoveFrame(BeatsPerMinute);
                    return;
                }

                if ((value.Version < Id3v2Version.Id3v240) && (value.TextEncoding != Id3v2FrameEncodingType.Default))
                    throw new InvalidDataException("value.TextEncoding has to to be Id3v2FrameEncodingType.Default");

                int i;
                if (value.Values.Any(v => !Int32.TryParse(v, out i)))
                    throw new InvalidDataException("One or more entries in value.Informations are not valid integers.");

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the composer.
        /// </summary>
        /// <value>
        /// The name of the composer.
        /// </value>
        /// <remarks>
        /// The 'Composer(s)' frame is intended for the name of the composer(s).
        /// </remarks>
        public Id3v2TextFrame ComposerName
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ComposerName);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ComposerName);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the conductor.
        /// </summary>
        /// <value>
        /// The name of the conductor.
        /// </value>
        /// <remarks>
        /// The 'Conductor' frame is used for the name of the conductor.
        /// </remarks>
        public Id3v2TextFrame ConductorName
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ConductorName);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ConductorName);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the content group description.
        /// </summary>
        /// <value>
        /// The group description.
        /// </value>
        /// <remarks>
        /// The 'Content group description' frame is used if the sound belongs to a larger category of sounds/music.
        /// For example, classical music is often sorted in different musical sections (e.g. "Piano Concerto", "Weather - Hurricane").
        /// </remarks>
        public Id3v2TextFrame ContentGroupDescription
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ContentGroupDescription);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ContentGroupDescription);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        /// <remarks>
        /// The content type, which previously (in Id3v1.1, see appendix A) was stored as a one byte numeric value only,
        /// is now a numeric string.
        /// You may use one or several of the types as Id3v1.1 did or,
        /// since the category list would be impossible to maintain with accurate and up to date categories, define your own.
        /// References to the Id3v1 genres can be made by, as first byte,
        /// enter "(" followed by a number from the genres list (section A.3.) and ended with a ")" character.
        /// This is optionally followed by a refinement, e.g. "(21)" or "(4)Eurodisco".
        /// Several references can be made in the same frame, e.g. "(51)(39)".
        /// If the refinement should begin with a "(" character it should be replaced with "((",
        /// e.g. "((I can figure out any genre)" or "(55)((I think...)".
        /// The following new content types is defined in Id3v2 and is implemented in the same way
        /// as the numeric content types, e.g. "(RX)".
        /// <para />
        /// RX  Remix
        /// CR  Cover
        /// </remarks>
        public Id3v2TextFrame ContentType
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ContentType);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ContentType);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the copyright message.
        /// </summary>
        /// <value>
        /// The copyright message.
        /// </value>
        /// <remarks>
        /// The 'Copyright message' frame, which must begin with a year and a space character (making five characters),
        /// is intended for the copyright holder of the original sound, not the audio file itself.
        /// The absence of this frame means only that the copyright information is unavailable or has been removed,
        /// and must not be interpreted to mean that the sound is public domain.
        /// Every time this field is displayed the field must be preceded with "Copyright " (C) " ",
        /// where (C) is one character showing a C in a circle.
        /// </remarks>
        public Id3v2TextFrame CopyrightMessage
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.CopyrightMessage);
            }

            set
            {
                if (value == null)
                    RemoveFrame(CopyrightMessage);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the date for the recording.
        /// </summary>
        /// <value>
        /// The date for the recording.
        /// </value>
        /// <remarks>
        /// The 'Date' frame is a numeric string in the DDMM format containing the date for the recording.
        /// This field is always four characters long.
        /// <para />
        /// This frame has been replaced by the TDRC frame, 'Recording time' as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame DateRecording
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.DateRecording) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                {
                    RemoveFrame(DateRecording);
                    return;
                }

                if (value.Values.Sum(v => v.Length) > 4)
                    throw new InvalidDataException(String.Format("Length of the text value may not exceed {0} characters for this frame", 4));

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the encoded by.
        /// </summary>
        /// <value>
        /// The encoded by.
        /// </value>
        /// <remarks>
        /// The 'Encoded by' frame contains the name of the person or organization that encoded the audio file.
        /// This field may contain a copyright message, if the audio file also is copyrighted by the encoder.
        /// </remarks>
        public Id3v2TextFrame EncodedBy
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.EncodedBy);
            }

            set
            {
                if (value == null)
                    RemoveFrame(EncodedBy);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding settings used.
        /// </summary>
        /// <value>
        /// The encoding settings used.
        /// </value>
        /// <remarks>
        /// The 'Software/hardware and settings used for encoding' frame includes the used audio encoder
        /// and its settings when the file was encoded.
        /// Hardware refers to hardware encoders, not the computer on which a program was run.
        /// </remarks>
        public Id3v2TextFrame EncodingSettingsUsed
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.EncodingSettingsUsed);
            }

            set
            {
                if (value == null)
                    RemoveFrame(EncodingSettingsUsed);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the type of the file.
        /// </summary>
        /// <value>
        /// The type of the file.
        /// </value>
        /// <remarks>
        /// The 'File type' frame indicates which type of audio this tag defines.
        /// The following type and refinements are defined:
        /// MPG    MPEG Audio
        /// /1     MPEG 2 layer I
        /// /2     MPEG 2 layer II
        /// /3     MPEG 2 layer III
        /// /2.5   MPEG 2.5
        /// /AAC   Advanced audio compression
        /// <para/>
        /// but other types may be used, not for these types though.
        /// This is used in a similar way to the predefined types in the "TMT" frame, but without parenthesis.
        /// If this frame is not present audio type is assumed to be "MPG".
        /// </remarks>
        public Id3v2TextFrame FileType
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.FileType);
            }

            set
            {
                if (value == null)
                    RemoveFrame(FileType);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the initial key.
        /// </summary>
        /// <value>
        /// The initial key.
        /// </value>
        /// <remarks>
        /// The 'Initial key' frame contains the musical key in which the sound starts.
        /// It is represented as a string with a maximum length of three characters.
        /// The ground keys are represented with "A","B","C","D","E", "F" and "G" and half keys represented with "b" and "#".
        /// Minor is represented as "m". Example "Cbm". Off key is represented with an "o" only.
        /// </remarks>
        public Id3v2TextFrame InitialKey
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.InitialKey);
            }

            set
            {
                if (value == null)
                    RemoveFrame(InitialKey);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the international standard recording code.
        /// </summary>
        /// <value>
        /// The international standard recording code.
        /// </value>
        /// <remarks>
        /// The 'ISRC' frame should contain the International Standard Recording Code [ISRC] (12 characters).
        /// </remarks>
        public Id3v2TextFrame InternationalStandardRecordingCode
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.InternationalStandardRecordingCode);
            }

            set
            {
                if (value == null)
                {
                    RemoveFrame(InternationalStandardRecordingCode);
                    return;
                }

                string s = value.Values.Aggregate(String.Empty, (current, str) => (current ?? String.Empty) + (str ?? String.Empty));
                if (s.Length != 12)
                    throw new InvalidDataException("ISR codes have to be 12 chars long.");

                string countryCode = s.Substring(0, 2);
                if (!Id3v2Frame.IsValidCountryCode(countryCode))
                    throw new InvalidDataException(String.Format("Language code '{0}' is not a valid ISO-3166-1 alpha-2 language code.", countryCode));

                //// string registrantCode = s.Substring(2, 3);
                //// string yearOfRegistration = s.Substring(5, 2);
                //// string soundRecording = s.Substring(7, 5);

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        /// <remarks>
        /// The 'Length' frame contains the length of the audio file in milliseconds, represented as a numeric string.
        /// </remarks>
        public Id3v2TextFrame Length
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.Length);
            }

            set
            {
                if (value == null)
                {
                    RemoveFrame(Length);
                    return;
                }

                if ((value.Version < Id3v2Version.Id3v240) && (value.TextEncoding != Id3v2FrameEncodingType.Default))
                    throw new InvalidDataException("value.TextEncoding has to to be Id3v2FrameEncodingType.Default");

                int i;
                if (value.Values.Any(v => !Int32.TryParse(v, out i)))
                    throw new InvalidDataException("One or more entries in value.Informations are not valid integers.");

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>
        /// The type of the media.
        /// </value>
        /// <remarks>
        /// The 'Media type' frame describes from which media the sound originated.
        /// This may be a text string or a reference to the predefined media types found in the list below.
        /// References are made within "(" and ")" and are optionally followed by a text refinement, e.g. "(MC) with four channels".
        /// If a text refinement should begin with a "(" character it should be replaced with "((" in the same way as in the "TCO" frame.
        /// Predefined refinements is appended after the media type, e.g. "(CD/S)" or "(VID/PAL/VHS)".
        /// <para/>
        /// DIG    Other digital media
        /// /A    Analog transfer from media
        /// <para/>
        /// ANA    Other analog media
        /// /WAC  Wax cylinder
        /// /8CA  8-track tape cassette
        /// <para/>
        /// CD     CD
        /// /A    Analog transfer from media
        /// /DD   DDD
        /// /AD   ADD
        /// /AA   AAD
        /// <para/>
        /// LD     Laserdisc
        /// /A     Analog transfer from media
        /// <para/>
        /// TT     Turntable records
        /// /33    33.33 rpm
        /// /45    45 rpm
        /// /71    71.29 rpm
        /// /76    76.59 rpm
        /// /78    78.26 rpm
        /// /80    80 rpm
        /// <para/>
        /// MD     MiniDisc
        /// /A    Analog transfer from media
        /// <para/>
        /// DAT    DAT
        /// /A    Analog transfer from media
        /// /1    standard, 48 kHz/16 bits, linear
        /// /2    mode 2, 32 kHz/16 bits, linear
        /// /3    mode 3, 32 kHz/12 bits, nonlinear, low speed
        /// /4    mode 4, 32 kHz/12 bits, 4 channels
        /// /5    mode 5, 44.1 kHz/16 bits, linear
        /// /6    mode 6, 44.1 kHz/16 bits, 'wide track' play
        /// <para/>
        /// DCC    DCC
        /// /A    Analog transfer from media
        /// <para/>
        /// DVD    DVD
        /// /A    Analog transfer from media
        /// <para/>
        /// TV     Television
        /// /PAL    PAL
        /// /NTSC   NTSC
        /// /SECAM  SECAM
        /// <para/>
        /// VID    Video
        /// /PAL    PAL
        /// /NTSC   NTSC
        /// /SECAM  SECAM
        /// /VHS    VHS
        /// /SVHS   S-VHS
        /// /BETA   BETAMAX
        /// <para/>
        /// RAD    Radio
        /// /FM   FM
        /// /AM   AM
        /// /LW   LW
        /// /MW   MW
        /// <para/>
        /// TEL    Telephone
        /// /I    ISDN
        /// <para/>
        /// MC     MC (normal cassette)
        /// /4    4.75 cm/s (normal speed for a two sided cassette)
        /// /9    9.5 cm/s
        /// /I    Type I cassette (ferric/normal)
        /// /II   Type II cassette (chrome)
        /// /III  Type III cassette (ferric chrome)
        /// /IV   Type IV cassette (metal)
        /// <para/>
        /// REE    Reel
        /// /9    9.5 cm/s
        /// /19   19 cm/s
        /// /38   38 cm/s
        /// /76   76 cm/s
        /// /I    Type I cassette (ferric/normal)
        /// /II   Type II cassette (chrome)
        /// /III  Type III cassette (ferric chrome)
        /// /IV   Type IV cassette (metal)
        /// </remarks>
        public Id3v2TextFrame MediaType
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.MediaType);
            }

            set
            {
                if (value == null)
                    RemoveFrame(MediaType);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the modified by.
        /// </summary>
        /// <value>
        /// The modified by.
        /// </value>
        /// <remarks>
        /// The 'Interpreted, remixed, or otherwise modified by' frame contains more information about the people
        /// behind a remix and similar interpretations of another existing piece.
        /// </remarks>
        public Id3v2TextFrame ModifiedBy
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.ModifiedBy);
            }

            set
            {
                if (value == null)
                    RemoveFrame(ModifiedBy);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original album title.
        /// </summary>
        /// <value>
        /// The original album title.
        /// </value>
        /// <remarks>
        /// The 'Original album/Movie/Show title' frame is intended for the title of the original recording(/source of sound),
        /// if for example the music in the file should be a cover of a previously released song.
        /// </remarks>
        public Id3v2TextFrame OriginalAlbumTitle
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.OriginalAlbumTitle);
            }

            set
            {
                if (value == null)
                    RemoveFrame(OriginalAlbumTitle);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original artist.
        /// </summary>
        /// <value>
        /// The original artist.
        /// </value>
        /// <remarks>
        /// The 'Original artist(s)/performer(s)' frame is intended for the performer(s) of the original recording,
        /// if for example the music in the file should be a cover of a previously released song.
        /// </remarks>
        public Id3v2TextFrame OriginalArtist
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.OriginalArtist);
            }

            set
            {
                if (value == null)
                    RemoveFrame(OriginalArtist);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original filename.
        /// </summary>
        /// <value>
        /// The original filename.
        /// </value>
        /// <remarks>
        /// The 'Original filename' frame contains the preferred filename for the file,
        /// since some media doesn't allow the desired length of the filename.
        /// The filename is case sensitive and includes its suffix.
        /// </remarks>
        public Id3v2TextFrame OriginalFilename
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.OriginalFilename);
            }

            set
            {
                if (value == null)
                    RemoveFrame(OriginalFilename);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original release year.
        /// </summary>
        /// <value>
        /// The original release year.
        /// </value>
        /// <remarks>
        /// The 'Original release year' frame is intended for the year when the original recording,
        /// if for example the music in the file should be a cover of a previously released song, was released.
        /// The field is formatted as in the "TDY" frame.
        /// <para />
        /// This frame has been replaced by the TDOR frame, 'Original release time' as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame OriginalReleaseYear
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.OriginalReleaseYear) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(OriginalReleaseYear);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original text writer.
        /// </summary>
        /// <value>
        /// The original text writer.
        /// </value>
        /// <remarks>
        /// The 'Original Lyricist(s)/text writer(s)' frame is intended for the text writer(s) of the original recording,
        /// if for example the music in the file should be a cover of a previously released song.
        /// </remarks>
        public Id3v2TextFrame OriginalTextWriter
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.OriginalTextWriter);
            }

            set
            {
                if (value == null)
                    RemoveFrame(OriginalTextWriter);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the part of set.
        /// </summary>
        /// <value>
        /// The part of set.
        /// </value>
        /// <remarks>
        /// The 'Part of a set' frame is a numeric string that describes which part of a set the audio came from.
        /// This frame is used if the source described in the "TAL" frame is divided into several mediums, e.g. a double CD.
        /// The value may be extended with a "/" character and a numeric string
        /// containing the total number of parts in the set. E.g. "1/2".
        /// </remarks>
        public Id3v2TextFrame PartOfSet
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.PartOfSet);
            }

            set
            {
                if (value == null)
                    RemoveFrame(PartOfSet);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the playlist delay.
        /// </summary>
        /// <value>
        /// The playlist delay.
        /// </value>
        /// <remarks>
        /// The 'Playlist delay' defines the numbers of milliseconds of silence between every song in a playlist.
        /// The player should use the "ETC" frame, if present,
        /// to skip initial silence and silence at the end of the audio to match the 'Playlist delay' time.
        /// The time is represented as a numeric string.
        /// </remarks>
        public Id3v2TextFrame PlaylistDelay
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.PlaylistDelay);
            }

            set
            {
                if (value == null)
                    RemoveFrame(PlaylistDelay);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the publisher.
        /// </summary>
        /// <value>
        /// The publisher.
        /// </value>
        /// <remarks>
        /// The 'Publisher' frame simply contains the name of the label or publisher.
        /// </remarks>
        public Id3v2TextFrame Publisher
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.Publisher);
            }

            set
            {
                if (value == null)
                    RemoveFrame(Publisher);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the recording dates.
        /// </summary>
        /// <value>
        /// The recording dates.
        /// </value>
        /// <remarks>
        /// The 'Recording dates' frame is a intended to be used as complement to the "TYE", "TDA" and "TIM" frames.
        /// E.g. "4th-7th June, 12th June" in combination with the "TYE" frame.
        /// <para />
        /// This frame has been replaced by the TDRC frame, 'Recording time' as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame RecordingDates
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.RecordingDates) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(RecordingDates);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the text language.
        /// </summary>
        /// <value>
        /// The text language.
        /// </value>
        /// <exception cref="System.IO.InvalidDataException">
        /// thrown when the language is not a ISO-639-2 value or the language is not XXX for version Id3v2.4.0
        /// </exception>
        /// <remarks>
        /// The 'Language(s)' frame should contain the languages of the text or lyrics in the audio file.
        /// The language is represented with three characters according to ISO-639-2.
        /// If more than one language is used in the text their language codes should follow according to their usage.
        /// </remarks>
        public Id3v2TextFrame TextLanguages
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.TextLanguages);
            }

            set
            {
                if (value == null)
                {
                    RemoveFrame(TextLanguages);
                    return;
                }

                string s = value.Values.Aggregate(String.Empty, (current, str) => (current ?? String.Empty) + (str ?? String.Empty));
                int index = 0;
                while (index < s.Length)
                {
                    // Id3v2.4.0: If the language is not known the string "XXX" should be used.
                    string code = s.Substring(index, Math.Min(3, s.Length - index));
                    //if (!Id3v2Frame.IsValidLanguageCode(code) && ((Version != Id3v2Version.Id3v240) || (code.ToUpper() != "XXX")))
                      //  throw new InvalidDataException(String.Format("Language code '{0}' is not a valid ISO-639-2 language code.", code));

                    index += 3;
                }

                // Id3v2.4.0 and later: The language should be represented in lower case.
                if (Version >= Id3v2Version.Id3v240)
                {
                    for (int i = 0; i < value.Values.Count; i++)
                        value.Values[i] = value.Values[i].ToLower();
                }

                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the text writer.
        /// </summary>
        /// <value>
        /// The text writer.
        /// </value>
        /// <remarks>
        /// The 'Lyricist(s)/text writer(s)' frame is intended for the writer(s) of the text or lyrics in the recording.
        /// </remarks>
        public Id3v2TextFrame TextWriter
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.TextWriter);
            }

            set
            {
                if (value == null)
                    RemoveFrame(TextWriter);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the time for the recording.
        /// </summary>
        /// <value>
        /// The time for the recording.
        /// </value>
        /// <remarks>
        /// The 'Time' frame is a numeric string in the HHMM format containing the time for the recording.
        /// This field is always four characters long.
        /// <para />
        /// This frame has been replaced by the TDRC frame, 'Recording time' as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame TimeRecording
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.TimeRecording) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(TimeRecording);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        /// <value>
        /// The track number.
        /// </value>
        /// <remarks>
        /// The 'Track number/Position in set' frame is a numeric string
        /// containing the order number of the audio-file on its original recording.
        /// This may be extended with a "/" character and a numeric string
        /// containing the total number of tracks/elements on the original recording. E.g. "4/9".
        /// </remarks>
        public Id3v2TextFrame TrackNumber
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.TrackNumber);
            }

            set
            {
                if (value == null)
                {
                    RemoveFrame(TrackNumber);
                    return;
                }
                SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the track title.
        /// </summary>
        /// <value>
        /// The track title.
        /// </value>
        public Id3v2TextFrame TrackTitle
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.TrackTitle);
            }

            set
            {
                if (value == null)
                    RemoveFrame(TrackTitle);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the track title description.
        /// </summary>
        /// <value>
        /// The track title description.
        /// </value>
        /// <remarks>
        /// The 'Subtitle/Description refinement' frame is used for information directly related
        /// to the contents title (e.g. "Op. 16" or "Performed live at wembley").
        /// </remarks>
        public Id3v2TextFrame TrackTitleDescription
        {
            get
            {
                return GetTextFrame(Id3v2TextFrameIdentifier.TrackTitleDescription);
            }

            set
            {
                if (value == null)
                    RemoveFrame(TrackTitleDescription);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the year of the recording.
        /// </summary>
        /// <value>
        /// The year of the recording.
        /// </value>
        /// <remarks>
        /// The 'Year' frame is a numeric string with a year of the recording.
        /// This frames is always four characters long (until the year 10000).
        /// <para />
        /// This frame has been replaced by the TDRC frame, 'Recording time' as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame YearRecording
        {
            get
            {
                return (Version < Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.YearRecording) : null;
            }

            set
            {
                if (Version >= Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(YearRecording);
                else
                    SetFrame(value);
            }
        }
    }
}
