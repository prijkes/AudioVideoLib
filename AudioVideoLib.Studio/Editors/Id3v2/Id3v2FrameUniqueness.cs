namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;

/// <summary>
/// Single source of truth for "may this frame identifier appear more than once
/// in a tag?" — used by Manage Frames, the Frame top-bar menu, and the right-click
/// context menu so they agree on Edit-vs-Add.
/// </summary>
/// <remarks>
/// The rules combine ID3v2 §4.2 (text frames are unique per identifier),
/// §4.3 (URL frames are unique per identifier), and §4.3.1 (WOAR / WAR may
/// appear once per performer). Non-family frames (APIC, COMM, ASPI, …) declare
/// their own uniqueness via the IsUniqueInstance flag on Id3v2FrameEditorAttribute;
/// callers should check that attribute first and only consult this helper for the
/// per-identifier rule.
/// </remarks>
public static class Id3v2FrameUniqueness
{
    /// <summary>
    /// True when an identifier in the text or URL family must appear at most once
    /// per tag. False for unknown identifiers — for those, fall back to the
    /// editor attribute's IsUniqueInstance flag.
    /// </summary>
    public static bool IsUniqueTextOrUrlIdentifier(string identifier)
    {
        foreach (var t in Id3v2KnownTextFrameIds.All)
        {
            if (Matches(identifier, t.Identifier, t.V220Identifier))
            {
                return true;
            }
        }
        foreach (var u in Id3v2KnownUrlFrameIds.All)
        {
            if (Matches(identifier, u.Identifier, u.V220Identifier))
            {
                return !u.AllowMultiple;
            }
        }
        return false;
    }

    private static bool Matches(string identifier, string primary, string? v220)
        => string.Equals(identifier, primary, StringComparison.Ordinal)
        || (v220 is not null && string.Equals(identifier, v220, StringComparison.Ordinal));
}
