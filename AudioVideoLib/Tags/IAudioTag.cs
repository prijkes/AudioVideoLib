namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Interface which tag implementations must implement.
/// </summary>
public interface IAudioTag : IEquatable<IAudioTag>
{
    /// <summary>
    /// Places the <see cref="IAudioTag"/> into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the <see cref="IAudioTag"/>.
    /// </returns>
    byte[] ToByteArray();
}
