/*
 * Date: 2011-05-14
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
        /// Gets or sets a list of unique file identifiers.
        /// </summary>
        /// <value>
        /// The unique file identifiers.
        /// </value>
        /// <remarks>
        /// The purpose of the <see cref="Id3v2UniqueFileIdentifierFrame"/> frame is to be able to identify the audio file in a database
        /// that may contain more information relevant to the content.
        /// All <see cref="Id3v2UniqueFileIdentifierFrame"/>s contain an URL of an email address,
        /// or a link to a location where an email address can be found,
        /// that belongs to the organization responsible for this specific database implementation.
        /// Questions regarding the database should be sent to the indicated email address.
        /// The URL should not be used for the actual database queries.
        /// The string "http://www.id3.org/dummy/ufid.html" should be used for tests.
        /// <para/>
        /// There may be more than one <see cref="Id3v2UniqueFileIdentifierFrame"/> frame in an <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2UniqueFileIdentifierFrame.OwnerIdentifier"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2UniqueFileIdentifierFrame> UniqueFileIdentifiers
        {
            get
            {
                return GetFrameCollection<Id3v2UniqueFileIdentifierFrame>();
            }

            set
            {
                RemoveFrames<Id3v2UniqueFileIdentifierFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of user defined text information.
        /// </summary>
        /// <value>
        /// The user defined text information's.
        /// </value>
        /// <remarks>
        /// These frames are intended for one-string text information concerning the audio file in a similar way to the other <see cref="Id3v2TextFrame"/>s.
        /// <para/>
        /// There may be more than one <see cref="Id3v2UserDefinedTextInformationFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2UserDefinedTextInformationFrame.Description"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2UserDefinedTextInformationFrame> UserDefinedTextInformations
        {
            get
            {
                return GetFrameCollection<Id3v2UserDefinedTextInformationFrame>();
            }

            set
            {
                RemoveFrames<Id3v2UserDefinedTextInformationFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of user defined URL links.
        /// </summary>
        /// <value>
        /// The user defined URL links.
        /// </value>
        /// <remarks>
        /// These frames are intended for URL [URL] links concerning the audio file in a similar way to the other <see cref="Id3v2UrlLinkFrame"/> frames.
        /// <para/>
        /// There may be more than one <see cref="Id3v2UserDefinedUrlLinkFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2UserDefinedUrlLinkFrame.Description"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2UserDefinedUrlLinkFrame> UserDefinedUrlLinks
        {
            get
            {
                return GetFrameCollection<Id3v2UserDefinedUrlLinkFrame>();
            }

            set
            {
                RemoveFrames<Id3v2UserDefinedUrlLinkFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of involved people.
        /// </summary>
        /// <value>
        /// The involved people list.
        /// </value>
        /// <remarks>
        /// Since there might be a lot of people contributing to an audio file in various ways, such as musicians and technicians,
        /// the 'Text information frames' are often insufficient to list everyone involved in a project.
        /// The 'Involved people list' is a frame containing the names of those involved, and how they were involved.
        /// <para/>
        /// There may only be one <see cref="Id3v2InvolvedPeopleListFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// <para/>
        /// This frame has been replaced by the two frames <see cref="MusicianCreditsList"/> and <see cref="InvolvedPeopleList2"/> as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2InvolvedPeopleListFrame InvolvedPeopleList
        {
            get
            {
                return GetFrame<Id3v2InvolvedPeopleListFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(InvolvedPeopleList);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the music CD identifier.
        /// </summary>
        /// <value>
        /// The music CD identifier.
        /// </value>
        /// <remarks>
        /// This frame is intended for music that comes from a CD, so that the CD can be identified in databases such as the CDDB [CDDB].
        /// The frame consists of a binary dump of the Table Of Contents, TOC,  from the CD,
        /// which is a header of 4 bytes and then 8 bytes/track on the CD making a maximum of 804 bytes.
        /// <para/>
        /// This frame requires a present and valid <see cref="TrackNumber"/>.
        /// <para/>
        /// There may only be one <see cref="Id3v2MusicCdIdentifierFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2MusicCdIdentifierFrame MusicCdIdentifier
        {
            get
            {
                return GetFrame<Id3v2MusicCdIdentifierFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(MusicCdIdentifier);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the event timing codes.
        /// </summary>
        /// <value>
        /// The event timing codes.
        /// </value>
        /// <remarks>
        /// This field allows synchronization with key events in a song or sound.
        /// <para/>
        /// There may only be one <see cref="Id3v2EventTimingCodesFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2EventTimingCodesFrame EventTimingCodes
        {
            get
            {
                return GetFrame<Id3v2EventTimingCodesFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(EventTimingCodes);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the MPEG location lookup table.
        /// </summary>
        /// <value>
        /// The MPEG location lookup table.
        /// </value>
        /// <remarks>
        /// This field includes references that the software can use to calculate positions in the file.
        /// <para/>
        /// There may only be one <see cref="Id3v2MpegLocationLookupTableFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2MpegLocationLookupTableFrame MpegLocationLookupTable
        {
            get
            {
                return GetFrame<Id3v2MpegLocationLookupTableFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(MpegLocationLookupTable);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the synced tempo codes.
        /// </summary>
        /// <value>
        /// The synced tempo codes.
        /// </value>
        /// <remarks>
        /// For a more accurate description of the tempo of a musical piece this frame might be used.
        /// Each tempo code consists of one tempo part and one time part.
        /// <para/>
        /// There may only be one <see cref="Id3v2SyncedTempoCodesFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2SyncedTempoCodesFrame SyncedTempoCodes
        {
            get
            {
                return GetFrame<Id3v2SyncedTempoCodesFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(SyncedTempoCodes);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of unsynchronized lyrics.
        /// </summary>
        /// <value>
        /// The unsynchronized lyrics.
        /// </value>
        /// <remarks>
        /// This frame contains the lyrics of the song or a text transcription of other vocal activities.
        /// <para/>
        /// There may be more than one <see cref="Id3v2UnsynchronizedLyricsFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2UnsynchronizedLyricsFrame.Language"/> and <see cref="Id3v2UnsynchronizedLyricsFrame.ContentDescriptor"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2UnsynchronizedLyricsFrame> UnsychronizedLyrics
        {
            get
            {
                return GetFrameCollection<Id3v2UnsynchronizedLyricsFrame>();
            }

            set
            {
                RemoveFrames<Id3v2UnsynchronizedLyricsFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of synchronized lyrics.
        /// </summary>
        /// <value>
        /// The synchronized lyrics.
        /// </value>
        /// <remarks>
        /// This is another way of incorporating the words, said or sung lyrics,
        /// in the audio file as text, this time, however, in sync with the audio.
        /// It might also be used to describing events e.g. occurring on a stage
        /// or on the screen in sync with the audio.
        /// <para/>
        /// There may be more than one <see cref="Id3v2SynchronizedLyricsFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2SynchronizedLyricsFrame.Language"/> and <see cref="Id3v2SynchronizedLyricsFrame.ContentDescriptor"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2SynchronizedLyricsFrame> SynchronizedLyrics
        {
            get
            {
                return GetFrameCollection<Id3v2SynchronizedLyricsFrame>();
            }

            set
            {
                RemoveFrames<Id3v2SynchronizedLyricsFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        /// <remarks>
        /// There may be more than one <see cref="Id3v2CommentFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2CommentFrame.Language"/> and <see cref="Id3v2CommentFrame.ShortContentDescription"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2CommentFrame> Comments
        {
            get
            {
                return GetFrameCollection<Id3v2CommentFrame>();
            }

            set
            {
                RemoveFrames<Id3v2CommentFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets the relative volume adjustment.
        /// </summary>
        /// <value>
        /// The relative volume adjustment.
        /// </value>
        /// <remarks>
        /// This field allows the user to say how much he wants to increase/decrease the volume on each channel while the file is played.
        /// The purpose is to be able to align all files to a reference volume, so that you don't have to change the volume constantly.
        /// This frame may also be used to balance adjust the audio.
        /// If the volume peak levels are known then this could be described with the <see cref="Id3v2RelativeVolumeAdjustmentFrame.PeakVolumeRightChannel"/> and <see cref="Id3v2RelativeVolumeAdjustmentFrame.PeakVolumeLeftChannel"/> fields.
        /// <para/>
        /// There may only be one <see cref="Id3v2RelativeVolumeAdjustmentFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// <para/>
        /// This frame has been replaced by the <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2RelativeVolumeAdjustmentFrame RelativeVolumeAdjustment
        {
            get
            {
                return GetFrame<Id3v2RelativeVolumeAdjustmentFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(RelativeVolumeAdjustment);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the equalisation.
        /// </summary>
        /// <value>
        /// The equalisation.
        /// </value>
        /// <remarks>
        /// This field allows the user to predefine an equalisation curve within the audio file.
        /// <para/>
        /// There may only be one <see cref="Id3v2EqualisationFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// <para/>
        /// This frame has been replaced by the <see cref="Id3v2Equalisation2Frame"/> frame as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2EqualisationFrame Equalisation
        {
            get
            {
                return GetFrame<Id3v2EqualisationFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(Equalisation);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the reverb.
        /// </summary>
        /// <value>
        /// The reverb.
        /// </value>
        /// <remarks>
        /// This frame is used to adjust echoes of different kinds.
        /// <para/>
        /// There may only be one <see cref="Id3v2ReverbFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2ReverbFrame Reverb
        {
            get
            {
                return GetFrame<Id3v2ReverbFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(Reverb);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of attached pictures.
        /// </summary>
        /// <value>
        /// The attached pictures.
        /// </value>
        /// <remarks>
        /// There may only be one picture with the picture type declared as <see cref="Id3v2AttachedPictureType.FileIcon"/>
        /// and <see cref="Id3v2AttachedPictureType.OtherFileIcon"/> respectively.
        /// <para/>
        /// There may be several <see cref="Id3v2AttachedPictureFrame"/> in one file,
        /// each in their individual <see cref="Id3v2AttachedPictureFrame"/>,
        /// but only one with the same <see cref="Id3v2AttachedPictureFrame.Description"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2AttachedPictureFrame> AttachedPictures
        {
            get
            {
                return GetFrameCollection<Id3v2AttachedPictureFrame>();
            }

            set
            {
                RemoveFrames<Id3v2AttachedPictureFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of general encapsulated objects.
        /// </summary>
        /// <value>
        /// The general encapsulated objects.
        /// </value>
        /// <remarks>
        /// In this field any type of file can be encapsulated.
        /// <para/>
        /// There may be more than one <see cref="Id3v2GeneralEncapsulatedObjectFrame"/> frame in each <see cref="Id3v2Tag"/>,
        /// but only one with the same <see cref="Id3v2GeneralEncapsulatedObjectFrame.ContentDescription"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2GeneralEncapsulatedObjectFrame> GeneralEncapsulatedObjects
        {
            get
            {
                return GetFrameCollection<Id3v2GeneralEncapsulatedObjectFrame>();
            }

            set
            {
                RemoveFrames<Id3v2GeneralEncapsulatedObjectFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets the play counter.
        /// </summary>
        /// <remarks>
        /// This is simply a counter of the number of times a file has been played.
        /// <para />
        /// There may only be one <see cref="Id3v2PlayCounterFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2PlayCounterFrame PlayCounter
        {
            get
            {
                return GetFrame<Id3v2PlayCounterFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(PlayCounter);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of popularimeters in the tag.
        /// </summary>
        /// <value>
        /// A list of popularimeters.
        /// </value>
        /// <remarks>
        /// The purpose of this field is to specify how good an audio file is.
        /// Many interesting applications could be found to this frame such as a playlist that features better audio files more often 
        /// than others or it could be used to profile a person's taste and find other 'good' files by comparing people's profiles.
        /// <para />
        /// There may be more than one <see cref="Id3v2PopularimeterFrame"/> frame in each <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2PopularimeterFrame.EmailToUser"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2PopularimeterFrame> Popularimeters
        {
            get
            {
                return GetFrameCollection<Id3v2PopularimeterFrame>();
            }

            set
            {
                RemoveFrames<Id3v2PopularimeterFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets the size of the recommended buffer.
        /// </summary>
        /// <value>
        /// The size of the recommended buffer.
        /// </value>
        /// <remarks>
        /// Embedded tags are generally not recommended since this could render unpredictable behavior from present software/hardware.
        /// <para />
        /// For applications like streaming audio it might be an idea to embed tags into the audio stream though.
        /// If the clients connects to individual connections like HTTP and there is a possibility to begin every transmission with a tag, 
        /// then this tag should include an <see cref="Id3v2RecommendedBufferSizeFrame"/> frame.
        /// If the client is connected to a arbitrary point in the stream, such as radio or multicast, 
        /// then the <see cref="Id3v2RecommendedBufferSizeFrame"/> frame SHOULD be included in every <see cref="Id3v2Tag"/>.
        /// <para />
        /// There may only be one <see cref="Id3v2RecommendedBufferSizeFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// </remarks>
        public Id3v2RecommendedBufferSizeFrame RecommendedBufferSize
        {
            get
            {
                return GetFrame<Id3v2RecommendedBufferSizeFrame>();
            }

            set
            {
                if (value == null)
                    RemoveFrame(RecommendedBufferSize);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of encrypted meta frames.
        /// </summary>
        /// <value>
        /// A list of encrypted meta frames.
        /// </value>
        /// <remarks>
        /// This property contains one or more encrypted frames.
        /// <para />
        /// This enables protection of copyrighted information such as pictures and text, that people might want to pay extra for.
        /// Since standardization of such an encryption scheme is beyond this document, 
        /// all <see cref="Id3v2EncryptedMetaFrame"/> frames contain an URL [URL] containing an email address, 
        /// or a link to a location where an email address can be found, 
        /// that belongs to the organization responsible for this specific encrypted meta frame.
        /// <para />
        /// Questions regarding the encrypted frame should be sent to the indicated email address.
        /// <para />
        /// When an Id3v2 decoder encounters an <see cref="Id3v2EncryptedMetaFrame"/> frame, 
        /// it should send the data block to the 'plugin' with the corresponding <see cref="Id3v2EncryptedMetaFrame.OwnerIdentifier"/> 
        /// and expect to receive either a data block with one or several Id3v2 frames after each other or an error.
        /// <para />
        /// There may be more than one <see cref="Id3v2EncryptedMetaFrame"/> frame in an <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2EncryptedMetaFrame.OwnerIdentifier"/>.
        /// <para />
        /// This frame has been removed as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2EncryptedMetaFrame> EncryptedMetaFrames
        {
            get
            {
                return GetFrameCollection<Id3v2EncryptedMetaFrame>();
            }

            set
            {
                RemoveFrames<Id3v2EncryptedMetaFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of audio encryptions.
        /// </summary>
        /// <value>
        /// A list of audio encryptions.
        /// </value>
        /// <remarks>
        /// This property indicates if the actual audio stream is encrypted, and by whom.
        /// Since standardization of such encryption scheme is beyond this document, 
        /// all <see cref="Id3v2AudioEncryptionFrame"/> frames contain an URL which can be an email address, 
        /// or a link to a location where an email address can be found, 
        /// that belongs to the organization responsible for this specific encrypted audio file.
        /// Questions regarding the encrypted audio should be sent to the email address specified.
        /// <para />
        /// There may be more than one <see cref="Id3v2AudioEncryptionFrame"/> frames in an <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2AudioEncryptionFrame.OwnerIdentifier"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2AudioEncryptionFrame> AudioEncryptions
        {
            get
            {
                return GetFrameCollection<Id3v2AudioEncryptionFrame>();
            }

            set
            {
                RemoveFrames<Id3v2AudioEncryptionFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of linked information.
        /// </summary>
        /// <value>
        /// A list of linked information.
        /// </value>
        /// <remarks>
        /// To keep space waste as low as possible this property may be used to link information 
        /// from another Id3v2 tag that might reside in another audio file or alone in a binary file.
        /// It is recommended that this method is only used when the files are stored on a CD-ROM 
        /// or other circumstances when the risk of file separation is low.
        /// The frame contains a frame identifier, which is the frame that should be linked into this tag, 
        /// a URL [URL] field, where a reference to the file where the frame is given, and additional ID data, if needed.
        /// Data should be retrieved from the first tag found in the file to which this link points.
        /// <para />
        /// There may be more than one <see cref="Id3v2LinkedInformationFrame"/> frame in an <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2LinkedInformationFrame.FrameIdentifier"/>, 
        /// <see cref="Id3v2LinkedInformationFrame.Url"/> and <see cref="Id3v2LinkedInformationFrame.AdditionalIdData"/>.
        /// <para />
        /// A linked frame is to be considered as part of the <see cref="Id3v2Tag"/> and has the same restrictions 
        /// as if it was a physical part of the tag (i.e. only one <see cref="Id3v2ReverbFrame"/> frame allowed, whether it's linked or not).
        /// <para />
        /// Frames that may be linked and need no additional data are 
        /// <see cref="Id3v2InvolvedPeopleListFrame"/>, 
        /// <see cref="Id3v2MusicCdIdentifierFrame"/>, 
        /// <see cref="Id3v2EventTimingCodesFrame"/>, 
        /// <see cref="Id3v2SyncedTempoCodesFrame"/>, 
        /// <see cref="Id3v2RelativeVolumeAdjustmentFrame"/>, 
        /// <see cref="Id3v2EqualisationFrame"/>, 
        /// <see cref="Id3v2ReverbFrame"/>, 
        /// <see cref="Id3v2RecommendedBufferSizeFrame"/>, 
        /// the <see cref="Id3v2TextFrame"/>s and the <see cref="Id3v2UrlLinkFrame"/> frames.
        /// <para />
        /// The <see cref="Id3v2UserDefinedTextInformationFrame"/>, 
        /// <see cref="Id3v2AttachedPictureFrame"/>, 
        /// <see cref="Id3v2GeneralEncapsulatedObjectFrame"/>, 
        /// <see cref="Id3v2EncryptedMetaFrame"/> and <see cref="Id3v2AudioEncryptionFrame"/> frames 
        /// may be linked with the content descriptor as <see cref="Id3v2LinkedInformationFrame.AdditionalIdData"/>.
        /// <para />
        /// The <see cref="Id3v2CommentFrame"/>, 
        /// <see cref="Id3v2SynchronizedLyricsFrame"/> and <see cref="Id3v2UnsynchronizedLyricsFrame"/> frames 
        /// may be linked with three bytes of language descriptor directly followed by a content descriptor 
        /// as <see cref="Id3v2LinkedInformationFrame.AdditionalIdData"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2LinkedInformationFrame> LinkedInformations
        {
            get
            {
                return GetFrameCollection<Id3v2LinkedInformationFrame>();
            }

            set
            {
                RemoveFrames<Id3v2LinkedInformationFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }
    }
}
