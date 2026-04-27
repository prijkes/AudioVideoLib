namespace AudioVideoLib.IO;

using System;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// Walks a Matroska / WebM (EBML) container looking for the <c>Tags</c> element of the first segment.
/// Does not decode codec payload from <c>Cluster</c> children.
/// </summary>
/// <remarks>
/// Supports round-trip serialisation: the original input bytes are preserved and a new <c>Tags</c>
/// element is spliced in (or appended) when <see cref="ToByteArray"/> is called.
/// </remarks>
public sealed class MatroskaStream : IMediaContainer
{
    /// <summary>The four-byte EBML header magic.</summary>
    public static readonly byte[] EbmlMagic = [0x1A, 0x45, 0xDF, 0xA3];

    /// <summary>EBML root element id.</summary>
    public const long EbmlId = 0x1A45DFA3;

    /// <summary>Segment element id.</summary>
    public const long SegmentId = 0x18538067;

    /// <summary>SeekHead element id.</summary>
    public const long SeekHeadId = 0x114D9B74;

    /// <summary>Info element id.</summary>
    public const long InfoId = 0x1549A966;

    /// <summary>TimecodeScale element id (uint).</summary>
    public const long TimecodeScaleId = 0x2AD7B1;

    /// <summary>Duration element id (float).</summary>
    public const long DurationId = 0x4489;

    /// <summary>Cluster element id (skipped).</summary>
    public const long ClusterId = 0x1F43B675;

    /// <summary>BlockGroup element id (skipped if it ever appears outside a Cluster).</summary>
    public const long BlockGroupId = 0xA0;

    /// <summary>Tracks element id.</summary>
    public const long TracksId = 0x1654AE6B;

    /// <summary>Cues element id.</summary>
    public const long CuesId = 0x1C53BB6B;

    /// <summary>Attachments element id.</summary>
    public const long AttachmentsId = 0x1941A469;

    /// <summary>Chapters element id.</summary>
    public const long ChaptersId = 0x1043A770;

    /// <summary>DocType element id (ASCII string, inside EBML header).</summary>
    public const long DocTypeId = 0x4282;

    private const int MaxRecursionDepth = 16;

