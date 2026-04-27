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
/// Supports round-trip serialisation: the source bytes are kept reachable via an
/// <see cref="ISourceReader"/> reference (no full-file copy in memory), and a new
/// <c>Tags</c> element is spliced in (or appended) at <see cref="WriteTo(Stream)"/> time
/// by streaming unchanged regions directly from the source. This is what makes editing
/// a 40 GB MKV viable: peak memory stays in the low MB range regardless of file size.
/// <para />
/// Callers must keep the underlying source (the <see cref="Stream"/> passed to
/// <see cref="ReadStream(Stream)"/>) alive until <see cref="WriteTo(Stream)"/> /
/// <c>ToByteArray</c> finishes — the walker reads from it on demand.
/// </remarks>
public sealed class MatroskaStream : IMediaContainer, IDisposable
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

    // Random-access source captured at ReadStream time. Offsets below are relative to the
    // start of the source (i.e. relative to the position the stream was at when ReadStream
    // was called).
    private ISourceReader? _source;
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
    /// <remarks>
    /// Always non-<c>null</c>; an empty value indicates the segment had no <c>Tags</c> element.
    /// Setter accepts <c>null</c> as shorthand for "clear all metadata".
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

        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4 ||
            magic[0] != EbmlMagic[0] || magic[1] != EbmlMagic[1] ||
            magic[2] != EbmlMagic[2] || magic[3] != EbmlMagic[3])
        {
            stream.Position = start;
            return false;
        }

        stream.Position = start;
        StartOffset = start;

        // Wrap the source for random-access reads at write time. The full file is NOT
        // copied — that's the whole point. The reader leaves the underlying stream open;
        // it's the caller's job to keep it alive until WriteTo finishes.
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        // Walk the structure on the live stream. Cluster/BlockGroup payloads are seeked
        // past, not buffered, so a 40 GB MKV walks in O(metadata) bytes touched.
        var ok = WalkTopLevel(stream);

        // Leave the stream positioned at the end of the matroska container so the
        // outer scanner (MediaContainers) can continue past us.
        stream.Position = EndOffset;
        return ok;
    }

    /// <inheritdoc/>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            return;
        }

        var newTagsBytes = Tag.Entries.Count > 0 ? Tag.ToByteArray() : [];

        if (!_hasTagsElement && newTagsBytes.Length == 0)
        {
            // No edits, no Tags element — copy the source verbatim.
            _source.CopyTo(0, _source.Length, destination);
            return;
        }

        if (_hasTagsElement)
        {
            SpliceTagsStreaming(destination, newTagsBytes);
        }
        else
        {
            AppendTagsStreaming(destination, newTagsBytes);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Buffer-shaped fallback: materialises the full output in memory. For multi-GB
    /// containers prefer <see cref="WriteTo(Stream)"/>, which streams chunks directly
    /// from the source to the destination.
    /// </remarks>
    public byte[] ToByteArray()
    {
        if (_source is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        WriteTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not dispose the user's
    /// source <see cref="Stream"/>; the caller still owns that.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
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
                _segmentHeaderStart = elemHeaderStart - StartOffset;
                _segmentPayloadStart = payloadStart - StartOffset;
                _segmentPayloadEnd = payloadStart + actualSize - StartOffset;
                _segmentSizeWasUnknown = topUnknown;
                ParseSegment(stream, payloadStart + actualSize);
                stream.Position = payloadStart + actualSize;
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
                stream.ReadExactly(buf, 0, buf.Length);
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

            // Skip massive media chunks outright — this is the magic that keeps reads
            // bounded: Cluster + BlockGroup are seeked past, not buffered.
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
                _tagsHeaderStart = headerStart - StartOffset;
                _tagsEndOffset = payloadStart + actualSize - StartOffset;
                _hasTagsElement = true;
                var payloadBytes = new byte[actualSize];
                stream.ReadExactly(payloadBytes, 0, payloadBytes.Length);
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

        // EBML uint payloads are at most 8 bytes by spec; floats are 4 or 8. Reuse the same
        // 8-byte stack buffer across the loop so the analyser is happy.
        Span<byte> scalarBuf = stackalloc byte[8];

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
                var n = (int)Math.Min(actualSize, scalarBuf.Length);
                stream.ReadExactly(scalarBuf[..n]);
                TimecodeScale = EbmlElement.DecodeUInt(scalarBuf[..n]);
                if (TimecodeScale == 0)
                {
                    TimecodeScale = 1_000_000;
                }

                stream.Position = payloadStart + actualSize;
            }
            else if (id == DurationId)
            {
                var n = (int)Math.Min(actualSize, scalarBuf.Length);
                stream.ReadExactly(scalarBuf[..n]);
                Duration = EbmlElement.DecodeFloat(scalarBuf[..n]);
                stream.Position = payloadStart + actualSize;
            }
            else
            {
                stream.Position = payloadStart + actualSize;
            }
        }
    }

    private void SpliceTagsStreaming(Stream destination, byte[] newTagsBytes)
    {
        var oldSegPayload = _segmentPayloadEnd - _segmentPayloadStart;
        var oldTagsSize = _tagsEndOffset - _tagsHeaderStart;
        var newSegPayload = oldSegPayload - oldTagsSize + newTagsBytes.Length;

        WriteHeadAndSegmentHeader(destination, newSegPayload);

        // Write segment payload up to the existing Tags element.
        var preTagsLen = _tagsHeaderStart - _segmentPayloadStart;
        if (preTagsLen > 0)
        {
            _source!.CopyTo(_segmentPayloadStart, preTagsLen, destination);
        }

        // Write the new Tags element (omit if empty).
        if (newTagsBytes.Length > 0)
        {
            destination.Write(newTagsBytes, 0, newTagsBytes.Length);
        }

        // Write the rest of the segment payload + anything after.
        var afterTagsLen = _source!.Length - _tagsEndOffset;
        if (afterTagsLen > 0)
        {
            _source.CopyTo(_tagsEndOffset, afterTagsLen, destination);
        }
    }

    private void AppendTagsStreaming(Stream destination, byte[] newTagsBytes)
    {
        var newSegPayload = _segmentPayloadEnd - _segmentPayloadStart + newTagsBytes.Length;

        WriteHeadAndSegmentHeader(destination, newSegPayload);

        // Write the rest of the segment payload (everything after the segment header).
        var segPayloadLen = _segmentPayloadEnd - _segmentPayloadStart;
        if (segPayloadLen > 0)
        {
            _source!.CopyTo(_segmentPayloadStart, segPayloadLen, destination);
        }

        // Append the new Tags inside the (now-larger) segment.
        destination.Write(newTagsBytes, 0, newTagsBytes.Length);

        // Then any post-segment bytes — usually none.
        var postSegmentLen = _source!.Length - _segmentPayloadEnd;
        if (postSegmentLen > 0)
        {
            _source.CopyTo(_segmentPayloadEnd, postSegmentLen, destination);
        }
    }

    private void WriteHeadAndSegmentHeader(Stream destination, long newSegPayload)
    {
        // Bytes before the Segment element (EBML header + any pre-segment elements).
        if (_segmentHeaderStart > 0)
        {
            _source!.CopyTo(0, _segmentHeaderStart, destination);
        }

        // Segment element header: id + size VINT. We preserve the VINT length so
        // any internal segment-relative offsets (Cluster / Cues references) stay valid.
        var idBytes = EbmlElement.EncodeId(SegmentId);
        var oldSegHeaderLen = (int)(_segmentPayloadStart - _segmentHeaderStart);
        var oldSegSizeVintLen = oldSegHeaderLen - idBytes.Length;

        var newSegSizeVint = _segmentSizeWasUnknown
            ? MakeUnknownSizeVint(oldSegSizeVintLen <= 0 ? 1 : oldSegSizeVintLen)
            : EbmlElement.EncodeVintSize(newSegPayload, oldSegSizeVintLen <= 0 ? 8 : oldSegSizeVintLen);

        destination.Write(idBytes, 0, idBytes.Length);
        destination.Write(newSegSizeVint, 0, newSegSizeVint.Length);
    }

    private static byte[] MakeUnknownSizeVint(int length)
    {
        if (length is < 1 or > 8)
        {
            length = 1;
        }

        var buf = new byte[length];
        var markerMask = (byte)(0x80 >> (length - 1));
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
