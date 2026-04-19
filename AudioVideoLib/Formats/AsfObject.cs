namespace AudioVideoLib.Formats;

using System;

/// <summary>
/// A single ASF (Advanced Systems Format) object discovered while walking a WMA / WMV / ASF container.
/// </summary>
/// <param name="Id">The 16-byte object GUID, in its standard ASF mixed-endian binary layout.</param>
/// <param name="StartOffset">Offset of the object header (its GUID field) from the start of the stream.</param>
/// <param name="EndOffset">Offset immediately past the object payload.</param>
/// <remarks>
/// ASF object headers are laid out as a 16-byte GUID followed by a 64-bit little-endian object size
/// that includes the header itself. The minimum legal size is therefore 24 bytes (16 + 8).
/// </remarks>
public sealed record AsfObject(Guid Id, long StartOffset, long EndOffset)
{
    /// <summary>
    /// The fixed size, in bytes, of an ASF object header (16-byte GUID + 8-byte size).
    /// </summary>
    public const int HeaderSize = 24;

    /// <summary>
    /// Gets the object's total length in bytes, including its 24-byte header.
    /// </summary>
    public long Size => EndOffset - StartOffset;

    /// <summary>
    /// Gets the offset of the object payload (the byte immediately after the header).
    /// </summary>
    public long PayloadOffset => StartOffset + HeaderSize;

    /// <summary>
    /// Gets the size of the object payload in bytes (total size minus the 24-byte header).
    /// </summary>
    public long PayloadSize => Size - HeaderSize;
}