    private byte[] _originalBytes = [];
    private long _segmentHeaderStart;
    private long _segmentPayloadStart;
    private long _segmentPayloadEnd;
    private bool _segmentSizeWasUnknown;
    private long _tagsHeaderStart;
    private long _tagsEndOffset;
    private bool _hasTagsElement;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration
    {
        get
        {
            // Duration is in TimecodeScale units (which is in nanoseconds; default 1000000 = 1ms).
            if (Duration <= 0 || TimecodeScale == 0)
            {
                return 0;
            }

            var nanoseconds = Duration * TimecodeScale;
            return (long)(nanoseconds / 1_000_000.0);
        }
    }

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; } = 0;

    /// <summary>Gets the EBML <c>DocType</c> declared in the header (e.g. <c>"matroska"</c>, <c>"webm"</c>).</summary>
    public string DocType { get; private set; } = string.Empty;

    /// <summary>Gets the segment <c>TimecodeScale</c> in nanoseconds per tick (default 1,000,000 = 1ms).</summary>
    public long TimecodeScale { get; private set; } = 1_000_000;

    /// <summary>Gets the raw <c>Duration</c> value (in <c>TimecodeScale</c> units), or 0 if absent.</summary>
    public double Duration { get; private set; }

    /// <summary>Gets the parsed <c>Tags</c> element, aggregating all top-level Tag entries from the first segment.</summary>
    /// <remarks>Always non-<c>null</c>; an empty value indicates the segment had no <c>Tags</c> element.</remarks>
    /// <remarks>
    /// Setter accepts <c>null</c> as shorthand for "clear all metadata"; the property
    /// is reset to a fresh empty <see cref="MatroskaTag"/> in that case.
    /// </remarks>
    public MatroskaTag Tag
    {
        get;
        set => field = value ?? new MatroskaTag();
    } = new();

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < 4)
        {
            return false;
        }

        var magic = new byte[4];
        if (stream.Read(magic, 0, 4) != 4 ||
            magic[0] != EbmlMagic[0] || magic[1] != EbmlMagic[1] ||
            magic[2] != EbmlMagic[2] || magic[3] != EbmlMagic[3])
        {
            stream.Position = start;
            return false;
        }

        stream.Position = start;
        StartOffset = start;

        // Cache the original bytes so ToByteArray() can splice the Tags element back in.
        var fileLen = stream.Length - start;
        _originalBytes = new byte[fileLen];
        var read = 0;
        while (read < _originalBytes.Length)
        {
            var n = stream.Read(_originalBytes, read, _originalBytes.Length - read);
            if (n <= 0)
            {
                break;
            }

            read += n;
        }

        if (read < _originalBytes.Length)
        {
            Array.Resize(ref _originalBytes, read);
        }

        EndOffset = start + _originalBytes.Length;
        stream.Position = EndOffset;

        // Walk the in-memory buffer.
        using var ms = new MemoryStream(_originalBytes, writable: false);
        return WalkTopLevel(ms);
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = ToByteArray();
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Buffer-shaped override: kept as the fast path for callers who want bytes in hand.
    /// </remarks>
    public byte[] ToByteArray()
    {
        if (_originalBytes.Length == 0)
        {
            return [];
        }

        var newTagsBytes = Tag.Entries.Count > 0 ? Tag.ToByteArray() : [];

        return _hasTagsElement
            ? SpliceTags(newTagsBytes)
            : newTagsBytes.Length == 0
                ? (byte[])_originalBytes.Clone()
                : AppendTagsToSegment(newTagsBytes);
    }

    private bool WalkTopLevel(Stream stream)
    {
        var fileEnd = stream.Length;

        // Parse EBML header.
        if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
            !EbmlElement.TryReadVintSize(stream, out _, out var size, out var unknown))
        {
            return false;
        }

        if (id != EbmlId)
        {
            return false;
        }

        var headerEnd = unknown ? fileEnd : Math.Min(stream.Position + size, fileEnd);
        ParseEbmlHeader(stream, headerEnd);
        stream.Position = headerEnd;

        // Walk the first Segment element.
        while (stream.Position < fileEnd)
        {
            var elemHeaderStart = stream.Position;
            if (!EbmlElement.TryReadVintId(stream, out _, out var topId) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var topSize, out var topUnknown))
            {
                break;
            }

            var payloadStart = stream.Position;
            var available = fileEnd - payloadStart;
            var actualSize = topUnknown ? available : topSize;
            if (actualSize < 0 || actualSize > available)
            {
                actualSize = available;
            }

            if (topId == SegmentId)
            {
                _segmentHeaderStart = elemHeaderStart;
                _segmentPayloadStart = payloadStart;
                _segmentPayloadEnd = payloadStart + actualSize;
                _segmentSizeWasUnknown = topUnknown;
                ParseSegment(stream, _segmentPayloadEnd);
                stream.Position = _segmentPayloadEnd;
                return true;
            }

            stream.Position = payloadStart + actualSize;
        }

        return false;
    }

    private void ParseEbmlHeader(Stream stream, long end)
    {
        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var size, out var unknown))
            {
                return;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : size;
            if (actualSize < 0 || actualSize > available)
            {
                return;
            }

            if (id == DocTypeId)
            {
                var buf = new byte[actualSize];
                _ = stream.Read(buf, 0, buf.Length);
                DocType = Encoding.ASCII.GetString(buf).TrimEnd('\0');
            }
            else
            {
                stream.Position = payloadStart + actualSize;
            }
        }
    }

    private void ParseSegment(Stream stream, long end)
    {
        var depth = 0;
        while (stream.Position < end)
        {
            var headerStart = stream.Position;
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var size, out var unknown))
            {
                return;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : size;
            if (actualSize < 0 || actualSize > available)
            {
                return;
            }

            // Skip massive media chunks outright.
            if (id is ClusterId or BlockGroupId)
            {
                stream.Position = payloadStart + actualSize;
                continue;
            }

            if (id == InfoId)
            {
                ParseInfo(stream, payloadStart + actualSize, depth + 1);
                stream.Position = payloadStart + actualSize;
                continue;
            }

            if (id == MatroskaElementIds.Tags)
            {
                _tagsHeaderStart = headerStart;
                _tagsEndOffset = payloadStart + actualSize;
                _hasTagsElement = true;
                var payloadBytes = new byte[actualSize];
                _ = stream.Read(payloadBytes, 0, payloadBytes.Length);
                Tag = MatroskaTag.FromPayload(payloadBytes);
                continue;
            }

            // Skip everything else (SeekHead, Tracks, Cues, Attachments, Chapters, ...).
            stream.Position = payloadStart + actualSize;
        }
    }

    private void ParseInfo(Stream stream, long end, int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            return;
        }

        while (stream.Position < end)
        {
            if (!EbmlElement.TryReadVintId(stream, out _, out var id) ||
                !EbmlElement.TryReadVintSize(stream, out _, out var size, out var unknown))
            {
                return;
            }

            var payloadStart = stream.Position;
            var available = end - payloadStart;
            var actualSize = unknown ? available : size;
            if (actualSize < 0 || actualSize > available)
            {
                return;
            }

            if (id == TimecodeScaleId)
            {
                var buf = new byte[actualSize];
                _ = stream.Read(buf, 0, buf.Length);
                TimecodeScale = EbmlElement.DecodeUInt(buf);
                if (TimecodeScale == 0)
                {
                    TimecodeScale = 1_000_000;
                }
            }
            else if (id == DurationId)
            {
                var buf = new byte[actualSize];
                _ = stream.Read(buf, 0, buf.Length);
                Duration = EbmlElement.DecodeFloat(buf);
            }
            else
            {
                stream.Position = payloadStart + actualSize;
            }
        }
    }

    private byte[] SpliceTags(byte[] newTagsBytes)
    {
        // Existing Tags element extends from _tagsHeaderStart to _tagsEndOffset (offsets relative to the buffer start).
        // Build: prefix [0, _tagsHeaderStart) + newTagsBytes + suffix [_tagsEndOffset, end).
        // Then update the segment size VINT if it was a known size.
        var prefixLen = (int)_tagsHeaderStart;
        var suffix = _originalBytes.AsSpan((int)_tagsEndOffset).ToArray();

        // Compute the new segment payload size.
        var oldSegPayload = _segmentPayloadEnd - _segmentPayloadStart;
        var oldTagsSize = _tagsEndOffset - _tagsHeaderStart;
        var newSegPayload = oldSegPayload - oldTagsSize + newTagsBytes.Length;

        return RebuildWithSegmentSize(prefixLen, newTagsBytes, suffix, newSegPayload);
    }

    private byte[] AppendTagsToSegment(byte[] newTagsBytes)
    {
        // Append at the end of the existing segment payload.
        var prefixLen = (int)_segmentPayloadEnd;
        var suffix = _originalBytes.AsSpan(prefixLen).ToArray();

        var newSegPayload = _segmentPayloadEnd - _segmentPayloadStart + newTagsBytes.Length;
        return RebuildWithSegmentSize(prefixLen, newTagsBytes, suffix, newSegPayload);
    }

    private byte[] RebuildWithSegmentSize(int prefixLen, byte[] inserted, byte[] suffix, long newSegPayload)
    {
        // The segment header consists of: [_segmentHeaderStart .. _segmentPayloadStart)
        // = id VINT (4 bytes for the canonical Matroska Segment id) + size VINT.
        var idBytes = EbmlElement.EncodeId(SegmentId);
        var oldSegHeaderLen = (int)(_segmentPayloadStart - _segmentHeaderStart);
        var oldSegSizeVintLen = oldSegHeaderLen - idBytes.Length;

        byte[] newSegSizeVint;
        if (_segmentSizeWasUnknown)
        {
            // Preserve length-unknown encoding using the same VINT length as before.
            newSegSizeVint = MakeUnknownSizeVint(oldSegSizeVintLen <= 0 ? 1 : oldSegSizeVintLen);
        }
        else
        {
            newSegSizeVint = EbmlElement.EncodeVintSize(newSegPayload, oldSegSizeVintLen <= 0 ? 8 : oldSegSizeVintLen);
        }

        var newSegHeaderLen = idBytes.Length + newSegSizeVint.Length;
        var prefix = _originalBytes.AsSpan(0, prefixLen).ToArray();
        var totalLen = prefix.Length + inserted.Length + suffix.Length;

        // If the segment header changed length (it shouldn't, since we passed the same VINT length),
        // we'd need to rebuild prefix from scratch — but we forced same-length encoding.
        if (newSegHeaderLen != oldSegHeaderLen)
        {
            // Fallback: rebuild without the old segment header bytes.
            var preSeg = _originalBytes.AsSpan(0, (int)_segmentHeaderStart).ToArray();
            var postSegHeader = _originalBytes.AsSpan((int)_segmentPayloadStart, prefixLen - (int)_segmentPayloadStart).ToArray();
            var combined = new byte[preSeg.Length + newSegHeaderLen + postSegHeader.Length + inserted.Length + suffix.Length];
            var pos = 0;
            Buffer.BlockCopy(preSeg, 0, combined, pos, preSeg.Length);
            pos += preSeg.Length;
            Buffer.BlockCopy(idBytes, 0, combined, pos, idBytes.Length);
            pos += idBytes.Length;
            Buffer.BlockCopy(newSegSizeVint, 0, combined, pos, newSegSizeVint.Length);
            pos += newSegSizeVint.Length;
            Buffer.BlockCopy(postSegHeader, 0, combined, pos, postSegHeader.Length);
            pos += postSegHeader.Length;
            Buffer.BlockCopy(inserted, 0, combined, pos, inserted.Length);
            pos += inserted.Length;
            Buffer.BlockCopy(suffix, 0, combined, pos, suffix.Length);
            return combined;
        }

        // Patch the segment size VINT in-place inside `prefix`.
        Buffer.BlockCopy(newSegSizeVint, 0, prefix, (int)_segmentHeaderStart + idBytes.Length, newSegSizeVint.Length);

        var output = new byte[totalLen];
        var off = 0;
        Buffer.BlockCopy(prefix, 0, output, off, prefix.Length);
        off += prefix.Length;
        Buffer.BlockCopy(inserted, 0, output, off, inserted.Length);
        off += inserted.Length;
        Buffer.BlockCopy(suffix, 0, output, off, suffix.Length);
        return output;
    }

    private static byte[] MakeUnknownSizeVint(int length)
    {
        if (length is < 1 or > 8)
        {
            length = 1;
        }

        var buf = new byte[length];
        // All data bits set, marker bit at position (8 - length) of the high byte.
        var markerMask = (byte)(0x80 >> (length - 1));
        // For length L: high byte value = markerMask | ((markerMask - 1)) — all bits below the marker set.
        // Then low (length - 1) bytes are 0xFF.
        buf[0] = (byte)(markerMask | (markerMask - 1));
        for (var i = 1; i < length; i++)
        {
            buf[i] = 0xFF;
        }

        return buf;
    }

    /// <summary>
    /// Helper for tests / writers: build a complete Matroska file containing only the EBML header and
    /// a Segment whose payload is exactly <paramref name="segmentPayload"/>.
    /// </summary>
    /// <param name="docType">DocType (e.g. <c>"matroska"</c>).</param>
    /// <param name="segmentPayload">Raw segment payload bytes.</param>
    /// <returns>The bytes of a minimal valid EBML container.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="segmentPayload"/> is <c>null</c>.</exception>
    public static byte[] BuildMinimalContainer(string docType, byte[] segmentPayload)
    {
        ArgumentNullException.ThrowIfNull(segmentPayload);
        docType ??= "matroska";
        var docTypeBytes = Encoding.ASCII.GetBytes(docType);

        using var headerBody = new MemoryStream();
        WriteRawElement(headerBody, DocTypeId, docTypeBytes);
        var headerBytes = headerBody.ToArray();

        using var ms = new MemoryStream();
        WriteRawElement(ms, EbmlId, headerBytes);
        WriteRawElement(ms, SegmentId, segmentPayload);
        return ms.ToArray();
    }

    private static void WriteRawElement(Stream stream, long id, byte[] payload)
    {
        var idBytes = EbmlElement.EncodeId(id);
        var sizeBytes = EbmlElement.EncodeVintSize(payload.Length);
        stream.Write(idBytes, 0, idBytes.Length);
        stream.Write(sizeBytes, 0, sizeBytes.Length);
        stream.Write(payload, 0, payload.Length);
    }
}
