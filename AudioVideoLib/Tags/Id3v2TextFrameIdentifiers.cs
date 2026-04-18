namespace AudioVideoLib.Tags;

using System.Collections.Generic;

/// <summary>
/// Class for storing a text frame.
/// </summary>
public sealed partial class Id3v2TextFrame
{
    private static readonly Dictionary<Id3v2TextFrameIdentifier, Dictionary<string, Id3v2Version[]>> Identifiers =
        new()
        {
                {
                    Id3v2TextFrameIdentifier.AlbumSortOrder,
                    new Dictionary<string, Id3v2Version[]> { { "TSOA", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.AlbumTitle,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TAL", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TALB", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.Artist,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TP1", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPE1", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ArtistExtra,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TP2", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPE2", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.AudioSize,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TSI", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TSIZ", [Id3v2Version.Id3v230 ] },
                        }
                },
                {
                    Id3v2TextFrameIdentifier.BeatsPerMinute,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TBP", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TBPM", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ComposerName,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TCM", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TCOM", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ConductorName,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TP3", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPE3", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ContentGroupDescription,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TT1", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TIT1", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ContentType,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TCO", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TCON", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.CopyrightMessage,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TCR", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TCOP", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.DateRecording,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TDA", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TDAT", [Id3v2Version.Id3v230 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.EncodedBy,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TEN", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TENC", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.EncodingSettingsUsed,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TSS", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TSSE", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.EncodingTime,
                    new Dictionary<string, Id3v2Version[]> { { "TDEN", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.FileOwner,
                    new Dictionary<string, Id3v2Version[]>
                        { { "TOWN", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.FileType,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TFT", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TFLT", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.InitialKey,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TKE", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TKEY", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.InternationalStandardRecordingCode,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TRC", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TSRC", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.InternetRadioStationName,
                    new Dictionary<string, Id3v2Version[]>
                        { { "TRSN", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.InternetRadioStationOwner,
                    new Dictionary<string, Id3v2Version[]>
                        { { "TRSO", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.InvolvedPeopleList,
                    new Dictionary<string, Id3v2Version[]> { { "TIPL", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.Length,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TLE", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TLEN", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.MediaType,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TMT", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TMED", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ModifiedBy,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TP4", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPE4", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.Mood,
                    new Dictionary<string, Id3v2Version[]> { { "TMOO", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.MusicianCreditsList,
                    new Dictionary<string, Id3v2Version[]> { { "TMCL", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalAlbumTitle,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TOT", [Id3v2Version.Id3v221, Id3v2Version.Id3v220 ] },
                            { "TOAL", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalArtist,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TOA", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TOPE", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalFilename,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TOF", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TOFN", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalReleaseTime,
                    new Dictionary<string, Id3v2Version[]> { { "TDOR", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalReleaseYear,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TOR", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TORY", [Id3v2Version.Id3v230 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.OriginalTextWriter,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TOL", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TOLY", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.PartOfSet,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TPA", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPOS", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.PerformerSortOrder,
                    new Dictionary<string, Id3v2Version[]> { { "TSOP", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.PlaylistDelay,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TDY", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TDLY", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.ProducedNote,
                    new Dictionary<string, Id3v2Version[]> { { "TPRO", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.Publisher,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TPB", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TPUB", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.RecordingDates,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TRD", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TRDA", [Id3v2Version.Id3v230 ] },
                        }
                },
                {
                    Id3v2TextFrameIdentifier.RecordingTime,
                    new Dictionary<string, Id3v2Version[]> { { "TDRC", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.ReleaseTime,
                    new Dictionary<string, Id3v2Version[]> { { "TDRL", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.SetSubtitle,
                    new Dictionary<string, Id3v2Version[]> { { "TSST", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.TaggingTime,
                    new Dictionary<string, Id3v2Version[]> { { "TDTG", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.TextLanguages,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TLA", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TLAN", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.TextWriter,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TXT", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TEXT", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.TimeRecording,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TIM", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TIME", [Id3v2Version.Id3v230 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.TitleSortOrder,
                    new Dictionary<string, Id3v2Version[]> { { "TSOT", [Id3v2Version.Id3v240 ] } }
                },
                {
                    Id3v2TextFrameIdentifier.TrackNumber,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TRK", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TRCK", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.TrackTitle,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TT2", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TIT2", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.TrackTitleDescription,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TT3", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TIT3", [Id3v2Version.Id3v230, Id3v2Version.Id3v240 ] }
                        }
                },
                {
                    Id3v2TextFrameIdentifier.YearRecording,
                    new Dictionary<string, Id3v2Version[]>
                        {
                            { "TYE", [Id3v2Version.Id3v220, Id3v2Version.Id3v221 ] },
                            { "TYER", [Id3v2Version.Id3v230 ] }
                        }
                }
            };
}
