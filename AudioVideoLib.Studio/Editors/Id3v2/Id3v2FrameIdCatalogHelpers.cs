namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Collections.Generic;

using AudioVideoLib.Tags;

/// <summary>
/// Shared plumbing for the <see cref="Id3v2KnownTextFrameIds"/> and
/// <see cref="Id3v2KnownUrlFrameIds"/> catalogs: version-mask folding and
/// case-insensitive primary/v2.2-alternate lookup.
/// </summary>
internal static class Id3v2FrameIdCatalogHelpers
{
    public static Id3v2VersionMask ToMask(IReadOnlyList<Id3v2Version> versions)
    {
        var mask = Id3v2VersionMask.None;
        foreach (var v in versions)
        {
            mask |= v.ToMask();
        }
        return mask;
    }

    public static bool TryFind<TEntry>(
        IReadOnlyList<TEntry> all,
        string identifier,
        Func<TEntry, string> primary,
        Func<TEntry, string?> alternate,
        out TEntry entry)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        foreach (var e in all)
        {
            if (string.Equals(primary(e), identifier, StringComparison.OrdinalIgnoreCase)
                || (alternate(e) is { } alt
                    && string.Equals(alt, identifier, StringComparison.OrdinalIgnoreCase)))
            {
                entry = e;
                return true;
            }
        }
        entry = default!;
        return false;
    }
}
