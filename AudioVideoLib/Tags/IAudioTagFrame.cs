namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Interface for implementing an tag frame.
/// </summary>
public interface IAudioTagFrame : IEquatable<IAudioTagFrame>
{
    /// <summary>
    /// Gets the data and only the data of this frame.
    /// </summary>
    /// <value>
    /// The data of this frame as a byte array.
    /// </value>
    /// <remarks>
    /// The data should not include any header(s) and/or footer(s).
    /// The header(s) and/or footer(s) should be included when calling <see cref="ToByteArray()"/>, together with the data.
    /// </remarks>
    byte[]? Data { get; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Writes the whole frame into a byte array.
    /// </summary>
    /// <returns>A byte array that represents the frame.</returns>
    byte[] ToByteArray();
}
