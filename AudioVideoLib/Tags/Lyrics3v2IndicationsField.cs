/*
 * Date: 2012-12-09
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class to store a Lyrics3v2 indications field.
/// </summary>
public sealed class Lyrics3v2IndicationsField : Lyrics3v2Field
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2IndicationsField"/> class.
    /// </summary>
    public Lyrics3v2IndicationsField() : base("IND")
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether the lyrics field is present.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the lyrics field is present; otherwise, <c>false</c>.
    /// </value>
    public bool LyricsFieldPresent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is a timestamp in the lyrics.
    /// </summary>
    /// <value>
    ///   <c>true</c> if there is a timestamp in the lyrics; otherwise, <c>false</c>.
    /// </value>
    public bool LyricsContainTimeStamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to inhibit tracks for random selection.
    /// </summary>
    /// <value>
    ///   <c>true</c> if tracks should be inhibited; otherwise, <c>false</c>.
    /// </value>
    public bool InhibitTrack { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[]? Data
    {
        get
        {
            var sb = new StreamBuffer();
            sb.WriteString(LyricsFieldPresent ? "1" : "0");
            sb.WriteString(LyricsContainTimeStamp ? "1" : "0");
            sb.WriteString(InhibitTrack ? "1" : "0");
            return sb.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var sb = new StreamBuffer(value);
            var fieldSize = value.Length;
            var ind = sb.ReadString(fieldSize);
            LyricsFieldPresent = (fieldSize >= 1) && string.Equals(ind[..1], "1", StringComparison.OrdinalIgnoreCase);
            LyricsContainTimeStamp = (fieldSize >= 2) && string.Equals(ind.Substring(1, 1), "1", StringComparison.OrdinalIgnoreCase);
            InhibitTrack = (fieldSize >= 3) && string.Equals(ind.Substring(2, 1), "1", StringComparison.OrdinalIgnoreCase);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Lyrics3v2Field? audioFrame)
    {
        return Equals(audioFrame as Lyrics3v2IndicationsField);
    }

    /// <summary>
    /// Equals the specified <see cref="Lyrics3v2IndicationsField"/>.
    /// </summary>
    /// <param name="field">The <see cref="Lyrics3v2IndicationsField"/>.</param>
    /// <returns>
    /// true if equal; false otherwise.
    /// </returns>
    public bool Equals(Lyrics3v2IndicationsField? field)
    {
        return field is not null && (ReferenceEquals(this, field) || (string.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase)
               && (field.LyricsFieldPresent == LyricsFieldPresent)
               && (field.LyricsContainTimeStamp == LyricsContainTimeStamp) && (field.InhibitTrack == InhibitTrack)));
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
