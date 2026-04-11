/*
 * Date: 2011-08-14
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        /// <summary>
        /// Gets or sets the album sort order.
        /// </summary>
        /// <value>
        /// The album sort order.
        /// </value>
        /// <remarks>
        /// The 'Album sort order' frame defines a string which should be used instead of the album name (TALB) for sorting purposes.
        /// E.g. an album named "A Soundtrack" might preferably be sorted as "Soundtrack".
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame AlbumSortOrder
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.AlbumSortOrder) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(AlbumSortOrder);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding time.
        /// </summary>
        /// <value>
        /// The encoding time.
        /// </value>
        /// <remarks>
        /// The 'Encoding time' frame contains a timestamp describing when the audio was encoded.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame EncodingTime
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.EncodingTime) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(EncodingTime);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the involved people list.
        /// </summary>
        /// <value>
        /// The involved people list.
        /// </value>
        /// <remarks>
        /// 'Involved people list' is very similar to the musician credits list, but maps between functions, like producer, and names.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame InvolvedPeopleList2
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.InvolvedPeopleList) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(InvolvedPeopleList2);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the mood.
        /// </summary>
        /// <value>
        /// The mood of the audio.
        /// </value>
        /// <remarks>
        /// The 'Mood' frame is intended to reflect the mood of the audio with a few keywords, e.g. "Romantic" or "Sad".
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame Mood
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.Mood) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(Mood);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the musician credits list.
        /// </summary>
        /// <value>
        /// The musician credits list.
        /// </value>
        /// <remarks>
        /// The 'Musician credits list' is intended as a mapping between instruments and the musician that played it.
        /// Every odd field is an instrument and every even is an artist or a comma delimited list of artists.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame MusicianCreditsList
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.MusicianCreditsList) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(MusicianCreditsList);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the original release time.
        /// </summary>
        /// <value>
        /// The original release time.
        /// </value>
        /// <remarks>
        /// The 'Original release time' frame contains a timestamp describing when the original recording of the audio was released.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame OriginalReleaseTime
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.OriginalReleaseTime) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(OriginalReleaseTime);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the performer sort order.
        /// </summary>
        /// <value>
        /// The performer sort order.
        /// </value>
        /// <remarks>
        /// The 'Performer sort order' frame defines a string which should be used instead of the performer (TPE2) for sorting purposes.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame PerformerSortOrder
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.PerformerSortOrder) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;
                
                if (value == null)
                    RemoveFrame(PerformerSortOrder);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the produced note.
        /// </summary>
        /// <value>
        /// The produced note.
        /// </value>
        /// <remarks>
        /// The 'Produced notice' frame, in which the string must begin with a year and a space character (making five characters), 
        /// is intended for the production copyright holder of the original sound, not the audio file itself.
        /// The absence of this frame means only that the production copyright information is unavailable or has been removed, 
        /// and must not be interpreted to mean that the audio is public domain.
        /// Every time this field is displayed the field must be preceded with "Produced " (P) " ", 
        /// where (P) is one character showing a P in a circle.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame ProducedNote
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.ProducedNote) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(ProducedNote);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the recording time.
        /// </summary>
        /// <value>
        /// The recording time.
        /// </value>
        /// <remarks>
        /// The 'Recording time' frame contains a timestamp describing when the audio was recorded.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame RecordingTime
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.RecordingTime) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(RecordingTime);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the release time.
        /// </summary>
        /// <value>
        /// The release time.
        /// </value>
        /// <remarks>
        /// The 'Release time' frame contains a timestamp describing when the audio was first released.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame ReleaseTime
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.ReleaseTime) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(ReleaseTime);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the set subtitle.
        /// </summary>
        /// <value>
        /// The set subtitle.
        /// </value>
        /// <remarks>
        /// The 'Set subtitle' frame is intended for the subtitle of the part of a set this track belongs to.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame SetSubtitle
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.SetSubtitle) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(SetSubtitle);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the tagging time.
        /// </summary>
        /// <value>
        /// The tagging time.
        /// </value>
        /// <remarks>
        /// The 'Tagging time' frame contains a timestamp describing then the audio was tagged.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame TaggingTime
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.TaggingTime) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(TaggingTime);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the title sort order.
        /// </summary>
        /// <value>
        /// The title sort order.
        /// </value>
        /// <remarks>
        /// The 'Title sort order' frame defines a string which should be used instead of the title (TIT2) for sorting purposes.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2TextFrame TitleSortOrder
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetTextFrame(Id3v2TextFrameIdentifier.TitleSortOrder) : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;
                
                if (value == null)
                    RemoveFrame(TitleSortOrder);
                else
                    SetFrame(value);
            }
        }
    }
}
