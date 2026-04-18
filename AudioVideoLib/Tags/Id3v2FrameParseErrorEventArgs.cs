namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Event data raised when <see cref="Id3v2TagReader"/> encounters a frame that cannot be parsed.
/// The reader skips the frame and continues — the error is surfaced via this event so callers
/// can decide whether to log it, display it, or fail.
/// </summary>
/// <param name="identifier">The frame identifier, if it was read before the failure; otherwise <c>null</c>.</param>
/// <param name="startOffset">The stream offset at which the failing frame began.</param>
/// <param name="version">The tag version being parsed.</param>
/// <param name="exception">The exception thrown during frame parsing.</param>
public sealed class Id3v2FrameParseErrorEventArgs(string? identifier, long startOffset, Id3v2Version version, Exception exception) : EventArgs
{
    /// <summary>Gets the frame identifier, if known.</summary>
    public string? Identifier { get; } = identifier;

    /// <summary>Gets the stream offset at which the failing frame began.</summary>
    public long StartOffset { get; } = startOffset;

    /// <summary>Gets the tag version being parsed.</summary>
    public Id3v2Version Version { get; } = version;

    /// <summary>Gets the exception thrown during frame parsing.</summary>
    public Exception Exception { get; } = exception;
}
