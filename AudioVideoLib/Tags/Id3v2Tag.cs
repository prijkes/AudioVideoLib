/*
 * Date: 2010-08-12
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://internet.ls-la.net/folklore/url-regexpr.html
 */

namespace AudioVideoLib.Tags;

using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Collections;

/// <summary>
/// Class to store an ID3v2 tag.
/// </summary>
/// <remarks>
/// An <see cref="Id3v2Tag"/> contains a header, <see cref="Id3v2Frame"/>s and optional <see cref="PaddingSize"/>.
/// </remarks>
public sealed partial class Id3v2Tag : IAudioTag
{
    /// <summary>
    /// The max amounts of <see cref="Id3v2Frame"/>s an <see cref="Id3v2Tag"/> can have.
    /// </summary>
    //// totalFrames <= MaxAllowedFrames
    public const int MaxAllowedFrames = (1024 * 1024) - 1; // or 0x000FFFFF

    /// <summary>
    /// The max size the <see cref="Id3v2Tag"/> can be.
    /// </summary>
    /// <remarks>
    /// The max size of an <see cref="Id3v2Tag"/> is 256MB.
    /// </remarks>
    //// The Id3v2 size is stored as a 32 bit synchsafe integer, making a total of 28 effective bits (representing up to 256MB).
    //// Hence, a larger value indicates an invalid size value.
    //// _totalFramesSize <= MaxAllowedSize
    public const int MaxAllowedSize = (1024 * 1024 * 256) - 1; // or 0x0FFFFFFF

    private readonly NotifyingList<Id3v2Frame> _frames = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2Tag"/> class.
    /// </summary>
    public Id3v2Tag()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2Tag"/> class.
    /// </summary>
    /// <param name="version">The tag version.</param>
    public Id3v2Tag(Id3v2Version version)
    {
        Version = version;

        Initialize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2Tag" /> class.
    /// </summary>
    /// <param name="version">The tag version.</param>
    /// <param name="flags">The flags.</param>
    public Id3v2Tag(Id3v2Version version, int flags)
    {
        Version = version;

        Flags = flags;

        Initialize();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the extended header.
    /// </summary>
    /// <value>
    /// The extended header.
    /// </value>
    public Id3v2ExtendedHeader ExtendedHeader
    {
        get;

        set
        {
            UseExtendedHeader = value != null;
            field = value!;
        }
    } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve frames after tag alteration.
    /// </summary>
    /// <value>
    /// <c>true</c> to preserve frames after tag alteration; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// When set to true, all frames will be written to the byte array when calling <see cref="ToByteArray"/>,
    /// regardless of the frame's <see cref="Id3v2Frame.TagAlterPreservation"/> flag.
    /// <para />
    /// Default is true.
    /// </remarks>
    public bool PreserveFramesAfterTagAlteration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preserve frames after file alteration.
    /// </summary>
    /// <value>
    /// <c>true</c> to preserve frames after file alteration; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// When set to true, all frames will be written to the byte array when calling <see cref="ToByteArray"/>,
    /// regardless of the frame's <see cref="Id3v2Frame.FileAlterPreservation"/> flag.
    /// <para />
    /// Default is true.
    /// </remarks>
    public bool PreserveFramesAfterFileAlteration { get; set; }

    /// <summary>
    /// Gets a read-only list of frames in the tag.
    /// </summary>
    /// <value>A read-only list of <see cref="Id3v2Frame"/>s in the tag.</value>
    public IEnumerable<Id3v2Frame> Frames
    {
        get
        {
            return _frames.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets or sets the amount of bytes to use as padding.
    /// </summary>
    /// <remarks>
    /// The 'Size of padding' is simply the total tag size excluding the frames and the headers, in other words the padding.
    /// <para />
    /// This does not include the <see cref="Id3v2ExtendedHeader.PaddingSize"/>.
    /// </remarks>
    public int PaddingSize { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Id3v2Tag);
    }

    /// <inheritdoc/>
    public bool Equals(IAudioTag? other)
    {
        return Equals(other as Id3v2Tag);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2Tag"/>.
    /// </summary>
    /// <param name="tag">The <see cref="Id3v2Tag"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    public bool Equals(Id3v2Tag? tag)
    {
        return tag is not null
            && (ReferenceEquals(this, tag)
                || ((tag.Version == Version)
                    && (tag.Flags == Flags)
                    && Equals(tag.ExtendedHeader, ExtendedHeader)
                    && tag.Frames.SequenceEqual(Frames)));
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
            return (Version.GetHashCode() * 397) ^ (UseHeader.GetHashCode() * 397);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var version = Version.ToString();
        return (version.Length == 1) ? "Id3v2" : version;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private void Initialize()
    {
        PreserveFramesAfterFileAlteration = PreserveFramesAfterTagAlteration = true;

        BindFrameEvents();
    }
}
