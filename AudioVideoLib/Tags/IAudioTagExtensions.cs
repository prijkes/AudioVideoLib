namespace AudioVideoLib.Tags;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// Buffer-shaped convenience helpers around <see cref="IAudioTag.WriteTo(Stream)"/>.
/// </summary>
/// <remarks>
/// Concrete implementers may shadow these with their own instance methods for a faster
/// direct-buffer path. When they do, the instance method wins for concrete-typed call sites.
/// </remarks>
public static class IAudioTagExtensions
{
    /// <summary>
    /// Places the <paramref name="tag"/> into a fresh byte array.
    /// </summary>
    /// <remarks>
    /// Buffers <see cref="IAudioTag.WriteTo(Stream)"/> into a <see cref="MemoryStream"/>.
    /// Useful when callers need bytes in hand (signing, hashing, embedding into another payload).
    /// </remarks>
    public static byte[] ToByteArray(this IAudioTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        using var ms = new MemoryStream();
        tag.WriteTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Gets the size, in bytes, of the serialised form of <paramref name="tag"/>.
    /// </summary>
    public static int GetSerializedSize(this IAudioTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        using var ms = new MemoryStream();
        tag.WriteTo(ms);
        return (int)ms.Length;
    }

    /// <summary>
    /// Writes the serialised form of <paramref name="tag"/> to the supplied buffer writer.
    /// </summary>
    public static void WriteTo(this IAudioTag tag, IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(writer);
        using var ms = new MemoryStream();
        tag.WriteTo(ms);
        writer.Write(ms.GetBuffer().AsSpan(0, (int)ms.Length));
    }

    /// <summary>
    /// Attempts to write the serialised form of <paramref name="tag"/> into a pre-sized buffer.
    /// </summary>
    public static bool TryWriteTo(this IAudioTag tag, Span<byte> destination, out int written)
    {
        ArgumentNullException.ThrowIfNull(tag);
        using var ms = new MemoryStream();
        tag.WriteTo(ms);
        var len = (int)ms.Length;
        if (len > destination.Length)
        {
            written = 0;
            return false;
        }

        ms.GetBuffer().AsSpan(0, len).CopyTo(destination);
        written = len;
        return true;
    }
}
