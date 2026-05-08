namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public static class Id3v2AddMenuBuilder
{
    public static string BuildEntryLabel(Id3v2FrameEditorAttribute attribute, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(tag);

        var ident = Id3v2FrameLookup.IdentifierFor(attribute, tag.Version) ?? "?";
        var name = StripTrailingIdentifier(attribute.MenuLabel, ident);
        var existing = attribute.IsUniqueInstance
            && tag.Frames.Any(f => f.GetType() == attribute.ItemType);
        var verb = existing ? "Edit" : "Add";
        return string.IsNullOrEmpty(name)
            ? $"{verb} {ident}…"
            : $"{verb} {ident} — {name}…";
    }

    /// <summary>
    /// Editor MenuLabel attributes commonly include a trailing parenthetical of the
    /// frame's identifier (e.g. "Comment (COMM)"). The unified menu-label format renders
    /// the identifier separately, so strip that parenthetical to avoid duplication.
    /// </summary>
    private static string StripTrailingIdentifier(string menuLabel, string identifier)
    {
        if (string.IsNullOrEmpty(menuLabel))
        {
            return string.Empty;
        }
        var suffix = $" ({identifier})";
        return menuLabel.EndsWith(suffix, StringComparison.Ordinal)
            ? menuLabel[..^suffix.Length]
            : menuLabel;
    }

    public static Id3v2MenuModel BuildModel(TagItemEditorRegistry registry, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tag);

        var version = tag.Version;
        var versionMask = version.ToMask();
        var categories = new List<Id3v2MenuCategory>();

        foreach (var category in CategoriesInDisplayOrder())
        {
            var entries = category switch
            {
                Id3v2FrameCategory.TextFrames => BuildTextFamilyEntries(versionMask, tag),
                Id3v2FrameCategory.UrlFrames => BuildUrlFamilyEntries(versionMask, tag),
                _ => BuildRegistryEntries(registry, category, version, tag),
            };

            if (entries.Count > 0)
            {
                categories.Add(new Id3v2MenuCategory(category, category.ToDisplay(), entries));
            }
        }

        return new Id3v2MenuModel(categories);
    }

    public static void Populate(
        ContextMenu menu,
        Id3v2MenuModel model,
        Action<Id3v2MenuEntry> onClick)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(onClick);

        menu.Items.Clear();
        foreach (var cat in model.Categories)
        {
            var sub = new MenuItem { Header = cat.Header };
            foreach (var entry in cat.Entries)
            {
                var mi = new MenuItem { Header = entry.Label, Tag = entry };
                mi.Click += (_, _) => onClick(entry);
                sub.Items.Add(mi);
            }
            menu.Items.Add(sub);
        }
    }

    private static IEnumerable<Id3v2FrameCategory> CategoriesInDisplayOrder() =>
    [
        Id3v2FrameCategory.TextFrames,
        Id3v2FrameCategory.UrlFrames,
        Id3v2FrameCategory.Identification,
        Id3v2FrameCategory.CommentsAndLyrics,
        Id3v2FrameCategory.TimingAndSync,
        Id3v2FrameCategory.People,
        Id3v2FrameCategory.AudioAdjustment,
        Id3v2FrameCategory.CountersAndRatings,
        Id3v2FrameCategory.Attachments,
        Id3v2FrameCategory.CommerceAndRights,
        Id3v2FrameCategory.EncryptionAndCompression,
        Id3v2FrameCategory.Containers,
        Id3v2FrameCategory.System,
        Id3v2FrameCategory.Experimental,
    ];

    private static IReadOnlyList<Id3v2MenuEntry> BuildTextFamilyEntries(Id3v2VersionMask versionMask, Id3v2Tag tag)
    {
        return [.. Id3v2KnownTextFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Select(i => BuildFamilyEntry(
                Id3v2KnownTextFrameIds.IdentifierFor(i, versionMask),
                i.FriendlyName,
                tag,
                f => f is Id3v2TextFrame))];
    }

    private static IReadOnlyList<Id3v2MenuEntry> BuildUrlFamilyEntries(Id3v2VersionMask versionMask, Id3v2Tag tag)
    {
        return [.. Id3v2KnownUrlFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Select(i => BuildFamilyEntry(
                Id3v2KnownUrlFrameIds.IdentifierFor(i, versionMask),
                i.FriendlyName,
                tag,
                f => f is Id3v2UrlLinkFrame))];
    }

    private static Id3v2MenuEntry BuildFamilyEntry(
        string identifier, string friendlyName, Id3v2Tag tag, Func<Id3v2Frame, bool> isFamilyMember)
    {
        // Text/URL family rules live in Id3v2FrameUniqueness so the Frame menu,
        // Manage Frames, and the right-click context menu all stay in sync.
        var existing = Id3v2FrameUniqueness.IsUniqueTextOrUrlIdentifier(identifier)
            && tag.Frames.Any(f => isFamilyMember(f) && string.Equals(f.Identifier, identifier, StringComparison.Ordinal));
        var verb = existing ? "Edit" : "Add";
        return new Id3v2MenuEntry($"{verb} {identifier} — {friendlyName}…", identifier, IsEditExisting: existing);
    }

    private static IReadOnlyList<Id3v2MenuEntry> BuildRegistryEntries(
        TagItemEditorRegistry registry,
        Id3v2FrameCategory category,
        Id3v2Version version,
        Id3v2Tag tag)
    {
        return [.. registry.Entries
            .Where(e => e.Attribute is Id3v2FrameEditorAttribute a
                        && a.Category == category
                        && a.SupportedVersions.Contains(version))
            .OrderBy(e => e.Attribute.Order)
            .Select(e =>
            {
                var attr = (Id3v2FrameEditorAttribute)e.Attribute;
                var label = BuildEntryLabel(attr, tag);
                var ident = Id3v2FrameLookup.IdentifierFor(attr, version) ?? string.Empty;
                var existing = attr.IsUniqueInstance && tag.Frames.Any(f => f.GetType() == attr.ItemType);
                return new Id3v2MenuEntry(label, ident, existing);
            })];
    }
}
