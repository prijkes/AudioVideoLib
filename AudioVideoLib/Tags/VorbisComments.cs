namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

/// <summary>
/// Class to store vorbis comments.
/// </summary>
public sealed class VorbisComments
{
    private readonly NotifyingList<VorbisComment> _comments = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the vendor.
    /// </summary>
    /// <value>
    /// The vendor.
    /// </value>
    public string Vendor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the comments.
    /// </summary>
    /// <value>
    /// The comments.
    /// </value>
    /// <exception cref="System.ArgumentNullException">Thrown if value is null.</exception>
    public IList<VorbisComment> Comments => _comments;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the <see cref="VorbisComments" /> from the stream.
    /// </summary>
    /// <param name="stream">The stream to read the <see cref="VorbisComments" /> from.</param>
    /// <returns>
    /// A <see cref="VorbisComments" /> instance if found; otherwise, null.
    /// <para />
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// stream can not be read
    /// or
    /// stream can not be seeked
    /// </exception>
    public static VorbisComments? ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("stream can not be read");
        }

        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("stream can not be seeked");
        }

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);

        var startPosition = sb.Position;

        var vorbisComment = new VorbisComments();
        if (!vorbisComment.ReadStream(sb))
        {
            sb.Position = startPosition;
            return null;
        }
        return vorbisComment;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as VorbisComments);
    }

    /// <summary>
    /// Equals the specified <see cref="VorbisComments"/>.
    /// </summary>
    /// <param name="tag">The <see cref="VorbisComments"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    public bool Equals(VorbisComments? tag)
    {
        return tag is not null && (ReferenceEquals(this, tag) || (string.Equals(Vendor, tag.Vendor, StringComparison.OrdinalIgnoreCase) && Comments.SequenceEqual(tag.Comments)));
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Places the <see cref="VorbisComments"/> into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the <see cref="VorbisComments"/>.
    /// </returns>
    public byte[] ToByteArray()
    {
        var buf = new StreamBuffer();
        Vendor ??= string.Empty;
        buf.WriteInt(Encoding.UTF8.GetByteCount(Vendor));
        buf.WriteString(Vendor, Encoding.UTF8);
        buf.WriteInt(Comments.Count);
        foreach (var data in Comments.Where(c => c != null).Select(c => c.ToByteArray()))
        {
            buf.WriteInt(data.Length);
            buf.Write(data);
        }
        return buf.ToByteArray();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "Vorbis comment";
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private bool ReadStream(StreamBuffer sb)
    {
        ArgumentNullException.ThrowIfNull(sb);

        var vendorLength = sb.ReadInt32();
        if (vendorLength < 0 || vendorLength > sb.Length - sb.Position)
        {
            return false;
        }

        Vendor = sb.ReadString(vendorLength);

        var commentCount = sb.ReadInt32();
        if (commentCount < 0)
        {
            return false;
        }

        // Each comment is at minimum a 4-byte length prefix, so `commentCount` can never
        // legitimately exceed the remaining byte budget / 4.
        if (commentCount > (sb.Length - sb.Position) / 4)
        {
            return false;
        }

        _comments.Clear();
        for (var i = 0; i < commentCount && sb.Position < sb.Length; i++)
        {
            var comment = VorbisComment.ReadStream(sb);
            if (comment == null)
            {
                break;
            }

            _comments.Add(comment);
        }

        return true;
    }
}
