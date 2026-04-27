namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;
using AudioVideoLib.Tags;

/// <summary>
/// Minimal structural walker for ASF (Advanced Systems Format) containers — i.e. <c>.asf</c>,
/// <c>.wma</c>, and <c>.wmv</c> files. Does not decode media payload.
/// </summary>
/// <remarks>
/// The walker validates the top-level Header Object GUID and enumerates its child objects, recording
/// each one as an <see cref="AsfObject"/>. Recognised metadata-bearing children (Content Description,
/// Extended Content Description, Metadata, Metadata Library — the last two nested inside the Header
/// Extension Object) are parsed into <see cref="MetadataTag"/>. Unrecognised objects are recorded
/// generically and skipped without failing the walk.
/// <para />
/// Only the (small) ASF Header Object is materialised in memory; the much larger Data Object and any
/// Index Object that follow are kept reachable via an <see cref="ISourceReader"/> reference and
/// streamed straight to the destination at write time. Editing metadata in a multi-GB WMV is
/// bounded by the header size, not the file size.
/// <para />
/// Callers must keep the source <see cref="Stream"/> passed to <see cref="ReadStream(Stream)"/>
/// alive until <see cref="WriteTo(Stream)"/> / <c>ToByteArray</c> finishes.
/// </remarks>
public sealed class AsfStream : IMediaContainer, IDisposable
{
    private const int MaxObjectsToRecord = 4096;

    private readonly List<AsfObject> _objects = [];

    // The Header Object is fully materialised (it's the only thing the splice writer mutates,
    // and it's typically tiny — KB to a few MB).
    private byte[] _headerBytes = [];

    // The full source is kept for streaming the post-header bytes (Data Object + Index Object).
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration { get; private set; }

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>
    /// Gets every ASF object encountered by the walker, in file order. The first entry is always the
    /// Header Object itself; the remainder are its direct children. Nested children of the Header
    /// Extension Object (Metadata, Metadata Library) are not exposed here.
    /// </summary>
    public IReadOnlyList<AsfObject> Objects => _objects;

    /// <summary>
    /// Gets the parsed ASF metadata tag — the union of the Content Description, Extended Content
    /// Description, Metadata, and Metadata Library objects discovered inside the Header Object.
    /// </summary>
    /// <remarks>
    /// Setter accepts <c>null</c> as shorthand for "clear all metadata"; the property
    /// is reset to a fresh empty <see cref="AsfMetadataTag"/> in that case.
    /// </remarks>
    public AsfMetadataTag MetadataTag
    {
        get;
        set => field = value ?? new AsfMetadataTag();
    } = new();

    /// <summary>
    /// Gets the absolute offset of the Header Object payload (the byte immediately after its 30-byte
    /// header + 6-byte child-count + reserved fields). 0 if the file was not parsed.
    /// </summary>
    public long HeaderObjectChildrenOffset { get; private set; }

    /// <summary>
    /// Gets the size of the Header Object as recorded in its on-disk size field, or 0.
    /// </summary>
    public long HeaderObjectSize { get; private set; }

    /// <summary>
    /// Gets the file's Play Duration as reported by the File Properties Object, in 100-nanosecond
    /// units. 0 if the File Properties Object is absent.
    /// </summary>
    public ulong PlayDuration100Ns { get; private set; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;
        if (stream.Length - start < AsfObject.HeaderSize)
        {
            return false;
        }

        // Peek the 24-byte Header Object preamble (GUID + size).
        Span<byte> peek = stackalloc byte[AsfObject.HeaderSize];
        stream.ReadExactly(peek);
        stream.Position = start;

        var headerGuid = new Guid(peek[..16]);
        if (headerGuid != AsfMetadataTag.HeaderObjectGuid)
        {
            return false;
        }

        var headerSize = BinaryPrimitives.ReadUInt64LittleEndian(peek[16..]);
        if (headerSize < (ulong)AsfObject.HeaderSize || (long)headerSize > stream.Length - start)
        {
            return false;
        }

        StartOffset = start;
        // Capture the source while the stream is still positioned at `start`, so reads via
        // _source.Read(0, …) hit the beginning of the container regardless of subsequent
        // seeks done by the walker.
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        // Materialise the Header Object only — typically KB-MB range. The rest of the file
        // (Data Object + optional Index Object — usually GB-scale) is left on disk.
        _headerBytes = new byte[(int)headerSize];
        _source.Read(0, _headerBytes);

        HeaderObjectSize = (long)headerSize;
        HeaderObjectChildrenOffset = start + 30;
        _objects.Add(new AsfObject(AsfMetadataTag.HeaderObjectGuid, start, start + (long)headerSize));

        if (headerSize < 30)
        {
            stream.Position = EndOffset;
            return false;
        }

        WalkChildren(_headerBytes, 30, (long)headerSize, recordTopLevel: true);

        stream.Position = EndOffset;
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Streams the rebuilt Header Object directly to the destination, then streams the unchanged
    /// Data + Index regions from the source via <see cref="ISourceReader"/>. Peak memory is bounded
    /// by the Header Object size, not the file size.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null || _headerBytes.Length == 0 || HeaderObjectSize == 0)
        {
            return;
        }

        var headerEnd = (int)HeaderObjectSize;
        if (headerEnd > _headerBytes.Length || headerEnd < 30)
        {
            return;
        }

        // Walk children inside the captured Header Object and emit each one to a new buffer except
        // those whose GUIDs we replace. We also count the surviving children and the new ones.
        using var newChildren = new MemoryStream();
        var surviving = 0;
        var pos = 30;
        while (pos + AsfObject.HeaderSize <= headerEnd)
        {
            var guid = new Guid(_headerBytes.AsSpan(pos, 16).ToArray());
            var size = ReadU64(_headerBytes, pos + 16);
            if (size < (ulong)AsfObject.HeaderSize || pos + (long)size > headerEnd)
            {
                break;
            }

            if (!IsReplaceableGuid(guid))
            {
                newChildren.Write(_headerBytes, pos, (int)size);
                surviving++;
            }

            pos += (int)size;
        }

        var newObjects = MetadataTag.ToByteArrays();
        foreach (var obj in newObjects)
        {
            newChildren.Write(obj, 0, obj.Length);
        }

        var newChildBytes = newChildren.ToArray();
        var newHeaderTotal = 30L + newChildBytes.Length;
        var newChildCount = surviving + newObjects.Length;

        // Write the rebuilt Header Object preamble (GUID + size + count + reserved).
        Span<byte> preamble = stackalloc byte[30];
        _headerBytes.AsSpan(0, 16).CopyTo(preamble);
        BinaryPrimitives.WriteUInt64LittleEndian(preamble[16..], (ulong)newHeaderTotal);
        BinaryPrimitives.WriteUInt32LittleEndian(preamble[24..], (uint)newChildCount);
        // 2-byte reserved field at offset 28 — preserve original bytes (typically 0x01 0x02).
        preamble[28] = _headerBytes[28];
        preamble[29] = _headerBytes[29];
        destination.Write(preamble);

        // New children.
        destination.Write(newChildBytes, 0, newChildBytes.Length);

        // Stream the rest of the file (Data Object + Index Object etc) directly from source —
        // this is the multi-GB region we deliberately never load into memory.
        var afterHeader = _source.Length - HeaderObjectSize;
        if (afterHeader > 0)
        {
            _source.CopyTo(HeaderObjectSize, afterHeader, destination);
        }
    }

    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
    /// <see cref="Stream"/>; the caller still owns that.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }

