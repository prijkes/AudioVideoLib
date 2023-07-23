/*
 * Date: 2012-11-26
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// A list of pre-defined known <see cref="Id3v2TextFrame"/> identifiers in an <see cref="Id3v2Tag"/>.
    /// </summary>
    public enum Id3v2TextFrameIdentifier
    {
        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.AlbumSortOrder"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        AlbumSortOrder,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.AlbumTitle"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        AlbumTitle,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.Artist"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        Artist,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ArtistExtra"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ArtistExtra,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.AudioSize"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This frame identifier has been removed as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        AudioSize,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.BeatsPerMinute"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        BeatsPerMinute,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ComposerName"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ComposerName,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ConductorName"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ConductorName,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ContentGroupDescription"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ContentGroupDescription,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ContentType"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ContentType,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.CopyrightMessage"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        CopyrightMessage,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.DateRecording"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been replaced by the <see cref="RecordingTime"/> identifier as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        DateRecording,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.EncodedBy"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        EncodedBy,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.EncodingSettingsUsed"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        EncodingSettingsUsed,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.EncodingTime"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        EncodingTime,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.FileOwner"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        FileOwner,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.FileType"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        FileType,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.InitialKey"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        InitialKey,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.InternationalStandardRecordingCode"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        InternationalStandardRecordingCode,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.InternetRadioStationName"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        InternetRadioStationName,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.InternetRadioStationOwner"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        InternetRadioStationOwner,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.InvolvedPeopleList2"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        InvolvedPeopleList,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.Length"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        Length,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.MediaType"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        MediaType,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ModifiedBy"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        ModifiedBy,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.Mood"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        Mood,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.MusicianCreditsList "/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        MusicianCreditsList,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalAlbumTitle"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        OriginalAlbumTitle,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalArtist"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        OriginalArtist,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalFilename"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        OriginalFilename,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalReleaseTime"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        OriginalReleaseTime,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalReleaseYear"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been replaced by the <see cref="OriginalReleaseTime"/> identifier as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        OriginalReleaseYear,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.OriginalTextWriter"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        OriginalTextWriter,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.PartOfSet"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        PartOfSet,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.PerformerSortOrder"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        PerformerSortOrder,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.PlaylistDelay"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        PlaylistDelay,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ProducedNote"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        ProducedNote,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.Publisher"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        Publisher,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.RecordingDates"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been replaced by the <see cref="RecordingTime"/> identifier as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        RecordingDates,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.RecordingTime"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        RecordingTime,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.ReleaseTime"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        ReleaseTime,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.SetSubtitle"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        SetSubtitle,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TaggingTime"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        TaggingTime,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TextLanguages"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        TextLanguages,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TextWriter"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        TextWriter,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TimeRecording"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been replaced by the <see cref="RecordingTime"/> identifier as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        TimeRecording,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TitleSortOrder"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        TitleSortOrder,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TrackNumber"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        TrackNumber,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TrackTitle"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        TrackTitle,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.TrackTitleDescription"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        TrackTitleDescription,

        /// <summary>
        /// Identifier for the <see cref="Id3v2Tag.YearRecording"/> <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <remarks>
        /// This identifier has been replaced by the <see cref="RecordingTime"/> identifier as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        YearRecording
    }
}
