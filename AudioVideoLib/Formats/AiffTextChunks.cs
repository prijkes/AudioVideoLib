namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// A single comment record from an AIFF <c>COMT</c> chunk.
/// </summary>
/// <param name="TimeStamp">
/// Mac OS epoch timestamp (seconds since 1904-01-01 00:00:00 UTC). A value of 0 means "no timestamp".
/// </param>
/// <param name="MarkerId">Optional reference to a marker in the AIFF <c>MARK</c> chunk; 0 if none.</param>
/// <param name="Text">The comment text (ASCII, no trailing null).</param>
public sealed record AiffComment(uint TimeStamp, ushort MarkerId, string Text)
{
    /// <summary>
    /// The Mac OS epoch (1904-01-01 00:00:00 UTC) used by AIFF <c>COMT</c> timestamps.
    /// </summary>
    public static readonly DateTime MacEpoch = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets the timestamp interpreted as a UTC <see cref="DateTime"/>, or <c>null</c> if <see cref="TimeStamp"/> is 0.
    /// </summary>
    public DateTime? TimeStampUtc => TimeStamp == 0 ? null : MacEpoch.AddSeconds(TimeStamp);
}

/// <summary>
/// Bundle of the standard text-bearing chunks defined by the AIFF (1989) specification:
/// <c>NAME</c>, <c>AUTH</c>, <c>ANNO</c>, and <c>COMT</c>.
/// </summary>
/// <param name="Name">The contents of the <c>NAME</c> chunk, or <c>null</c> if absent.</param>
/// <param name="Author">The contents of the <c>AUTH</c> chunk, or <c>null</c> if absent.</param>
/// <param name="Annotation">The contents of the <c>ANNO</c> chunk, or <c>null</c> if absent.</param>
/// <param name="Comments">The list of comments parsed from the <c>COMT</c> chunk; empty if no <c>COMT</c> chunk was present.</param>
public sealed record AiffTextChunks(
    string? Name,
    string? Author,
    string? Annotation,
    IReadOnlyList<AiffComment> Comments)
{
    /// <summary>
    /// Gets a value indicating whether the bundle holds no text data at all.
    /// </summary>
    public bool IsEmpty =>
        Name is null && Author is null && Annotation is null && Comments.Count == 0;

    /// <summary>
    /// Parses a single <c>NAME</c>, <c>AUTH</c>, or <c>ANNO</c> chunk payload into a string.
    /// </summary>
    /// <param name="payload">The raw chunk payload (ASCII text, no null terminator).</param>
    /// <returns>The decoded ASCII string, or <c>null</c> if <paramref name="payload"/> is <c>null</c>.</returns>
    public static string? ReadText(byte[]? payload) =>
        payload is null ? null : Encoding.ASCII.GetString(payload);

    /// <summary>
    /// Parses a single <c>NAME</c>, <c>AUTH</c>, or <c>ANNO</c> chunk payload into a string.
    /// </summary>
    /// <param name="payload">The raw chunk payload (ASCII text, no null terminator).</param>
    /// <returns>The decoded ASCII string.</returns>
    /// <remarks>
    /// Span-based overload: lets callers pass slices of an existing buffer without
    /// allocating an intermediate <see cref="T:byte[]"/>.
    /// </remarks>
    public static string ReadText(ReadOnlySpan<byte> payload) =>
        Encoding.ASCII.GetString(payload);

    /// <summary>
    /// Parses a <c>COMT</c> chunk payload into a sequence of <see cref="AiffComment"/> records.
    /// </summary>
    /// <param name="payload">The raw <c>COMT</c> chunk payload.</param>
    /// <returns>
    /// The decoded list of comments, or <c>null</c> if <paramref name="payload"/> is <c>null</c> or malformed.
    /// </returns>
    public static IReadOnlyList<AiffComment>? ReadComments(byte[]? payload) =>
        payload is null ? null : ReadComments((ReadOnlySpan<byte>)payload);

    /// <summary>
    /// Parses a <c>COMT</c> chunk payload into a sequence of <see cref="AiffComment"/> records.
    /// </summary>
    /// <param name="payload">The raw <c>COMT</c> chunk payload.</param>
    /// <returns>The decoded list of comments, or <c>null</c> if the payload is malformed.</returns>
    /// <remarks>
    /// Span-based overload: lets callers pass slices of an existing buffer without
    /// allocating an intermediate <see cref="T:byte[]"/>.
    /// </remarks>
    public static IReadOnlyList<AiffComment>? ReadComments(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
        {
            return null;
        }

        var numComments = (ushort)((payload[0] << 8) | payload[1]);
        var comments = new List<AiffComment>(numComments);
        var pos = 2;
        for (var i = 0; i < numComments; i++)
        {
            // 4 (timestamp) + 2 (markerId) + 2 (count) = 8 bytes minimum per comment header.
            if (pos + 8 > payload.Length)
            {
                return null;
            }

            var timeStamp = (uint)((payload[pos] << 24) | (payload[pos + 1] << 16) | (payload[pos + 2] << 8) | payload[pos + 3]);
            var markerId = (ushort)((payload[pos + 4] << 8) | payload[pos + 5]);
            var count = (ushort)((payload[pos + 6] << 8) | payload[pos + 7]);
            pos += 8;

            if (pos + count > payload.Length)
            {
                return null;
            }

            var text = Encoding.ASCII.GetString(payload.Slice(pos, count));
            pos += count;

            // Per spec the text is padded to even length; the pad byte is NOT counted in count.
            if ((count & 1) != 0 && pos < payload.Length)
            {
                pos++;
            }

            comments.Add(new AiffComment(timeStamp, markerId, text));
        }

        return comments;
    }

    /// <summary>
    /// Serialises the bundle as a sequence of AIFF chunks (NAME, AUTH, ANNO, COMT, in that order),
    /// each with a 4-byte FOURCC, a 4-byte big-endian payload size, the payload, and a pad byte
    /// when the payload length is odd.
    /// </summary>
    /// <returns>
    /// A byte array suitable for splicing directly into an AIFF FORM body. Empty when no chunk has data.
    /// </returns>
    public byte[] ToByteArray()
    {
        using var ms = new MemoryStream();
        WriteTextChunk(ms, "NAME", Name);
        WriteTextChunk(ms, "AUTH", Author);
        WriteTextChunk(ms, "ANNO", Annotation);
        if (Comments.Count > 0)
        {
            WriteCommentsChunk(ms, Comments);
        }

        return ms.ToArray();
    }

    private static void WriteTextChunk(Stream s, string id, string? text)
    {
        if (text is null)
        {
            return;
        }

        var bytes = Encoding.ASCII.GetBytes(text);
        WriteHeader(s, id, (uint)bytes.Length);
        s.Write(bytes, 0, bytes.Length);
        if ((bytes.Length & 1) != 0)
        {
            s.WriteByte(0);
        }
    }

    private static void WriteCommentsChunk(Stream s, IReadOnlyList<AiffComment> comments)
    {
        using var body = new MemoryStream();
        body.WriteByte((byte)((comments.Count >> 8) & 0xFF));
        body.WriteByte((byte)(comments.Count & 0xFF));
        foreach (var c in comments)
        {
            var text = Encoding.ASCII.GetBytes(c.Text);
            var count = (ushort)text.Length;

            body.WriteByte((byte)((c.TimeStamp >> 24) & 0xFF));
            body.WriteByte((byte)((c.TimeStamp >> 16) & 0xFF));
            body.WriteByte((byte)((c.TimeStamp >> 8) & 0xFF));
            body.WriteByte((byte)(c.TimeStamp & 0xFF));
            body.WriteByte((byte)((c.MarkerId >> 8) & 0xFF));
            body.WriteByte((byte)(c.MarkerId & 0xFF));
            body.WriteByte((byte)((count >> 8) & 0xFF));
            body.WriteByte((byte)(count & 0xFF));
            body.Write(text, 0, text.Length);
            if ((count & 1) != 0)
            {
                body.WriteByte(0);
            }
        }

        var payload = body.ToArray();
        WriteHeader(s, "COMT", (uint)payload.Length);
        s.Write(payload, 0, payload.Length);
        if ((payload.Length & 1) != 0)
        {
            s.WriteByte(0);
        }
    }

    private static void WriteHeader(Stream s, string id, uint size)
    {
        var idBytes = Encoding.ASCII.GetBytes(id);
        s.Write(idBytes, 0, 4);
        s.WriteByte((byte)((size >> 24) & 0xFF));
        s.WriteByte((byte)((size >> 16) & 0xFF));
        s.WriteByte((byte)((size >> 8) & 0xFF));
        s.WriteByte((byte)(size & 0xFF));
    }
}
