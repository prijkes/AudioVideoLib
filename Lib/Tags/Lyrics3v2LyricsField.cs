/*
 * Date: 2012-12-09
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 lyrics field.
    /// </summary>
    public sealed class Lyrics3v2LyricsField : Lyrics3v2Field
    {
        // TimeStamp ([mm:ss] is 7 chars at least)
        private const int MinTimeStampLength = 7;

        private readonly Regex _regex = new Regex(@"\[(\d+):(\d+)\]");

        private readonly EventList<Lyrics3v2LyricLine> _lyricLines = new EventList<Lyrics3v2LyricLine>();

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
        public IList<Lyrics3v2LyricLine> LyricLines
        {
            get
            {
                return _lyricLines;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < LyricLines.Count; i++)
                {
                    Lyrics3v2LyricLine line = LyricLines[i];
                    foreach (TimeSpan timestamp in line.TimeStamps)
                        strBuilder.Append(timestamp.ToString(@"\[mm\:ss\]"));

                    strBuilder.Append(line.LyricLine);

                    // Only append a new line if the line is not empty; or if it's not the last line.
                    if (!String.IsNullOrEmpty(line.LyricLine) || (i + 1 < LyricLines.Count))
                        strBuilder.Append(Lyrics3v2Tag.NewLine);
                }
                return Encoding.ASCII.GetBytes(strBuilder.ToString());
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (!IsValidData(value))
                    throw new InvalidDataException("Data contains one ore more invalid characters.");

                using (StreamBuffer sb = new StreamBuffer(value))
                {
                    _lyricLines.Clear();
                    string lyrics = sb.ReadString(Encoding.ASCII);

                    // Lyrics, for example:
                    // [01:20]the real black[CR][LF]
                    // [CR][LF]
                    // Chorus:[CR][LF]
                    // [01:25][05:45]Time is tickin' away[CR][LF]
                    string[] entries = lyrics.Split(new[] { Lyrics3v2Tag.NewLine }, StringSplitOptions.None);
                    foreach (string entry in entries)
                    {
                        Lyrics3v2LyricLine lyricLine = new Lyrics3v2LyricLine();

                        if (entry.Length < MinTimeStampLength)
                        {
                            lyricLine.LyricLine = entry;
                            _lyricLines.Add(lyricLine);
                            continue;
                        }

                        List<TimeSpan> lyricsTimeSpans = new List<TimeSpan>();
                        int regexLength = 0;
                        MatchCollection matches = _regex.Matches(entry);
                        foreach (Match match in matches)
                        {
                            int minutes = Int32.Parse(match.Groups[1].Value);
                            int seconds = Int32.Parse(match.Groups[2].Value);
                            lyricsTimeSpans.Add(new TimeSpan(0, 0, minutes, seconds));
                            regexLength += MinTimeStampLength;
                        }

                        // Remove the timestamps from the line: [01:25][05:45]Time is tickin' away
                        lyricLine.LyricLine = entry.Substring(regexLength);

                        foreach (TimeSpan lyricTimeSpan in lyricsTimeSpans.OrderBy(x => x))
                            lyricLine.TimeStamps.Add(lyricTimeSpan);

                        _lyricLines.Add(lyricLine);
                    }
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Lyrics3v2Field audioFrame)
        {
            return Equals(audioFrame as Lyrics3v2LyricsField);
        }

        /// <summary>
        /// Equals the specified <see cref="Lyrics3v2LyricsField"/>.
        /// </summary>
        /// <param name="field">The <see cref="Lyrics3v2LyricsField"/>.</param>
        /// <returns>
        /// true if equal; false otherwise.
        /// </returns>
        public bool Equals(Lyrics3v2LyricsField field)
        {
            if (ReferenceEquals(null, field))
                return false;

            if (ReferenceEquals(this, field))
                return true;

            return String.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase) && field.LyricLines.SequenceEqual(LyricLines);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return Identifier.GetHashCode() * 397;
            }
        }
    }
}
