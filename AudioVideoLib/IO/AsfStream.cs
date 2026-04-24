namespace AudioVideoLib.IO;

using System;
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
/// </remarks>
public sealed class AsfStream : IMediaContainer
{
    private const int MaxObjectsToRecord = 4096;

    private readonly List<AsfObject> _objects = [];
    private byte[] _originalBytes = [];

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
    /// Always non-<c>null</c> after a successful <see cref="ReadStream"/>; check the individual
    /// collections (<see cref="AsfMetadataTag.HasContentDescription"/>, <c>ExtendedItems.Count</c>, …)
    /// for emptiness.
    /// </summary>
    public AsfMetadataTag MetadataTag { get; private set; } = new();

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

        // Capture the entire stream up-front so we can splice it back together in ToByteArray.
        var totalLen = stream.Length - start;
        _originalBytes = new byte[totalLen];
        var read = 0;
        while (read < totalLen)
        {
            var n = stream.Read(_originalBytes, read, (int)(totalLen - read));
            if (n <= 0)
            {
                break;
            }

            read += n;
        }

        if (read < AsfObject.HeaderSize)
        {
            return false;
        }

        // Validate and record the top-level Header Object.
        var headerGuid = new Guid(_originalBytes.AsSpan(0, 16).ToArray());
        if (headerGuid != AsfMetadataTag.HeaderObjectGuid)
        {
            return false;
        }

        var headerSize = ReadU64(_originalBytes, 16);
        if (headerSize < (ulong)AsfObject.HeaderSize || headerSize > (ulong)read)
        {
            return false;
        }

        StartOffset = start;
        EndOffset = start + read;
        HeaderObjectSize = (long)headerSize;
        _objects.Add(new AsfObject(AsfMetadataTag.HeaderObjectGuid, start, start + (long)headerSize));

        // Header Object body shape: GUID(16) + size(8) + numChildren(4) + reserved(2) = 30 bytes.
        // Children begin at offset 30.
        if (headerSize < 30)
        {
            return false;
        }

        HeaderObjectChildrenOffset = start + 30;
        WalkChildren(_originalBytes, 30, (long)headerSize, recordTopLevel: true);

        return true;
    }

    /// <summary>
    /// Rebuilds the ASF file by splicing the current <see cref="MetadataTag"/> back into the original
    /// Header Object: existing CDO / ECDO / MO / MLO objects are removed and replacements emitted by
    /// <see cref="AsfMetadataTag.ToByteArrays"/> are inserted near the end of the Header Object's
    /// child block. The Header Object size and child-count fields are recomputed.
    /// </summary>
    /// <returns>The rewritten file bytes, or an empty array if no source bytes were captured.</returns>
    public byte[] ToByteArray()
    {
        if (_originalBytes.Length == 0 || HeaderObjectSize == 0)
        {
            return [];
        }

        var headerEnd = (int)HeaderObjectSize;
        if (headerEnd > _originalBytes.Length || headerEnd < 30)
        {
            return [];
        }

        // Walk children inside the original Header Object and emit each one to a new buffer except
        // those whose GUIDs we replace. We also count the surviving children and the new ones.
        using var newChildren = new MemoryStream();
        var surviving = 0;
        var pos = 30;
        while (pos + AsfObject.HeaderSize <= headerEnd)
        {
            var guid = new Guid(_originalBytes.AsSpan(pos, 16).ToArray());
            var size = ReadU64(_originalBytes, pos + 16);
            if (size < (ulong)AsfObject.HeaderSize || pos + (long)size > headerEnd)
            {
                break;
            }

            if (!IsReplaceableGuid(guid))
            {
                newChildren.Write(_originalBytes, pos, (int)size);
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

        var afterHeader = _originalBytes.Length - headerEnd;
        var output = new byte[newHeaderTotal + afterHeader];

        // Copy existing Header Object GUID; overwrite size + child count + reserved bytes.
        Buffer.BlockCopy(_originalBytes, 0, output, 0, 16);
        var sizeBytes = AsfMetadataTag.U64Le((ulong)newHeaderTotal);
        Buffer.BlockCopy(sizeBytes, 0, output, 16, 8);
        var countBytes = AsfMetadataTag.U32Le((uint)newChildCount);
        Buffer.BlockCopy(countBytes, 0, output, 24, 4);
        // 2-byte reserved field at offset 28 — preserve original bytes (typically 0x01 0x02).
        output[28] = _originalBytes[28];
        output[29] = _originalBytes[29];

        Buffer.BlockCopy(newChildBytes, 0, output, 30, newChildBytes.Length);
        if (afterHeader > 0)
        {
            Buffer.BlockCopy(_originalBytes, headerEnd, output, (int)newHeaderTotal, afterHeader);
        }

        return output;
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
                MetadataTag.ParseContentDescription(SliceCopy(data, payloadStart, payloadLen));
            }
            else if (guid == AsfMetadataTag.ExtendedContentDescriptionObjectGuid)
            {
                MetadataTag.ParseExtendedContentDescription(SliceCopy(data, payloadStart, payloadLen));
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
        // File Properties Object payload (after the 24-byte object header) layout per spec:
        //   File ID                 (16 bytes)
        //   File Size               (QWORD, 8)
        //   Creation Date           (QWORD, 8)
        //   Data Packets Count      (QWORD, 8)
        //   Play Duration           (QWORD, 8)  <- this is what we want
        //   Send Duration           (QWORD, 8)
        //   Preroll                 (QWORD, 8)
        //   Flags                   (DWORD, 4)
        //   Min Data Packet Size    (DWORD, 4)
        //   Max Data Packet Size    (DWORD, 4)
        //   Max Bitrate             (DWORD, 4)
        // Play Duration sits at offset 16 + 8 + 8 + 8 = 40.
        const int PlayDurationOffset = 40;
        if (length < PlayDurationOffset + 8)
        {
            return;
        }

        PlayDuration100Ns = ReadU64(data, offset + PlayDurationOffset);
        // Convert 100-ns units to milliseconds.
        TotalDuration = (long)(PlayDuration100Ns / 10000UL);
    }

    private void ParseHeaderExtension(byte[] data, int offset, int length)
    {
        // Header Extension Object payload (after 24-byte header):
        //   Reserved Field 1   (GUID, 16 bytes)
        //   Reserved Field 2   (WORD, 2 bytes)
        //   Header Extension Data Size (DWORD, 4 bytes)
        //   Header Extension Data      (variable)
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
                MetadataTag.ParseMetadata(SliceCopy(data, payloadStart, payloadLen), library: false);
            }
            else if (guid == AsfMetadataTag.MetadataLibraryObjectGuid)
            {
                MetadataTag.ParseMetadata(SliceCopy(data, payloadStart, payloadLen), library: true);
            }

            pos += (int)size;
        }
    }

    private static byte[] SliceCopy(byte[] src, int offset, int length)
    {
        if (length <= 0)
        {
            return [];
        }

        var dst = new byte[length];
        Buffer.BlockCopy(src, offset, dst, 0, length);
        return dst;
    }

    private static ulong ReadU64(byte[] b, int off)
    {
        ulong v = 0;
        for (var i = 7; i >= 0; i--)
        {
            v = (v << 8) | b[off + i];
        }

        return v;
    }

    private static uint ReadU32(byte[] b, int off) =>
        (uint)(b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24));
}
