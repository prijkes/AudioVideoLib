namespace AudioVideoLib.Studio.Editors.Id3v2;

using AudioVideoLib.Tags;

/// <summary>
/// Single-line summaries for ID3v2 frames, shared by the inspector tree and the
/// advanced-frames grid so both surfaces show the same description for the same
/// frame.
/// </summary>
public static class Id3v2FrameSummary
{
    /// <summary>
    /// Returns a single-line summary of <paramref name="frame"/> for display in lists.
    /// </summary>
    /// <param name="frame">The frame, or <c>null</c> for unparsed/unknown data.</param>
    /// <param name="fallbackByteSize">
    /// Byte count to render in the fallback <c>&lt;N bytes&gt;</c> form when the frame
    /// is <c>null</c> or its runtime type has no specialised describer. Pass the
    /// frame's <c>Data.Length</c> for already-parsed frames; the inspector tree
    /// passes the on-disk frame size which may differ.
    /// </param>
    public static string Describe(Id3v2Frame? frame, long fallbackByteSize)
    {
        return frame switch
        {
            null => $"<{fallbackByteSize:N0} bytes>",
            Id3v2TextFrame text => string.Join(" / ", text.Values),
            Id3v2UserDefinedTextInformationFrame u => u.Value ?? string.Empty,
            Id3v2UrlLinkFrame url => url.Url ?? string.Empty,
            Id3v2UserDefinedUrlLinkFrame uurl => uurl.Url ?? string.Empty,
            Id3v2CommentFrame comm => comm.Text ?? string.Empty,
            Id3v2UnsynchronizedLyricsFrame u => $"[{u.Language}:{u.ContentDescriptor}] {u.Lyrics}",
            Id3v2AttachedPictureFrame p => $"{p.ImageFormat} {p.PictureType} {p.PictureData?.Length ?? 0:N0} bytes",
            Id3v2PrivateFrame p => $"[{p.OwnerIdentifier}] {p.PrivateData?.Length ?? 0:N0} bytes",
            Id3v2UniqueFileIdentifierFrame u => $"[{u.OwnerIdentifier}] {u.IdentifierData?.Length ?? 0:N0} bytes",
            _ => $"<{fallbackByteSize:N0} bytes>",
        };
    }
}
