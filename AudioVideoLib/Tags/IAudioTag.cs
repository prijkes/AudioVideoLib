namespace AudioVideoLib.Tags;

using System;
using System.IO;

/// <summary>
/// Interface which tag implementations must implement.
/// </summary>
/// <remarks>
/// The canonical serialisation primitive is <see cref="WriteTo(Stream)"/>. Buffer-shaped
/// helpers (<c>ToByteArray</c>, <c>GetSerializedSize</c>, <c>TryWriteTo</c>,
/// <c>WriteTo(IBufferWriter&lt;byte&gt;)</c>) are provided as extension methods on
/// <see cref="IAudioTagExtensions"/>; concrete types may override them with their own
/// instance method for a faster direct path.
/// </remarks>
public interface IAudioTag : IEquatable<IAudioTag>
{
    /// <summary>
    /// Writes the serialised form of this tag to the supplied <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write the tag bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    void WriteTo(Stream destination);
}
