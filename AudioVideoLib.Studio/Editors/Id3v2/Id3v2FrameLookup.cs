namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

/// <summary>
/// ID3v2-specific lookup primitives built on top of <see cref="TagItemEditorRegistry"/>.
/// Lives outside <see cref="Id3v2AddMenuBuilder"/> so non-menu callers (frame
/// construction, validation) don't reach into a UI class for dispatch logic.
/// </summary>
public static class Id3v2FrameLookup
{
    /// <summary>
    /// Resolves an editor attribute to the identifier its frames carry at
    /// <paramref name="version"/>. Tries the most-likely ctors first; returns
    /// <c>null</c> for editors whose frames refuse construction at the given
    /// version.
    /// </summary>
    internal static string? IdentifierFor(Id3v2FrameEditorAttribute attr, Id3v2Version version)
    {
        if (!string.IsNullOrEmpty(attr.KnownIdentifier))
        {
            return attr.KnownIdentifier;
        }

        // Try the version-aware ctor first.
        var versionCtor = attr.ItemType.GetConstructor([typeof(Id3v2Version)]);
        if (versionCtor is not null)
        {
            try
            {
                return ((Id3v2Frame)versionCtor.Invoke([version])).Identifier;
            }
            catch
            {
                // Fall through to the (Id3v2Version, string) overload below — frames whose
                // sole ctor takes an extra arg (e.g. LinkedInformationFrame) reach this path.
            }
        }

        // Some frames require an additional argument alongside Id3v2Version (e.g.
        // Id3v2LinkedInformationFrame's only ctor is (Id3v2Version, string frameIdentifier);
        // the Identifier getter is version-derived and doesn't depend on the second arg).
        var versionStringCtor = attr.ItemType.GetConstructor([typeof(Id3v2Version), typeof(string)]);
        if (versionStringCtor is not null)
        {
            try
            {
                return ((Id3v2Frame)versionStringCtor.Invoke([version, string.Empty])).Identifier;
            }
            catch
            {
                // Fall through.
            }
        }

        var defaultCtor = attr.ItemType.GetConstructor(Type.EmptyTypes);
        if (defaultCtor is not null)
        {
            try
            {
                return ((Id3v2Frame)defaultCtor.Invoke([])).Identifier;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Locates the registry entry whose ID3v2 editor attribute resolves to
    /// <paramref name="identifier"/> at <paramref name="version"/>. Skips
    /// family-base editors (<see cref="Id3v2TextFrame"/>/<see cref="Id3v2UrlLinkFrame"/>) —
    /// their per-identifier identity lives in the catalogs, not the editor attribute.
    /// </summary>
    /// <remarks>
    /// Match is <see cref="StringComparison.OrdinalIgnoreCase"/> for resilience against
    /// case-mismatched callers; ID3v2 identifiers are uppercase by spec.
    /// </remarks>
    public static bool TryFindEntryByIdentifier(
        TagItemEditorRegistry registry,
        string identifier,
        Id3v2Version version,
        out TagItemEditorRegistry.RegistrationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(identifier);
        foreach (var candidate in registry.Entries)
        {
            if (candidate.Attribute is not Id3v2FrameEditorAttribute attr)
            {
                continue;
            }
            if (attr.ItemType == typeof(Id3v2TextFrame) || attr.ItemType == typeof(Id3v2UrlLinkFrame))
            {
                continue;
            }
            if (!attr.SupportedVersions.Contains(version))
            {
                continue;
            }
            if (string.Equals(IdentifierFor(attr, version), identifier, StringComparison.OrdinalIgnoreCase))
            {
                entry = candidate;
                return true;
            }
        }
        entry = default;
        return false;
    }
}
