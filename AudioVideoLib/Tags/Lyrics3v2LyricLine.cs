namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Collections;

/// <summary>
/// Class to store a Lyrics3v2 lyric line.
/// </summary>
public sealed class Lyrics3v2LyricLine
{
    private readonly NotifyingList<TimeSpan> _timeStamps = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2LyricLine"/> class.
    /// </summary>
    public Lyrics3v2LyricLine()
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets a list of time stamps for the lyric line.
    /// </summary>
    /// <value>
    /// A list of time stamps for the lyric line.
    /// </value>
    public IList<TimeSpan> TimeStamps
    {
        get
        {
            return _timeStamps;
        }
    }

    /// <summary>
    /// Gets or sets the lyric line.
    /// </summary>
    /// <value>
    /// The lyric line.
    /// </value>
    public string LyricLine
    {
        get;

        set
        {
            if (!string.IsNullOrEmpty(value) && !Lyrics3v2Field.IsValidString(value))
            {
                throw new InvalidDataException("Value contains one or more invalid characters.");
            }

            field = value;
        }
    } = null!;
}