    private static bool IsReplaceableGuid(Guid g) =>
        g == AsfMetadataTag.ContentDescriptionObjectGuid
        || g == AsfMetadataTag.ExtendedContentDescriptionObjectGuid;

    private void WalkChildren(byte[] data, int from, long limit, bool recordTopLevel)
    {
        var pos = from;
        while (pos + AsfObject.HeaderSize <= limit && _objects.Count < MaxObjectsToRecord)
        {
            var guid = new Guid(data.AsSpan(pos, 16).ToArray());
            var size = ReadU64(data, pos + 16);
            if (size < (ulong)AsfObject.HeaderSize || (long)size > limit - pos)
            {
                return;
            }

            var objStart = StartOffset + pos;
            var objEnd = objStart + (long)size;
            if (recordTopLevel)
            {
                _objects.Add(new AsfObject(guid, objStart, objEnd));
            }

            var payloadStart = pos + AsfObject.HeaderSize;
            var payloadLen = (int)((long)size - AsfObject.HeaderSize);

            if (guid == AsfMetadataTag.ContentDescriptionObjectGuid)
            {
                MetadataTag.ParseContentDescription(data.AsSpan(payloadStart, payloadLen));
            }
            else if (guid == AsfMetadataTag.ExtendedContentDescriptionObjectGuid)
            {
                MetadataTag.ParseExtendedContentDescription(data.AsSpan(payloadStart, payloadLen));
            }
            else if (guid == AsfMetadataTag.FilePropertiesObjectGuid)
            {
                ParseFileProperties(data, payloadStart, payloadLen);
            }
            else if (guid == AsfMetadataTag.HeaderExtensionObjectGuid)
            {
                ParseHeaderExtension(data, payloadStart, payloadLen);
            }

            pos += (int)size;
        }
    }

    private void ParseFileProperties(byte[] data, int offset, int length)
    {
        // Play Duration sits at offset 16 + 8 + 8 + 8 = 40 inside the payload.
        const int PlayDurationOffset = 40;
        if (length < PlayDurationOffset + 8)
        {
            return;
        }

        PlayDuration100Ns = ReadU64(data, offset + PlayDurationOffset);
        TotalDuration = (long)(PlayDuration100Ns / 10000UL);
    }

    private void ParseHeaderExtension(byte[] data, int offset, int length)
    {
        const int ExtensionDataHeaderSize = 16 + 2 + 4;
        if (length < ExtensionDataHeaderSize)
        {
            return;
        }

        var dataSize = ReadU32(data, offset + 18);
        if (dataSize == 0 || dataSize > (uint)(length - ExtensionDataHeaderSize))
        {
            return;
        }

        var nestedFrom = offset + ExtensionDataHeaderSize;
        var nestedLimit = nestedFrom + (int)dataSize;
        var pos = nestedFrom;
        while (pos + AsfObject.HeaderSize <= nestedLimit && _objects.Count < MaxObjectsToRecord)
        {
            var guid = new Guid(data.AsSpan(pos, 16).ToArray());
            var size = ReadU64(data, pos + 16);
            if (size < (ulong)AsfObject.HeaderSize || (long)size > nestedLimit - pos)
            {
                return;
            }

            var payloadStart = pos + AsfObject.HeaderSize;
            var payloadLen = (int)((long)size - AsfObject.HeaderSize);
            if (guid == AsfMetadataTag.MetadataObjectGuid)
            {
                MetadataTag.ParseMetadata(data.AsSpan(payloadStart, payloadLen), library: false);
            }
            else if (guid == AsfMetadataTag.MetadataLibraryObjectGuid)
            {
                MetadataTag.ParseMetadata(data.AsSpan(payloadStart, payloadLen), library: true);
            }

            pos += (int)size;
        }
    }

    private static ulong ReadU64(byte[] b, int off) =>
        BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(off, 8));

    private static uint ReadU32(byte[] b, int off) =>
        BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(off, 4));
}
