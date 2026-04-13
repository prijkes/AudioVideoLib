namespace AudioVideoLib.Tags;

using System.Collections.Generic;

/// <summary>
/// Class to store a Lyrics3v2 text field.
/// </summary>
public sealed partial class Lyrics3v2TextField
{
    private static readonly Dictionary<Lyrics3v2TextFieldIdentifier, string> Identifiers =
        new()
        {
                { Lyrics3v2TextFieldIdentifier.AdditionalInformation, "INF" },
                { Lyrics3v2TextFieldIdentifier.LyricsAuthorName, "AUT" },
                { Lyrics3v2TextFieldIdentifier.ExtendedAlbumName, "EAL" },
                { Lyrics3v2TextFieldIdentifier.ExtendedArtistName, "EAR" },
                { Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle, "ETT" },
                { Lyrics3v2TextFieldIdentifier.Genre, "GRE" }
            };
}
