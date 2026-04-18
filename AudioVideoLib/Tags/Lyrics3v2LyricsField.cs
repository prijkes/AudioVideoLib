namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

/// <summary>
/// Class to store a Lyrics3v2 lyrics field.
/// </summary>
public sealed partial class Lyrics3v2LyricsField : Lyrics3v2Field
{
    // TimeStamp ([mm:ss] is 7 chars at least)
    private const int MinTimeStampLength = 7;

    private readonly NotifyingList<Lyrics3v2LyricLine> _lyricLines = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2LyricsField"/> class.
    /// </summary>
    public Lyrics3v2LyricsField() : base("LYR")
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the lyric lines.
    /// </summary>
    /// <value>
    /// The lyric lines.
    /// </value>
    public IList<Lyrics3v2LyricLine> LyricLines => _lyricLines;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[]? Data
    {
        get
        {
            var strBuilder = new StringBuilder();
            for (var i = 0; i < LyricLines.Count; i++)
            {
                var line = LyricLines[i];
                foreach (var timestamp in line.TimeStamps)
                {
                    strBuilder.Append(timestamp.ToString(@"\[mm\:ss\]"));
                }

                strBuilder.Append(line.LyricLine);

                // Only append a new line if the line is not empty; or if it's not the last line.
                if (!string.IsNullOrEmpty(line.LyricLine) || (i + 1 < LyricLines.Count))
                {
                    strBuilder.Append(Lyrics3v2Tag.NewLine);
                }
            }
            return Encoding.ASCII.GetBytes(strBuilder.ToString());
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsValidData(value))
            {
                throw new InvalidDataException("Data contains one ore more invalid characters.");
            }

            var sb = new StreamBuffer(value);
            _lyricLines.Clear();
            var lyrics = sb.ReadString(Encoding.ASCII);

            // Lyrics, for example:
            // [01:20]the real black[CR][LF]
            // [CR][LF]
            // Chorus:[CR][LF]
            // [01:25][05:45]Time is tickin' away[CR][LF]
            var entries = lyrics.Split([Lyrics3v2Tag.NewLine], StringSplitOptions.None);
            foreach (var entry in entries)
            {
                var lyricLine = new Lyrics3v2LyricLine();

                if (entry.Length < MinTimeStampLength)
                {
                    lyricLine.LyricLine = entry;
                    _lyricLines.Add(lyricLine);
                    continue;
                }

                List<TimeSpan> lyricsTimeSpans = [];
                var regexLength = 0;
                var matches = TimeStampPattern().Matches(entry);
                foreach (Match match in matches)
                {
                    var minutes = int.Parse(match.Groups[1].Value);
                    var seconds = int.Parse(match.Groups[2].Value);
                    lyricsTimeSpans.Add(new TimeSpan(0, 0, minutes, seconds));
                    regexLength += MinTimeStampLength;
                }

                // Remove the timestamps from the line: [01:25][05:45]Time is tickin' away
                lyricLine.LyricLine = entry[regexLength..];

                foreach (var lyricTimeSpan in lyricsTimeSpans.OrderBy(x => x))
                {
                    lyricLine.TimeStamps.Add(lyricTimeSpan);
                }

                _lyricLines.Add(lyricLine);
            }
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Lyrics3v2Field? audioFrame) => Equals(audioFrame as Lyrics3v2LyricsField);

    /// <summary>
    /// Equals the specified <see cref="Lyrics3v2LyricsField"/>.
    /// </summary>
    /// <param name="field">The <see cref="Lyrics3v2LyricsField"/>.</param>
    /// <returns>
    /// true if equal; false otherwise.
    /// </returns>
    public bool Equals(Lyrics3v2LyricsField? field)
    {
        return field is not null && (ReferenceEquals(this, field) || (string.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase) && field.LyricLines.SequenceEqual(LyricLines)));
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="T:System.Object"/>.
    /// </returns>
    /// The value should be calculated on immutable fields only.
    public override int GetHashCode()
    {
        unchecked
        {
            return Identifier.GetHashCode() * 397;
        }
    }

    [GeneratedRegex(@"\[(\d+):(\d+)\]")]
    private static partial Regex TimeStampPattern();
}
