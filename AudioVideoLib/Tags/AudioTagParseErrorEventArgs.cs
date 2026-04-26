namespace AudioVideoLib.Tags;

using System;
using System.IO;

/// <summary>
/// Event data raised when <see cref="AudioTags"/> encounters a tag reader that throws while parsing.
/// The failing tag is skipped and the next reader is tried — the error is surfaced via this event
/// so callers can decide whether to log it, display it, or fail.
/// </summary>
/// <param name="reader">The reader that threw while parsing.</param>
/// <param name="tagOrigin">The origin (start or end of stream) being scanned.</param>
/// <param name="startOffset">The stream offset at which the failing tag began.</param>
/// <param name="exception">The exception thrown during tag parsing.</param>
public sealed class AudioTagParseErrorEventArgs(IAudioTagReader reader, TagOrigin tagOrigin, long startOffset, Exception exception) : EventArgs
{
    /// <summary>Gets the reader that threw while parsing.</summary>
    public IAudioTagReader Reader { get; } = reader;

    /// <summary>Gets the origin (start or end of stream) being scanned.</summary>
    public TagOrigin TagOrigin { get; } = tagOrigin;

    /// <summary>Gets the stream offset at which the failing tag began.</summary>
    public long StartOffset { get; } = startOffset;

    /// <summary>Gets the exception thrown during tag parsing.</summary>
    public Exception Exception { get; } = exception;

    /// <summary>
    /// Gets a structured classification of <see cref="Exception"/> so callers can dispatch
    /// without parsing the message text.
    /// </summary>
    public AudioTagParseErrorKind Kind { get; } = ClassifyError(exception);

    private static AudioTagParseErrorKind ClassifyError(Exception ex) => ex switch
    {
        EndOfStreamException => AudioTagParseErrorKind.Truncated,
        InvalidVersionException => AudioTagParseErrorKind.UnsupportedVersion,
        InvalidDataException => AudioTagParseErrorKind.MalformedData,
        ArgumentException => AudioTagParseErrorKind.InvalidArgument,
        _ => AudioTagParseErrorKind.Unknown,
    };
}
