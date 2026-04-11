/*
 * Date: 2012-12-08
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System.Collections.Generic;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing a text frame.
    /// </summary>
    public sealed partial class Id3v2TextFrame
    {
        private static readonly Dictionary<Id3v2TextFrameIdentifier, Dictionary<string, Id3v2Version[]>> Identifiers =
            new Dictionary<Id3v2TextFrameIdentifier, Dictionary<string, Id3v2Version[]>>
                {
                    {
                        Id3v2TextFrameIdentifier.AlbumSortOrder,
                        new Dictionary<string, Id3v2Version[]> { { "TSOA", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.AlbumTitle,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TAL", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TALB", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.Artist,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TP1", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPE1", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ArtistExtra,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TP2", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPE2", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.AudioSize,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TSI", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TSIZ", new[] { Id3v2Version.Id3v230 } },
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.BeatsPerMinute,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TBP", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TBPM", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ComposerName,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TCM", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TCOM", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ConductorName,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TP3", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPE3", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ContentGroupDescription,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TT1", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TIT1", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ContentType,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TCO", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TCON", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.CopyrightMessage,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TCR", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TCOP", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.DateRecording,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TDA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TDAT", new[] { Id3v2Version.Id3v230 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.EncodedBy,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TEN", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TENC", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.EncodingSettingsUsed,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TSS", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TSSE", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.EncodingTime,
                        new Dictionary<string, Id3v2Version[]> { { "TDEN", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.FileOwner,
                        new Dictionary<string, Id3v2Version[]>
                            { { "TOWN", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.FileType,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TFT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TFLT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.InitialKey,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TKE", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TKEY", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.InternationalStandardRecordingCode,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TRC", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TSRC", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.InternetRadioStationName,
                        new Dictionary<string, Id3v2Version[]>
                            { { "TRSN", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.InternetRadioStationOwner,
                        new Dictionary<string, Id3v2Version[]>
                            { { "TRSO", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.InvolvedPeopleList,
                        new Dictionary<string, Id3v2Version[]> { { "TIPL", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.Length,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TLE", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TLEN", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.MediaType,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TMT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TMED", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ModifiedBy,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TP4", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPE4", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.Mood,
                        new Dictionary<string, Id3v2Version[]> { { "TMOO", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.MusicianCreditsList,
                        new Dictionary<string, Id3v2Version[]> { { "TMCL", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalAlbumTitle,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TOT", new[] { Id3v2Version.Id3v221, Id3v2Version.Id3v220 } },
                                { "TOAL", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalArtist,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TOA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TOPE", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalFilename,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TOF", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TOFN", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalReleaseTime,
                        new Dictionary<string, Id3v2Version[]> { { "TDOR", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalReleaseYear,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TOR", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TORY", new[] { Id3v2Version.Id3v230 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.OriginalTextWriter,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TOL", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TOLY", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.PartOfSet,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TPA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPOS", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.PerformerSortOrder,
                        new Dictionary<string, Id3v2Version[]> { { "TSOP", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.PlaylistDelay,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TDY", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TDLY", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.ProducedNote,
                        new Dictionary<string, Id3v2Version[]> { { "TPRO", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.Publisher,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TPB", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TPUB", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.RecordingDates,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TRD", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TRDA", new[] { Id3v2Version.Id3v230 } },
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.RecordingTime,
                        new Dictionary<string, Id3v2Version[]> { { "TDRC", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.ReleaseTime,
                        new Dictionary<string, Id3v2Version[]> { { "TDRL", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.SetSubtitle,
                        new Dictionary<string, Id3v2Version[]> { { "TSST", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.TaggingTime,
                        new Dictionary<string, Id3v2Version[]> { { "TDTG", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.TextLanguages,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TLA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TLAN", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.TextWriter,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TXT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TEXT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.TimeRecording,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TIM", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TIME", new[] { Id3v2Version.Id3v230 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.TitleSortOrder,
                        new Dictionary<string, Id3v2Version[]> { { "TSOT", new[] { Id3v2Version.Id3v240 } } }
                    },
                    {
                        Id3v2TextFrameIdentifier.TrackNumber,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TRK", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TRCK", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.TrackTitle,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TT2", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TIT2", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.TrackTitleDescription,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TT3", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TIT3", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } }
                            }
                    },
                    {
                        Id3v2TextFrameIdentifier.YearRecording,
                        new Dictionary<string, Id3v2Version[]>
                            {
                                { "TYE", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                                { "TYER", new[] { Id3v2Version.Id3v230 } }
                            }
                    }
                };
    }
}
