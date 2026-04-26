namespace AudioVideoLib.Tags;

/// <summary>
/// Categorises a tag-parse failure so callers can dispatch on a stable enum value
/// instead of inspecting the raw <see cref="System.Exception"/> message.
/// </summary>
public enum AudioTagParseErrorKind
{
    /// <summary>Failure type couldn't be classified — fall back to the raw exception.</summary>
    Unknown = 0,

    /// <summary>The data was syntactically invalid (bad magic, impossible length, etc.).</summary>
    MalformedData = 1,

    /// <summary>The reader hit the end of the stream before the tag was complete.</summary>
    Truncated = 2,

    /// <summary>The tag declared a version the library does not support.</summary>
    UnsupportedVersion = 3,

    /// <summary>An argument-validation check failed inside the reader.</summary>
    InvalidArgument = 4,
}
