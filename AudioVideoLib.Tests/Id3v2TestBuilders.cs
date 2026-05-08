namespace AudioVideoLib.Tests;

using AudioVideoLib.Tags;

/// <summary>
/// Shared helpers for constructing ID3v2 tag and frame fixtures across test classes.
/// </summary>
internal static class Id3v2TestBuilders
{
    /// <summary>
    /// Creates a single-value <see cref="Id3v2TextFrame"/> at <paramref name="version"/>
    /// with <paramref name="identifier"/> and <paramref name="value"/>.
    /// </summary>
    public static Id3v2TextFrame MakeTextFrame(
        Id3v2Version version,
        string identifier,
        string value,
        Id3v2FrameEncodingType encoding = Id3v2FrameEncodingType.Default)
    {
        var frame = new Id3v2TextFrame(version, identifier) { TextEncoding = encoding };
        frame.Values.Add(value);
        return frame;
    }

    /// <summary>
    /// Builds a serialized <see cref="Id3v2Tag"/> containing a single text frame.
    /// </summary>
    public static byte[] BuildSimpleTagBytes(
        Id3v2Version version,
        string identifier,
        string value,
        Id3v2FrameEncodingType encoding = Id3v2FrameEncodingType.Default)
    {
        var tag = new Id3v2Tag(version);
        tag.SetFrame(MakeTextFrame(version, identifier, value, encoding));
        return tag.ToByteArray();
    }
}
