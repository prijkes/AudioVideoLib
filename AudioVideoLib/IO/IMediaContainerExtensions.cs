namespace AudioVideoLib.IO;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// Buffer-shaped convenience helpers around <see cref="IMediaContainer.WriteTo(Stream)"/>.
/// </summary>
/// <remarks>
/// Concrete implementers may shadow these with their own instance methods for a faster
/// direct-buffer path. When they do, the instance method wins for concrete-typed call sites.
/// </remarks>
public static class IMediaContainerExtensions
{
    /// <summary>
    /// Places the <paramref name="container"/> into a fresh byte array.
    /// </summary>
    public static byte[] ToByteArray(this IMediaContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        using var ms = new MemoryStream();
        container.WriteTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Gets the size, in bytes, of the serialised form of <paramref name="container"/>.
    /// </summary>
    public static int GetSerializedSize(this IMediaContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        using var ms = new MemoryStream();
        container.WriteTo(ms);
        return (int)ms.Length;
    }

    /// <summary>
    /// Writes the serialised form of <paramref name="container"/> to the supplied buffer writer.
    /// </summary>
    public static void WriteTo(this IMediaContainer container, IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(writer);
        using var ms = new MemoryStream();
        container.WriteTo(ms);
        writer.Write(ms.GetBuffer().AsSpan(0, (int)ms.Length));
    }

    /// <summary>
    /// Attempts to write the serialised form of <paramref name="container"/> into a pre-sized buffer.
    /// </summary>
    public static bool TryWriteTo(this IMediaContainer container, Span<byte> destination, out int written)
    {
        ArgumentNullException.ThrowIfNull(container);
        using var ms = new MemoryStream();
        container.WriteTo(ms);
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
