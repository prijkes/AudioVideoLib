/*
 * Date: 2011-08-13
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags;

/// <summary>
/// Class to store an Id3v2 tag.
/// </summary>
public partial class Id3v2Tag
{
    /// <summary>
    /// Gets or sets the file owner.
    /// </summary>
    /// <value>
    /// The owner of the file.
    /// </value>
    /// <remarks>
    /// The 'File owner/licensee' frame contains the name of the owner or licensee of the file and it's contents.
    /// <para />
    /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
    /// </remarks>
    public Id3v2TextFrame? FileOwner
    {
        get => GetVersionedTextFrame(Id3v2TextFrameIdentifier.FileOwner, Id3v2Version.Id3v230);
        set => SetVersionedTextFrame(value, Id3v2TextFrameIdentifier.FileOwner, Id3v2Version.Id3v230);
    }

    /// <summary>
    /// Gets or sets the name of the internet radio station.
    /// </summary>
    /// <value>
    /// The name of the internet radio station.
    /// </value>
    /// <remarks>
    /// The 'Internet radio station name' frame contains the name of the internet radio station from which the audio is streamed.
    /// <para />
    /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
    /// </remarks>
    public Id3v2TextFrame? InternetRadioStationName
    {
        get => GetVersionedTextFrame(Id3v2TextFrameIdentifier.InternetRadioStationName, Id3v2Version.Id3v230);
        set => SetVersionedTextFrame(value, Id3v2TextFrameIdentifier.InternetRadioStationName, Id3v2Version.Id3v230);
    }

    /// <summary>
    /// Gets or sets the internet radio station owner.
    /// </summary>
    /// <value>
    /// The internet radio station owner.
    /// </value>
    /// <remarks>
    /// The 'Internet radio station owner' frame contains 
    /// the name of the owner of the internet radio station from which the audio is streamed.
    /// <para />
    /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
    /// </remarks>
    public Id3v2TextFrame? InternetRadioStationOwner
    {
        get => GetVersionedTextFrame(Id3v2TextFrameIdentifier.InternetRadioStationOwner, Id3v2Version.Id3v230);
        set => SetVersionedTextFrame(value, Id3v2TextFrameIdentifier.InternetRadioStationOwner, Id3v2Version.Id3v230);
    }
}
